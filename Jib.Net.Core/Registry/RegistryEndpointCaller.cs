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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using Authorization = com.google.cloud.tools.jib.http.Authorization;

namespace com.google.cloud.tools.jib.registry
{

















    internal static class RegistryEndpointCaller
    {
        /**
         * @see <a
         *     href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/308">https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/308</a>
         */
        public static readonly HttpStatusCode STATUS_CODE_PERMANENT_REDIRECT = (HttpStatusCode)308;

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
        private static readonly string DEFAULT_PROTOCOL = "https";

        private static bool isHttpsProtocol(Uri url)
        {
            return "https".Equals(url.getProtocol());
        }

        private readonly IEventHandlers eventHandlers;
        private readonly Uri initialRequestUrl;
        private readonly string userAgent;
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
            string userAgent,
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
            string userAgent,
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
        public T call()
        {
            return callWithAllowInsecureRegistryHandling(initialRequestUrl);
        }

        private T callWithAllowInsecureRegistryHandling(Uri url)
        {
            if (!isHttpsProtocol(url) && !allowInsecureRegistries)
            {
                throw new InsecureRegistryException(url);
            }

            try
            {
                return call(url, connectionFactory);
            }
            catch (HttpRequestException e) when (e.InnerException is AuthenticationException)
            {
                return handleUnverifiableServerException(url);
            }
            catch (ConnectException)
            {
                if (allowInsecureRegistries && isHttpsProtocol(url) && url.IsDefaultPort)
                {
                    // Fall back to HTTP only if "url" had no port specified (i.e., we tried the default HTTPS
                    // port 443) and we could not connect to 443. It's worth trying port 80.
                    return fallBackToHttp(url);
                }
                throw;
            }
        }

        private T handleUnverifiableServerException(Uri url)
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
                return call(url, getInsecureConnectionFactory());
            }
            catch (AuthenticationException)
            {
                return fallBackToHttp(url);
            }
            catch(HttpRequestException e) when (e.InnerException is AuthenticationException)
            {
                return fallBackToHttp(url);
            }
        }

        private T fallBackToHttp(Uri url)
        {
            UriBuilder httpUrl = new UriBuilder(url)
            {
                Scheme = Uri.UriSchemeHttp,
                Port = url.IsDefaultPort ? -1 : url.Port
            };
            eventHandlers.dispatch(
                LogEvent.info(
                    "Failed to connect to " + url + " over HTTPS. Attempting again with HTTP: " + httpUrl));
            return call(httpUrl.toURL(), connectionFactory);
        }

        private Func<Uri, IConnection> getInsecureConnectionFactory()
        {
            try
            {
                if (insecureConnectionFactory == null)
                {
                    insecureConnectionFactory = Connection.getInsecureConnectionFactory();
                }
                return insecureConnectionFactory;
            }
            catch (GeneralSecurityException ex)
            {
                throw new RegistryException("cannot turn off TLS peer verification", ex);
            }
        }

        /**
         * Calls the registry endpoint with a certain {@link Uri}.
         *
         * @param url the endpoint Uri to call
         * @return an object representing the response, or {@code null}
         * @throws IOException for most I/O exceptions when making the request
         * @throws RegistryException for known exceptions when interacting with the registry
         */
        private T call(Uri url, Func<Uri, IConnection> connectionFactory)
        {
            // Only sends authorization if using HTTPS or explicitly forcing over HTTP.
            bool sendCredentials =
                isHttpsProtocol(url) || JibSystemProperties.isSendCredentialsOverHttpEnabled();
            try
            {
                using (IConnection connection = connectionFactory.apply(url))
                {
                    var request = new HttpRequestMessage(registryEndpointProvider.getHttpMethod(), url);
                    request.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue(userAgent)));
                    foreach (var accept in registryEndpointProvider.getAccept())
                    {
                        request.Headers.Accept.ParseAdd(accept);
                    }
                    request.Content = registryEndpointProvider.getContent();
                    if (sendCredentials)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue(authorization.getScheme(), authorization.getToken());
                    }
                    HttpResponseMessage response = connection.send(request);

                    return registryEndpointProvider.handleResponse(response);
                }
            }
            catch (HttpResponseException ex)
            {
                {
                    if (ex.getStatusCode() == HttpStatusCode.BadRequest
                        || ex.getStatusCode() == HttpStatusCode.NotFound
                        || ex.getStatusCode()
                            == HttpStatusCode.MethodNotAllowed)
                    {
                        // The name or reference was invalid.
                        throw newRegistryErrorException(ex);
                    }
                    else if (ex.getStatusCode() == HttpStatusCode.Forbidden)
                    {
                        throw new RegistryUnauthorizedException(
                            registryEndpointRequestProperties.getServerUrl(),
                            registryEndpointRequestProperties.getImageName(),
                            ex);
                    }
                    else if (ex.getStatusCode()
                      == HttpStatusCode.Unauthorized)
                    {
                        if (sendCredentials)
                        {
                            // Credentials are either missing or wrong.
                            throw new RegistryUnauthorizedException(
                                registryEndpointRequestProperties.getServerUrl(),
                                registryEndpointRequestProperties.getImageName(),
                                ex);
                        }
                        else
                        {
                            throw new RegistryCredentialsNotSentException(
                                registryEndpointRequestProperties.getServerUrl(),
                                registryEndpointRequestProperties.getImageName());
                        }
                    }
                    else if (ex.getStatusCode() == HttpStatusCode.TemporaryRedirect ||
                        ex.getStatusCode() == HttpStatusCode.MovedPermanently ||
                        ex.getStatusCode() == RegistryEndpointCaller.STATUS_CODE_PERMANENT_REDIRECT)
                    {
                        // 'Location' header can be relative or absolute.
                        Uri redirectLocation = new Uri(url, ex.getHeaders().getLocation());
                        return callWithAllowInsecureRegistryHandling(redirectLocation);
                    }
                    else
                    {
                        // Unknown
                        throw;
                    }
                }
            }
            catch (NoHttpResponseException ex)
            {
                throw new RegistryNoResponseException(ex.Message, ex);
            }
            catch (IOException ex)
            {
                if (RegistryEndpointCaller.isBrokenPipe(ex))
                {
                    throw new RegistryBrokenPipeException(ex);
                }
                throw;
            }
        }

        public RegistryErrorException newRegistryErrorException(HttpResponseException httpResponseException)
        {
            RegistryErrorExceptionBuilder registryErrorExceptionBuilder =
                new RegistryErrorExceptionBuilder(
                    registryEndpointProvider.getActionDescription(), httpResponseException.Cause);

            string stringContent = httpResponseException.getContent().ReadAsStringAsync().Result;
            try
            {
                ErrorResponseTemplate errorResponse =
                    JsonTemplateMapper.readJson<ErrorResponseTemplate>(
                        stringContent);
                foreach (ErrorEntryTemplate errorEntry in errorResponse.getErrors())
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
