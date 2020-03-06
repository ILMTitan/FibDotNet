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
using Fib.Net.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Fib.Net.Core.Registry
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

        public BlobHttpContent GetContent()
        {
            return null;
        }

        public IList<string> GetAccept()
        {
            return new List<string>();
        }

        /**
         * The request did not error, meaning that the registry does not require authentication.
         *
         * @param response ignored
         * @return {@code null}
         */

        public Task<RegistryAuthenticator> HandleResponseAsync(HttpResponseMessage response)
        {
            return Task.FromResult(HandleHttpResponse(response));
        }

        public Uri GetApiRoute(string apiRouteBase)
        {
            return new Uri(apiRouteBase);
        }

        public HttpMethod GetHttpMethod()
        {
            return HttpMethod.Get;
        }

        public string GetActionDescription()
        {
            return "retrieve authentication method for " + registryEndpointRequestProperties.GetRegistry();
        }

        public RegistryAuthenticator HandleHttpResponse(HttpResponseMessage httpResponse)
        {
            if (httpResponse.IsSuccessStatusCode)
            {
                return null;
            }
            // Only valid for status code of '401 Unauthorized'.
            if (httpResponse.StatusCode != HttpStatusCode.Unauthorized)
            {
                throw new HttpResponseException(httpResponse);
            }

            // Checks if the 'WWW-Authenticate' header is present.
            AuthenticationHeaderValue authenticationMethod = httpResponse.Headers.WwwAuthenticate.FirstOrDefault();
            if (authenticationMethod == null)
            {
                throw new RegistryErrorExceptionBuilder(GetActionDescription(), httpResponse)
                    .AddReason("'WWW-Authenticate' header not found")
                    .Build();
            }

            // Parses the header to retrieve the components.
            try
            {
                return RegistryAuthenticator.FromAuthenticationMethod(
                    authenticationMethod, registryEndpointRequestProperties, userAgent);
            }
            catch (RegistryAuthenticationFailedException)
            {
                throw new RegistryErrorExceptionBuilder(GetActionDescription(), httpResponse)
                    .AddReason("Failed get authentication method from 'WWW-Authenticate' header")
                    .Build();
            }
        }
    }
}
