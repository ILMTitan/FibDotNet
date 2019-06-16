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
using com.google.cloud.tools.jib.global;
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry.json;
using Jib.Net.Core.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

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
        public static RegistryAuthenticator fromAuthenticationMethod(
            AuthenticationHeaderValue authenticationMethod,
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            string userAgent)
        {
            // If the authentication method starts with 'basic ' (case insensitive), no registry
            // authentication is needed.
            if (authenticationMethod.Scheme.matches("^(?i)(basic)"))
            {
                return null;
            }

            // Checks that the authentication method starts with 'bearer ' (case insensitive).
            if (!authenticationMethod.Scheme.matches("^(?i)(bearer)"))
            {
                throw newRegistryAuthenticationFailedException(
                    registryEndpointRequestProperties.getServerUrl(),
                    registryEndpointRequestProperties.getImageName(),
                    authenticationMethod.Scheme,
                    "Bearer");
            }

            Regex realmPattern = new Regex("realm=\"(.*?)\"");
            Match realmMatcher = realmPattern.matcher(authenticationMethod.Parameter);
            if (!realmMatcher.find())
            {
                throw newRegistryAuthenticationFailedException(
                    registryEndpointRequestProperties.getServerUrl(),
                    registryEndpointRequestProperties.getImageName(),
                    authenticationMethod.Parameter,
                    "realm");
            }
            string realm = realmMatcher.group(1);

            Regex servicePattern = new Regex("service=\"(.*?)\"");
            Match serviceMatcher = servicePattern.matcher(authenticationMethod.Parameter);
            // use the provided registry location when missing service (e.g., for OpenShift)
            string service =
                serviceMatcher.find()
                    ? serviceMatcher.group(1)
                    : registryEndpointRequestProperties.getServerUrl();

            return new RegistryAuthenticator(realm, service, registryEndpointRequestProperties, userAgent);
        }

        private static RegistryAuthenticationFailedException newRegistryAuthenticationFailedException(
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
        private class AuthenticationResponseTemplate
        {
            [JsonProperty]
            public string Token { get; }

            /**
             * {@code access_token} is accepted as an alias for {@code token}.
             *
             * @see <a
             *     href="https://docs.docker.com/registry/spec/auth/token/#token-response-fields">https://docs.docker.com/registry/spec/auth/token/#token-response-fields</a>
             */
            [JsonProperty]
            public string AccessToken { get; }

            /** @return {@link #token} if not null, or {@link #access_token} */
            public string getToken()
            {
                if (Token != null)
                {
                    return Token;
                }
                return AccessToken;
            }
        }

        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
        private readonly string realm;
        private readonly string service;
        private readonly string userAgent;

        private RegistryAuthenticator(
            string realm,
            string service,
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            string userAgent)
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
        public Authorization authenticatePull(Credential credential)
        {
            return authenticate(credential, "pull");
        }

        /**
         * Authenticates permission to pull and push.
         *
         * @param credential the credential used to authenticate
         * @return an {@code Authorization} authenticating the push
         * @throws RegistryAuthenticationFailedException if authentication fails
         */
        public Authorization authenticatePush(Credential credential)
        {
            return authenticate(credential, "pull,push");
        }

        private string getServiceScopeRequestParameters(string scope)
        {
            return "service="
                + service
                + "&scope=repository:"
                + registryEndpointRequestProperties.getImageName()
                + ":"
                + scope;
        }

        public Uri getAuthenticationUrl(Credential credential, string scope)
        {
            if (isOAuth2Auth(credential))
            {
                return new Uri(realm);
            }
            else
            {
                return new UriBuilder(realm)
                {
                    Query = "?" + getServiceScopeRequestParameters(scope)
                }.Uri;
            }
        }

        public string getAuthRequestParameters(Credential credential, string scope)
        {
            string serviceScope = getServiceScopeRequestParameters(scope);
            return isOAuth2Auth(credential)
                ? serviceScope
                    // https://github.com/GoogleContainerTools/jib/pull/1545
                    + "&client_id=jib.da031fe481a93ac107a95a96462358f9"
                    + "&grant_type=refresh_token&refresh_token="
                    // If OAuth2, credential.getPassword() is a refresh token.
                    + Verify.verifyNotNull(credential).getPassword()
                : serviceScope;
        }

        public bool isOAuth2Auth(Credential credential)
        {
            return credential != null && credential.isOAuth2RefreshToken();
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
        private Authorization authenticate(Credential credential, string scope)
        {
            try
            {
                using (Connection connection =
                    Connection.getConnectionFactory().apply(getAuthenticationUrl(credential, scope)))
                {
                    var request = new HttpRequestMessage();
                    request.Headers.UserAgent.ParseAdd(userAgent);

                    if (isOAuth2Auth(credential))
                    {
                        string parameters = getAuthRequestParameters(credential, scope);
                        request.Content = new BlobHttpContent(Blobs.from(parameters), MediaType.FORM_DATA.toString());
                    }
                    else if (credential != null)
                    {
                        Authorization authorization = Authorization.fromBasicCredentials(credential.getUsername(), credential.getPassword());
                        request.Headers.Authorization = new AuthenticationHeaderValue(authorization.getScheme(), authorization.getToken());
                    }
                    if (isOAuth2Auth(credential))
                    {
                        request.Method = HttpMethod.Post;
                    }
                    else
                    {
                        request.Method = HttpMethod.Get;
                    }

                    HttpResponseMessage response = connection.send(request);
                    string responseString =
                        CharStreams.toString(new StreamReader(response.getBody(), StandardCharsets.UTF_8));

                    AuthenticationResponseTemplate responseJson =
                        JsonTemplateMapper.readJson<AuthenticationResponseTemplate>(responseString);

                    if (responseJson.getToken() == null)
                    {
                        throw new RegistryAuthenticationFailedException(
                            registryEndpointRequestProperties.getServerUrl(),
                            registryEndpointRequestProperties.getImageName(),
                            "Did not get token in authentication response from "
                                + getAuthenticationUrl(credential, scope)
                                + "; parameters: "
                                + getAuthRequestParameters(credential, scope));
                    }
                    return Authorization.fromBearerToken(responseJson.getToken());
                }
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                throw new RegistryAuthenticationFailedException(
                    registryEndpointRequestProperties.getServerUrl(),
                    registryEndpointRequestProperties.getImageName(),
                    ex);
            }
        }
    }
}
