// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using Fib.Net.Core.Api;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Events;
using Fib.Net.Core.Http;
using Fib.Net.Core.Json;
using Fib.Net.Core.Registry.Json;
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
using Authorization = Fib.Net.Core.Http.Authorization;

namespace Fib.Net.Core.Registry
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

        // https://github.com/GoogleContainerTools/fib/issues/1316

        public static bool IsBrokenPipe(Exception original)
        {
            Exception exception = original;
            while (exception != null)
            {
                if (exception is Win32Exception e && e.NativeErrorCode == ERROR_BROKEN_PIPE)
                {
                    return true;
                }

                exception = exception.InnerException;
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

        private static bool IsHttpsProtocol(Uri url)
        {
            return "https" == url.Scheme;
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
              Connection.GetConnectionFactory(),
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
            initialRequestUrl =
                registryEndpointProvider.GetApiRoute(DEFAULT_PROTOCOL + "://" + apiRouteBase);
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
        public async Task<T> CallAsync()
        {
            return await CallWithAllowInsecureRegistryHandlingAsync(initialRequestUrl).ConfigureAwait(false);
        }

        private async Task<T> CallWithAllowInsecureRegistryHandlingAsync(Uri url)
        {
            if (!IsHttpsProtocol(url) && !allowInsecureRegistries)
            {
                throw new InsecureRegistryException(url);
            }

            try
            {
                return await CallAsync(url, connectionFactory).ConfigureAwait(false);
            }
            catch (HttpRequestException e) when (IsSecurityException(e))
            {
                return await HandleUnverifiableServerExceptionAsync(url).ConfigureAwait(false);
            }
            catch (ConnectException)
            {
                if (allowInsecureRegistries && IsHttpsProtocol(url) && url.IsDefaultPort)
                {
                    // Fall back to HTTP only if "url" had no port specified (i.e., we tried the default HTTPS
                    // port 443) and we could not connect to 443. It's worth trying port 80.
                    return await FallBackToHttpAsync(url).ConfigureAwait(false);
                }
                throw;
            }
        }

        private static bool IsSecurityException(HttpRequestException e)
        {
            return e.InnerException is AuthenticationException
                || e.InnerException is IOException ioEx && ioEx.Source == "System.Net.Security"
                || e.InnerException is Win32Exception win32Ex && win32Ex.NativeErrorCode == 12175;
        }

        private async Task<T> HandleUnverifiableServerExceptionAsync(Uri url)
        {
            if (!allowInsecureRegistries)
            {
                throw new InsecureRegistryException(url);
            }

            try
            {
                eventHandlers.Dispatch(
                    LogEvent.Info(
                        "Cannot verify server at " + url + ". Attempting again with no TLS verification."));
                return await CallAsync(url, GetInsecureConnectionFactory()).ConfigureAwait(false);
            }
            catch (AuthenticationException)
            {
                return await FallBackToHttpAsync(url).ConfigureAwait(false);
            }
            catch (HttpRequestException e) when (IsSecurityException(e))
            {
                return await FallBackToHttpAsync(url).ConfigureAwait(false);
            }
        }

        private async Task<T> FallBackToHttpAsync(Uri url)
        {
            UriBuilder httpUrl = new UriBuilder(url)
            {
                Scheme = Uri.UriSchemeHttp,
                Port = url.IsDefaultPort ? -1 : url.Port
            };
            eventHandlers.Dispatch(
                LogEvent.Info(
                    "Failed to connect to " + url + " over HTTPS. Attempting again with HTTP: " + httpUrl));
            return await CallAsync(httpUrl.Uri, connectionFactory).ConfigureAwait(false);
        }

        private Func<Uri, IConnection> GetInsecureConnectionFactory()
        {
            return insecureConnectionFactory ?? (insecureConnectionFactory = Connection.GetInsecureConnectionFactory());
        }

        /**
         * Calls the registry endpoint with a certain {@link Uri}.
         *
         * @param url the endpoint Uri to call
         * @return an object representing the response, or {@code null}
         * @throws IOException for most I/O exceptions when making the request
         * @throws RegistryException for known exceptions when interacting with the registry
         */
        private async Task<T> CallAsync(Uri url, Func<Uri, IConnection> connectionFactory)
        {
            // Only sends authorization if using HTTPS or explicitly forcing over HTTP.
            bool sendCredentials =
                IsHttpsProtocol(url) || FibSystemProperties.IsSendCredentialsOverHttpEnabled();
            try
            {
                using (IConnection connection = connectionFactory(url))
                {
                    var request = new HttpRequestMessage(registryEndpointProvider.GetHttpMethod(), url);
                    foreach (var value in userAgent)
                    {
                        request.Headers.UserAgent.Add(value);
                    }
                    foreach (var accept in registryEndpointProvider.GetAccept())
                    {
                        request.Headers.Accept.ParseAdd(accept);
                    }
                    request.Content = registryEndpointProvider.GetContent();
                    if (sendCredentials && authorization != null)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue(authorization.GetScheme(), authorization.GetToken());
                    }
                    HttpResponseMessage response = await connection.SendAsync(request).ConfigureAwait(false);

                    return await registryEndpointProvider.HandleResponseAsync(response).ConfigureAwait(false);
                }
            }
            catch (HttpResponseException ex)
            {
                if (ex.GetStatusCode() == HttpStatusCode.BadRequest
                    || ex.GetStatusCode() == HttpStatusCode.NotFound
                    || ex.GetStatusCode() == HttpStatusCode.MethodNotAllowed)
                {
                    // The name or reference was invalid.
                    throw await NewRegistryErrorExceptionAsync(ex).ConfigureAwait(false);
                }
                else if (ex.GetStatusCode() == HttpStatusCode.Forbidden)
                {
                    throw new RegistryUnauthorizedException(
                        registryEndpointRequestProperties.GetRegistry(),
                        registryEndpointRequestProperties.GetImageName(),
                        ex);
                }
                else if (ex.GetStatusCode() == HttpStatusCode.Unauthorized)
                {
                    if (sendCredentials)
                    {
                        // Credentials are either missing or wrong.
                        throw new RegistryUnauthorizedException(
                            registryEndpointRequestProperties.GetRegistry(),
                            registryEndpointRequestProperties.GetImageName(),
                            ex);
                    }
                    else
                    {
                        throw new RegistryCredentialsNotSentException(
                            registryEndpointRequestProperties.GetRegistry(),
                            registryEndpointRequestProperties.GetImageName());
                    }
                }
                else if (ex.GetStatusCode() == HttpStatusCode.TemporaryRedirect ||
                    ex.GetStatusCode() == HttpStatusCode.MovedPermanently ||
                    ex.GetStatusCode() == RegistryEndpointCaller.STATUS_CODE_PERMANENT_REDIRECT)
                {
                    // 'Location' header can be relative or absolute.
                    Uri redirectLocation = new Uri(url, ex.GetHeaders().Location);
                    return await CallWithAllowInsecureRegistryHandlingAsync(redirectLocation).ConfigureAwait(false);
                }
                else
                {
                    // Unknown
                    throw;
                }
            }
            catch (IOException ex)
            {
                if (RegistryEndpointCaller.IsBrokenPipe(ex))
                {
                    throw new RegistryBrokenPipeException(ex);
                }
                throw;
            }
            catch (TimeoutException e)
            {
                throw new RegistryNoResponseException(e);
            }
        }

        public async Task<RegistryErrorException> NewRegistryErrorExceptionAsync(HttpResponseException httpResponseException)
        {
            RegistryErrorExceptionBuilder registryErrorExceptionBuilder =
                new RegistryErrorExceptionBuilder(
                    registryEndpointProvider.GetActionDescription(), httpResponseException.Cause);

            string stringContent = await httpResponseException.GetContent().ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                ErrorResponseTemplate errorResponse =
                    JsonTemplateMapper.ReadJson<ErrorResponseTemplate>(stringContent);
                foreach (ErrorEntryTemplate errorEntry in errorResponse?.Errors ?? Enumerable.Empty<ErrorEntryTemplate>())
                {
                    registryErrorExceptionBuilder.AddReason(errorEntry);
                }
            }
            catch (Exception e) when (e is IOException || e is JsonException)
            {
                registryErrorExceptionBuilder.AddReason(
                    $"registry returned error code {httpResponseException.GetStatusCode():D}; " +
                    $"possible causes include invalid or wrong reference. " +
                    $"Actual error output follows:\n{stringContent}\n");
            }

            return registryErrorExceptionBuilder.Build();
        }
    }
}
