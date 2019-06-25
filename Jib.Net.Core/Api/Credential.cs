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

using System;

namespace com.google.cloud.tools.jib.api
{
    /** Holds credentials (username and password). */
    public sealed class Credential
    {
        // If the username is set to <token>, the secret would be a refresh token.
        // https://github.com/docker/cli/blob/master/docs/reference/commandline/login.md#credential-helper-protocol
        private const string OAUTH2_TOKEN_USER_NAME = "<token>";

        /**
         * Gets a {@link Credential} configured with a username and password.
         *
         * @param username the username
         * @param password the password
         * @return a new {@link Credential}
         */
        public static Credential from(string username, string password)
        {
            return new Credential(username, password);
        }

        private readonly string username;
        private readonly string password;

        private Credential(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        /**
         * Gets the username.
         *
         * @return the username
         */
        public string getUsername()
        {
            return username;
        }

        /**
         * Gets the password.
         *
         * @return the password
         */
        public string getPassword()
        {
            return password;
        }

        /**
         * Check whether this credential is an OAuth 2.0 refresh token.
         *
         * @return true if this credential is an OAuth 2.0 refresh token.
         */
        public bool isOAuth2RefreshToken()
        {
            return OAUTH2_TOKEN_USER_NAME == username;
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (!(other is Credential otherCredential))
            {
                return false;
            }
            return username == otherCredential.username
                && password == otherCredential.password;
        }

        public override int GetHashCode()
        {
            return Objects.hash(username, password);
        }

        public override string ToString()
        {
            return username + ":" + password;
        }
    }
}
