/*
 * Copyright 2018 Google LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.global;
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry.json;
using Jib.Net.Core.Global;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Threading.Tasks;
using Authorization = com.google.cloud.tools.jib.http.Authorization;

namespace com.google.cloud.tools.jib.registry
{
    internal static class RegistryEndpointCaller
    {
        /**
         * @see <a
         *     href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/308">https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/308</a>
         */
        public const HttpStatusCode STATUS_CODE_PERMANENT_REDIRECT = (HttpStatusCode)308;

        /// <summary>
        /// The broken pipe error code.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/openspecs/windows_protocols/ms-erref"/>
        public const int ERROR_BROKEN_PIPE = 0x6d;

        // https://github.com/GoogleContainerTools/jib/issues/1316

        public static bool isBrokenPipe(Exception original)
        {
            Exception exception = original;
            while (exception != null)
            {
                if (exception is Win32Exception e && e.NativeErrorCode == ERROR_BROKEN_PIPE)
                {
                    return true;
                }

                exception = exception.getCause();
            }
            return false;
        }
    }

    /**
     * Makes requests to a registry endpoint.
     *
     * @param <T> the type returned by calling the endpoint
     */
    internal class RegistryEndpointCaller<T>
    {
        private const string DEFAULT_PROTOCOL = "https";

        private static bool isHttpsProtocol(Uri url)
        {
            return "https" == url.getProtocol();
        }

        private readonly IEventHandlers eventHandlers;
        private readonly Uri initialRequestUrl;
        private readonly IEnumerable<ProductInfoHeaderValue> userAgent;
        private readonly RegistryEndpointProvider<T> registryEndpointProvider;
        private readonly Authorization authorization;
        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
        private readonly bool allowInsecureRegistries;

        /** Makes a {@link Connection} to the specified {@link Uri}. */
        private readonly Func<Uri, IConnection> connectionFactory;

        /** Makes an insecure {@link Connection} to the specified {@link Uri}. */
        private Func<Uri, IConnection> insecureConnectionFactory;

        /**
         * Constructs with parameters for making the request.
         *
         * @param eventHandlers the event dispatcher used for dispatching log events
         * @param userAgent {@code User-Agent} header to send with the request
         * @param apiRouteBase the endpoint's API root, without the protocol
         * @param registryEndpointProvider the {@link RegistryEndpointProvider} to the endpoint
         * @param authorization optional authentication credentials to use
         * @param registryEndpointRequestProperties properties of the registry endpoint request
         * @param allowInsecureRegistries if {@code true}, insecure connections will be allowed
         * @throws MalformedURLException if the Uri generated for the endpoint is malformed
         */
        public RegistryEndpointCaller(
            IEventHandlers eventHandlers,
            IEnumerable<ProductInfoHeaderValue> userAgent,
            string apiRouteBase,
            RegistryEndpointProvider<T> registryEndpointProvider,
            Authorization authorization,
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            bool allowInsecureRegistries) :
          this(
              eventHandlers,
              userAgent,
              apiRouteBase,
              registryEndpointProvider,
              authorization,
              registryEndpointRequestProperties,
              allowInsecureRegistries,
              Connection.getConnectionFactory(),
              null /* might never be used, so create lazily to delay throwing potential GeneralSecurityException */)
        {
        }

        public RegistryEndpointCaller(
            IEventHandlers eventHandlers,
            IEnumerable<ProductInfoHeaderValue> userAgent,
            string apiRouteBase,
            RegistryEndpointProvider<T> registryEndpointProvider,
            Authorization authorization,
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            bool allowInsecureRegistries,
            Func<Uri, IConnection> connectionFactory,
            Func<Uri, IConnection> insecureConnectionFactory)
        {
            this.eventHandlers = eventHandlers;
            this.initialRequestUrl =
                registryEndpointProvider.getApiRoute(DEFAULT_PROTOCOL + "://" + apiRouteBase);
            this.userAgent = userAgent;
            this.registryEndpointProvider = registryEndpointProvider;
            this.authorization = authorization;
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.allowInsecureRegistries = allowInsecureRegistries;
            this.connectionFactory = connectionFactory;
            this.insecureConnectionFactory = insecureConnectionFactory;
        }

        /**
         * Makes the request to the endpoint.
         *
         * @return an object representing the response, or {@code null}
         * @throws IOException for most I/O exceptions when making the request
         * @throws RegistryException for known exceptions when interacting with the registry
         */
        public async Task<T> callAsync()
        {
            return await callWithAllowInsecureRegistryHandlingAsync(initialRequestUrl).ConfigureAwait(false);
        }

        private async Task<T> callWithAllowInsecureRegistryHandlingAsync(Uri url)
        {
            if (!isHttpsProtocol(url) && !allowInsecureRegistries)
            {
                throw new InsecureRegistryException(url);
            }

            try
            {
                return await callAsync(url, connectionFactory).ConfigureAwait(false);
            }
            catch (HttpRequestException e) when (e.InnerException is AuthenticationException || (e.InnerException is IOException ioEx && ioEx.Source == "System.Net.Security"))
            {
                return await handleUnverifiableServerExceptionAsync(url).ConfigureAwait(false);
            }
            catch (ConnectException)
            {
                if (allowInsecureRegistries && isHttpsProtocol(url) && url.IsDefaultPort)
                {
                    // Fall back to HTTP only if "url" had no port specified (i.e., we tried the default HTTPS
                    // port 443) and we could not connect to 443. It's worth trying port 80.
                    return await fallBackToHttpAsync(url).ConfigureAwait(false);
                }
                throw;
            }
        }

        private async Task<T> handleUnverifiableServerExceptionAsync(Uri url)
        {
            if (!allowInsecureRegistries)
            {
                throw new InsecureRegistryException(url);
            }

            try
            {
                eventHandlers.dispatch(
                    LogEvent.info(
                        "Cannot verify server at " + url + ". Attempting again with no TLS verification."));
                return await callAsync(url, getInsecureConnectionFactory()).ConfigureAwait(false);
            }
            catch (AuthenticationException)
            {
                return await fallBackToHttpAsync(url).ConfigureAwait(false);
            }
            catch(HttpRequestException e) when (e.InnerException is AuthenticationException || (e.InnerException is IOException ioEx && ioEx.Source == "System.Net.Security"))
            {
                return await fallBackToHttpAsync(url).ConfigureAwait(false);
            }
        }

        private async Task<T> fallBackToHttpAsync(Uri url)
        {
            UriBuilder httpUrl = new UriBuilder(url)
            {
                Scheme = Uri.UriSchemeHttp,
                Port = url.IsDefaultPort ? -1 : url.Port
            };
            eventHandlers.dispatch(
                LogEvent.info(
                    "Failed to connect to " + url + " over HTTPS. Attempting again with HTTP: " + httpUrl));
            return await callAsync(httpUrl.toURL(), connectionFactory).ConfigureAwait(false);
        }

        private Func<Uri, IConnection> getInsecureConnectionFactory()
        {
            if (insecureConnectionFactory == null)
            {
                insecureConnectionFactory = Connection.getInsecureConnectionFactory();
            }
            return insecureConnectionFactory;
        }

        /**
         * Calls the registry endpoint with a certain {@link Uri}.
         *
         * @param url the endpoint Uri to call
         * @return an object representing the response, or {@code null}
         * @throws IOException for most I/O exceptions when making the request
         * @throws RegistryException for known exceptions when interacting with the registry
         */
        private async Task<T> callAsync(Uri url, Func<Uri, IConnection> connectionFactory)
        {
            // Only sends authorization if using HTTPS or explicitly forcing over HTTP.
            bool sendCredentials =
                isHttpsProtocol(url) || JibSystemProperties.isSendCredentialsOverHttpEnabled();
            try
            {
                using (IConnection connection = connectionFactory.apply(url))
                {
                    var request = new HttpRequestMessage(registryEndpointProvider.getHttpMethod(), url);
                    foreach (var value in userAgent)
                    {
                        request.Headers.UserAgent.Add(value);
                    }
                    foreach (var accept in registryEndpointProvider.getAccept())
                    {
                        request.Headers.Accept.ParseAdd(accept);
                    }
                    request.Content = registryEndpointProvider.getContent();
                    if (sendCredentials && authorization != null)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue(authorization.getScheme(), authorization.getToken());
                    }
                    HttpResponseMessage response = await connection.sendAsync(request).ConfigureAwait(false);

                    return await registryEndpointProvider.handleResponseAsync(response).ConfigureAwait(false);
                }
            }
            catch (HttpResponseException ex)
            {
                {
                    if (ex.getStatusCode() == HttpStatusCode.BadRequest
                        || ex.getStatusCode() == HttpStatusCode.NotFound
                        || ex.getStatusCode() == HttpStatusCode.MethodNotAllowed)
                    {
                        // The name or reference was invalid.
                        throw await newRegistryErrorExceptionAsync(ex).ConfigureAwait(false);
                    }
                    else if (ex.getStatusCode() == HttpStatusCode.Forbidden)
                    {
                        throw new RegistryUnauthorizedException(
                            registryEndpointRequestProperties.getRegistry(),
                            registryEndpointRequestProperties.getImageName(),
                            ex);
                    }
                    else if (ex.getStatusCode() == HttpStatusCode.Unauthorized)
                    {
                        if (sendCredentials)
                        {
                            // Credentials are either missing or wrong.
                            throw new RegistryUnauthorizedException(
                                registryEndpointRequestProperties.getRegistry(),
                                registryEndpointRequestProperties.getImageName(),
                                ex);
                        }
                        else
                        {
                            throw new RegistryCredentialsNotSentException(
                                registryEndpointRequestProperties.getRegistry(),
                                registryEndpointRequestProperties.getImageName());
                        }
                    }
                    else if (ex.getStatusCode() == HttpStatusCode.TemporaryRedirect ||
                        ex.getStatusCode() == HttpStatusCode.MovedPermanently ||
                        ex.getStatusCode() == RegistryEndpointCaller.STATUS_CODE_PERMANENT_REDIRECT)
                    {
                        // 'Location' header can be relative or absolute.
                        Uri redirectLocation = new Uri(url, ex.getHeaders().getLocation());
                        return await callWithAllowInsecureRegistryHandlingAsync(redirectLocation).ConfigureAwait(false);
                    }
                    else
                    {
                        // Unknown
                        throw;
                    }
                }
            }
            catch (IOException ex)
            {
                if (RegistryEndpointCaller.isBrokenPipe(ex))
                {
                    throw new RegistryBrokenPipeException(ex);
                }
                throw;
            }
            catch (TimeoutException e)
            {
                throw new RegistryNoResponseException("Registry failed to respond", e);
            }
        }

        public async Task<RegistryErrorException> newRegistryErrorExceptionAsync(HttpResponseException httpResponseException)
        {
            RegistryErrorExceptionBuilder registryErrorExceptionBuilder =
                new RegistryErrorExceptionBuilder(
                    registryEndpointProvider.getActionDescription(), httpResponseException.Cause);

            string stringContent = await httpResponseException.getContent().ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                ErrorResponseTemplate errorResponse =
                    JsonTemplateMapper.readJson<ErrorResponseTemplate>(stringContent);
                foreach (ErrorEntryTemplate errorEntry in errorResponse?.getErrors() ?? Enumerable.Empty<ErrorEntryTemplate>())
                {
                    registryErrorExceptionBuilder.addReason(errorEntry);
                }
            }
            catch (Exception e) when (e is IOException || e is JsonException)
            {
                registryErrorExceptionBuilder.addReason(
                    $"registry returned error code {httpResponseException.getStatusCode():D}; " +
                    $"possible causes include invalid or wrong reference. " +
                    $"Actual error output follows:\n{stringContent}\n");
            }

            return registryErrorExceptionBuilder.build();
        }
    }
}
