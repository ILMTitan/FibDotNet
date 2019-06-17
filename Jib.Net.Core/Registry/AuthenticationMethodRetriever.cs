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
using com.google.cloud.tools.jib.http;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace com.google.cloud.tools.jib.registry
{


    /** Retrieves the {@code WWW-Authenticate} header from the registry API. */
    internal class AuthenticationMethodRetriever : RegistryEndpointProvider<RegistryAuthenticator>
    {
        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
        private readonly IEnumerable<ProductInfoHeaderValue> userAgent;

        public AuthenticationMethodRetriever(
            RegistryEndpointRequestProperties registryEndpointRequestProperties, IEnumerable<ProductInfoHeaderValue> userAgent)
        {
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.userAgent = userAgent;
        }

        public BlobHttpContent getContent()
        {
            return null;
        }

        public IList<string> getAccept()
        {
            return Collections.emptyList<string>();
        }

        /**
         * The request did not error, meaning that the registry does not require authentication.
         *
         * @param response ignored
         * @return {@code null}
         */

        public RegistryAuthenticator handleResponse(HttpResponseMessage response)
        {
            return null;
        }

        public Uri getApiRoute(string apiRouteBase)
        {
            return new Uri(apiRouteBase);
        }

        public HttpMethod getHttpMethod()
        {
            return HttpMethod.Get;
        }

        public string getActionDescription()
        {
            return "retrieve authentication method for " + registryEndpointRequestProperties.getServerUrl();
        }

        public RegistryAuthenticator handleHttpResponse(HttpResponseMessage httpResponse)
        {
            // Only valid for status code of '401 Unauthorized'.
            if (httpResponse.getStatusCode() != HttpStatusCode.Unauthorized)
            {
                throw new HttpResponseException(httpResponse);
            }

            // Checks if the 'WWW-Authenticate' header is present.
            AuthenticationHeaderValue authenticationMethod = httpResponse.getHeaders().getAuthenticate().FirstOrDefault();
            if (authenticationMethod == null)
            {
                throw new RegistryErrorExceptionBuilder(getActionDescription(), httpResponse)
                    .addReason("'WWW-Authenticate' header not found")
                    .build();
            }

            // Parses the header to retrieve the components.
            try
            {
                return RegistryAuthenticator.fromAuthenticationMethod(
                    authenticationMethod, registryEndpointRequestProperties, userAgent);
            }
            catch (RegistryAuthenticationFailedException)
            {
                throw new RegistryErrorExceptionBuilder(getActionDescription(), httpResponse)
                    .addReason("Failed get authentication method from 'WWW-Authenticate' header")
                    .build();
            }
        }
    }
}
