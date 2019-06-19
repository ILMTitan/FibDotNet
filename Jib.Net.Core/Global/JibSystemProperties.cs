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
using System;

namespace com.google.cloud.tools.jib.global
{
    /** Names of system properties defined/used by Jib. */
    public static class JibSystemProperties
    {
        private const int defaultTimeoutMills = 60000;
        public static readonly string HTTP_TIMEOUT = "jib.httpTimeout";

        public static readonly string SEND_CREDENTIALS_OVER_HTTP = "sendCredentialsOverHttp";

        private static readonly string SERIALIZE = "jibSerialize";

        private static readonly string DISABLE_USER_AGENT = "_JIB_DISABLE_USER_AGENT";

        /**
         * Gets the HTTP connection/read timeouts for registry interactions in milliseconds. This is
         * defined by the {@code jib.httpTimeout} system property. The default value is 20000 if the
         * system property is not set, and 0 indicates an infinite timeout.
         *
         * @return the HTTP connection/read timeouts for registry interactions in milliseconds
         */
        public static int getHttpTimeout()
        {
            if (int.TryParse(Environment.GetEnvironmentVariable(HTTP_TIMEOUT), out int timeoutMills)
                )
            { return timeoutMills; }
            else
            {
                return defaultTimeoutMills;
            }
        }

        /**
         * Gets whether or not to serialize Jib's execution. This is defined by the {@code jibSerialize}
         * system property.
         *
         * @return {@code true} if Jib's execution should be serialized, {@code false} if not
         */
        public static bool isSerializedExecutionEnabled()
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable(SERIALIZE), out bool serialize))
            {
                return serialize;
            }
            else
            {
                return false;
            }
        }

        /**
         * Gets whether or not to allow sending authentication information over insecure HTTP connections.
         * This is defined by the {@code sendCredentialsOverHttp} system property.
         *
         * @return {@code true} if authentication information is allowed to be sent over insecure
         *     connections, {@code false} if not
         */
        public static bool isSendCredentialsOverHttpEnabled()
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable(SEND_CREDENTIALS_OVER_HTTP), out bool sendCredentialsOverHttp))
            {
                return sendCredentialsOverHttp;
            }
            else
            {
                return false;
            }
        }

        /**
         * Gets whether or not to enable the User-Agent header. This is defined by the {@code
         * _JIB_DISABLE_USER_AGENT} system property.
         *
         * @return {@code true} if the User-Agent header is enabled, {@code false} if not
         */
        public static bool isUserAgentEnabled()
        {
            return Strings.isNullOrEmpty(Environment.GetEnvironmentVariable(DISABLE_USER_AGENT));
        }

        /**
         * Checks the {@code jib.httpTimeout} system property for invalid (non-integer or negative)
         * values.
         *
         * @throws NumberFormatException if invalid values
         */
        public static void checkHttpTimeoutProperty()
        {
            checkNumericSystemProperty(HTTP_TIMEOUT, Range.atLeast(0));
        }

        private static void checkNumericSystemProperty(string property, Range<int> validRange)
        {
            string value = Environment.GetEnvironmentVariable(property);
            if (value == null)
            {
                return;
            }

            int parsed;
            try
            {
                parsed = int.Parse(value);
            }
            catch (FormatException ex)
            {
                throw new FormatException(property + " must be an integer: " + value, ex);
            }
            if (validRange.hasLowerBound() && validRange.lowerEndpoint() > parsed)
            {
                throw new FormatException(
                    property + " cannot be less than " + validRange.lowerEndpoint() + ": " + value);
            }
            else if (validRange.hasUpperBound() && validRange.upperEndpoint() < parsed)
            {
                throw new FormatException(
                    property + " cannot be greater than " + validRange.upperEndpoint() + ": " + value);
            }
        }
    }
}
