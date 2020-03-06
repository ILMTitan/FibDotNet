// Copyright 2017 Google LLC.
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

using System;
using System.Net.Http.Headers;
using System.Text;

namespace Fib.Net.Core.Http
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
            return a?.ToAuthenticationHeaderValue();
        }

        public AuthenticationHeaderValue ToAuthenticationHeaderValue()
        {
            return new AuthenticationHeaderValue(GetScheme(), GetToken());
        }

        /**
         * @param token the token
         * @return an {@link Authorization} with a {@code Bearer} token
         */
        public static Authorization FromBearerToken(string token)
        {
            return new Authorization("Bearer", token);
        }

        /**
         * @param username the username
         * @param secret the secret
         * @return an {@link Authorization} with a {@code Basic} credentials
         */
        public static Authorization FromBasicCredentials(string username, string secret)
        {
            string credentials = username + ":" + secret;
            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            return new Authorization("Basic", token);
        }

        /**
         * @param token the token
         * @return an {@link Authorization} with a base64-encoded {@code username:password} string
         */
        public static Authorization FromBasicToken(string token)
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

        public string GetScheme()
        {
            return scheme;
        }

        public string GetToken()
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
            if (!(other is Authorization otherAuthorization))
            {
                return false;
            }
            return scheme == otherAuthorization.scheme && token == otherAuthorization.token;
        }

        public override int GetHashCode()
        {
            return Objects.Hash(scheme, token);
        }
    }
}
