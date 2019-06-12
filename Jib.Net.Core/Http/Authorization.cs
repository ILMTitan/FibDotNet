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
using com.google.cloud.tools.jib.docker;
using Jib.Net.Core.Global;
using System;
using System.Net.Http.Headers;

namespace com.google.cloud.tools.jib.http
{
    /**
     * Holds the credentials for an HTTP {@code Authorization} header.
     *
     * <p>The HTTP {@code Authorization} header is in the format:
     *
     * <pre>{@code Authorization: <scheme> <token>}</pre>
     */
    public sealed class Authorization
    {
        public static implicit operator AuthenticationHeaderValue(Authorization a)
        {
            return new AuthenticationHeaderValue(a.getScheme(), a.getToken());
        }

        /**
         * @param token the token
         * @return an {@link Authorization} with a {@code Bearer} token
         */
        public static Authorization fromBearerToken(string token)
        {
            return new Authorization("Bearer", token);
        }

        /**
         * @param username the username
         * @param secret the secret
         * @return an {@link Authorization} with a {@code Basic} credentials
         */
        public static Authorization fromBasicCredentials(string username, string secret)
        {
            string credentials = username + ":" + secret;
            string token = Convert.ToBase64String(credentials.getBytes(StandardCharsets.UTF_8));
            return new Authorization("Basic", token);
        }

        /**
         * @param token the token
         * @return an {@link Authorization} with a base64-encoded {@code username:password} string
         */
        public static Authorization fromBasicToken(string token)
        {
            return new Authorization("Basic", token);
        }

        private readonly string scheme;
        private readonly string token;

        private Authorization(string scheme, string token)
        {
            this.scheme = scheme;
            this.token = token;
        }

        public string getScheme()
        {
            return scheme;
        }

        public string getToken()
        {
            return token;
        }

        /** Return the HTTP {@link Authorization} header value. */

        public override string ToString()
        {
            return scheme + " " + token;
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (!(other is Authorization))
            {
                return false;
            }
            Authorization otherAuthorization = (Authorization)other;
            return scheme.Equals(otherAuthorization.scheme) && token.Equals(otherAuthorization.token);
        }

        public override int GetHashCode()
        {
            return Objects.hash(scheme, token);
        }
    }
}
