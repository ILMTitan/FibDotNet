/*
 * Copyright 2017 Google LLC.
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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /**
     * Authenticates push/pull access with a registry service.
     *
     * @see <a
     *     href="https://docs.docker.com/registry/spec/auth/token/">https://docs.docker.com/registry/spec/auth/token/</a>
     */
    public sealed class RegistryAuthenticator
    {
        // TODO: Replace with a WWW-Authenticate header parser.
        /**
         * Instantiates from parsing a {@code WWW-Authenticate} header.
         *
         * @param authenticationMethod the {@code WWW-Authenticate} header value
         * @param registryEndpointRequestProperties the registry request properties
         * @param userAgent the {@code User-Agent} header value to use in later authentication calls
         * @return a new {@link RegistryAuthenticator} for authenticating with the registry service
         * @throws RegistryAuthenticationFailedException if authentication fails
         * @see <a
         *     href="https://docs.docker.com/registry/spec/auth/token/#how-to-authenticate">https://docs.docker.com/registry/spec/auth/token/#how-to-authenticate</a>
         */
        public static RegistryAuthenticator FromAuthenticationMethod(
            AuthenticationHeaderValue authenticationMethod,
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            IEnumerable<ProductInfoHeaderValue> userAgent)
        {
            authenticationMethod =
                authenticationMethod ?? throw new ArgumentNullException(nameof(authenticationMethod));
            registryEndpointRequestProperties =
                registryEndpointRequestProperties ??
                throw new ArgumentNullException(nameof(registryEndpointRequestProperties));
            // If the authentication method starts with 'basic ' (case insensitive), no registry
            // authentication is needed.
            if (authenticationMethod.Scheme.Matches("^(?i)(basic)"))
            {
                return null;
            }

            // Checks that the authentication method starts with 'bearer ' (case insensitive).
            if (!authenticationMethod.Scheme.Matches("^(?i)(bearer)"))
            {
                throw NewRegistryAuthenticationFailedException(
                    registryEndpointRequestProperties.GetRegistry(),
                    registryEndpointRequestProperties.GetImageName(),
                    authenticationMethod.Scheme,
                    "Bearer");
            }

            Regex realmPattern = new Regex("realm=\"(.*?)\"");
            Match realmMatcher = realmPattern.Matcher(authenticationMethod.Parameter);
            if (!realmMatcher.Find())
            {
                throw NewRegistryAuthenticationFailedException(
                    registryEndpointRequestProperties.GetRegistry(),
                    registryEndpointRequestProperties.GetImageName(),
                    authenticationMethod.Parameter,
                    "realm");
            }
            string realm = realmMatcher.Group(1);

            Regex servicePattern = new Regex("service=\"(.*?)\"");
            Match serviceMatcher = servicePattern.Matcher(authenticationMethod.Parameter);
            // use the provided registry location when missing service (e.g., for OpenShift)
            string service =
                serviceMatcher.Find()
                    ? serviceMatcher.Group(1)
                    : registryEndpointRequestProperties.GetRegistry();

            return new RegistryAuthenticator(realm, service, registryEndpointRequestProperties, userAgent);
        }

        private static RegistryAuthenticationFailedException NewRegistryAuthenticationFailedException(
            string registry, string repository, string authenticationMethod, string authParam)
        {
            return new RegistryAuthenticationFailedException(
                registry,
                repository,
                "'"
                    + authParam
                    + "' was not found in the 'WWW-Authenticate' header, tried to parse: "
                    + authenticationMethod);
        }

        /** Template for the authentication response JSON. */
        [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
        public class AuthenticationResponseTemplate
        {
            public string Token { get; }

            /**
             * {@code access_token} is accepted as an alias for {@code token}.
             *
             * @see <a
             *     href="https://docs.docker.com/registry/spec/auth/token/#token-response-fields">https://docs.docker.com/registry/spec/auth/token/#token-response-fields</a>
             */
            public string AccessToken { get; }

            [JsonConstructor]
            public AuthenticationResponseTemplate(string token, string accessToken)
            {
                Token = token;
                AccessToken = accessToken;
            }

            /** @return {@link #token} if not null, or {@link #access_token} */
            public string GetTokenOrAccessToken()
            {
                return Token ?? AccessToken;
            }
        }

        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
        private readonly string realm;
        private readonly string service;
        private readonly IEnumerable<ProductInfoHeaderValue> userAgent;

        private RegistryAuthenticator(
            string realm,
            string service,
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            IEnumerable<ProductInfoHeaderValue> userAgent)
        {
            this.realm = realm;
            this.service = service;
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.userAgent = userAgent;
        }

        /**
         * Authenticates permissions to pull.
         *
         * @param credential the credential used to authenticate
         * @return an {@code Authorization} authenticating the pull
         * @throws RegistryAuthenticationFailedException if authentication fails
         */
        public async Task<Authorization> AuthenticatePullAsync(Credential credential)
        {
            return await AuthenticateAsync(credential, "pull").ConfigureAwait(false);
        }

        /**
         * Authenticates permission to pull and push.
         *
         * @param credential the credential used to authenticate
         * @return an {@code Authorization} authenticating the push
         * @throws RegistryAuthenticationFailedException if authentication fails
         */
        public async Task<Authorization> AuthenticatePushAsync(Credential credential)
        {
            return await AuthenticateAsync(credential, "pull,push").ConfigureAwait(false);
        }

        private string GetServiceScopeRequestParameters(string scope)
        {
            return "service="
                + service
                + "&scope=repository:"
                + registryEndpointRequestProperties.GetImageName()
                + ":"
                + scope;
        }

        public Uri GetAuthenticationUrl(Credential credential, string scope)
        {
            if (IsOAuth2Auth(credential))
            {
                return new Uri(realm);
            }
            else
            {
                return new UriBuilder(realm)
                {
                    Query = "?" + GetServiceScopeRequestParameters(scope)
                }.Uri;
            }
        }

        public string GetAuthRequestParameters(Credential credential, string scope)
        {
            string serviceScope = GetServiceScopeRequestParameters(scope);
            return IsOAuth2Auth(credential)
                ? serviceScope
                    // https://github.com/GoogleContainerTools/jib/pull/1545
                    + "&client_id=jib.da031fe481a93ac107a95a96462358f9"
                    + "&grant_type=refresh_token&refresh_token="
                    // If OAuth2, credential.getPassword() is a refresh token.
                    + Verify.VerifyNotNull(credential).GetPassword()
                : serviceScope;
        }

        public bool IsOAuth2Auth(Credential credential)
        {
            return credential?.IsOAuth2RefreshToken() == true;
        }

        /**
         * Sends the authentication request and retrieves the Bearer authorization token.
         *
         * @param credential the credential used to authenticate
         * @param scope the scope of permissions to authenticate for
         * @return the {@link Authorization} response
         * @throws RegistryAuthenticationFailedException if authentication fails
         * @see <a
         *     href="https://docs.docker.com/registry/spec/auth/token/#how-to-authenticate">https://docs.docker.com/registry/spec/auth/token/#how-to-authenticate</a>
         */
        private async Task<Authorization> AuthenticateAsync(Credential credential, string scope)
        {
            try
            {
                using (Connection connection =
                    Connection.GetConnectionFactory().Apply(GetAuthenticationUrl(credential, scope)))
                using (var request = new HttpRequestMessage())
                {
                    foreach (var value in userAgent)
                    {
                        request.Headers.UserAgent.Add(value);
                    }

                    if (IsOAuth2Auth(credential))
                    {
                        string parameters = GetAuthRequestParameters(credential, scope);
                        request.Content = new BlobHttpContent(Blobs.From(parameters), JavaExtensions.ToString(MediaType.FORM_DATA));
                    }
                    else if (credential != null)
                    {
                        Authorization authorization = Authorization.FromBasicCredentials(credential.GetUsername(), credential.GetPassword());
                        request.Headers.Authorization = new AuthenticationHeaderValue(authorization.GetScheme(), authorization.GetToken());
                    }
                    if (IsOAuth2Auth(credential))
                    {
                        request.Method = HttpMethod.Post;
                    }
                    else
                    {
                        request.Method = HttpMethod.Get;
                    }

                    string responseString;
                    using (HttpResponseMessage response = await connection.SendAsync(request).ConfigureAwait(false))
                    using (StreamReader reader = new StreamReader(await response.GetBodyAsync().ConfigureAwait(false), Encoding.UTF8))
                    {
                        responseString = CharStreams.ToString(reader);
                    }

                    AuthenticationResponseTemplate responseJson =
                        JsonTemplateMapper.ReadJson<AuthenticationResponseTemplate>(responseString);

                    if (responseJson.GetTokenOrAccessToken() == null)
                    {
                        throw new RegistryAuthenticationFailedException(
                            registryEndpointRequestProperties.GetRegistry(),
                            registryEndpointRequestProperties.GetImageName(),
                            "Did not get token in authentication response from "
                                + GetAuthenticationUrl(credential, scope)
                                + "; parameters: "
                                + GetAuthRequestParameters(credential, scope));
                    }
                    return Authorization.FromBearerToken(responseJson.GetTokenOrAccessToken());
                }
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                throw new RegistryAuthenticationFailedException(
                    registryEndpointRequestProperties.GetRegistry(),
                    registryEndpointRequestProperties.GetImageName(),
                    ex);
            }
        }
    }
}
