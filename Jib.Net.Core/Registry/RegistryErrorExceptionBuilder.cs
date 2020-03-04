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

using Jib.Net.Core.Global;
using Jib.Net.Core.Registry.Json;
using System;
using System.Net.Http;
using System.Text;

namespace Jib.Net.Core.Registry
{
    /** Builds a {@link RegistryErrorException} with multiple causes. */
    public class RegistryErrorExceptionBuilder
    {
        private readonly HttpResponseMessage cause;
        private readonly StringBuilder errorMessageBuilder = new StringBuilder();

        private bool firstErrorReason = true;

        /**
         * Gets the reason for certain errors.
         *
         * @param errorCodeString string form of {@link ErrorCodes}
         * @param message the original received error message, which may or may not be used depending on
         *     the {@code errorCode}
         */
        private static string GetReason(ErrorCode? errorCode, string message)
        {
            if (message == null)
            {
                message = "no details";
            }

            if (!errorCode.HasValue)
            {
                // Unknown errorCodeString
                return "unknown: " + message;
            }

            if (errorCode == ErrorCode.ManifestInvalid || errorCode == ErrorCode.BlobUnknown)
            {
                return message + " (something went wrong)";
            }
            else if (errorCode == ErrorCode.ManifestUnknown
              || errorCode == ErrorCode.TagInvalid
              || errorCode == ErrorCode.ManifestUnverified)
            {
                return message;
            }
            else
            {
                return "other: " + message;
            }
        }

        /** @param method the registry method that errored */
        public RegistryErrorExceptionBuilder(string method, HttpResponseMessage cause)
        {
            this.cause = cause;

            JavaExtensions.Append(errorMessageBuilder, "Tried to ");
            JavaExtensions.Append(errorMessageBuilder, method);
            JavaExtensions.Append(errorMessageBuilder, " but failed because: ");
        }

        /** @param method the registry method that errored */
        public RegistryErrorExceptionBuilder(string method) : this(method, null)
        {
        }

        // TODO: Don't use a JsonTemplate as a data object to pass around.
        /**
         * Builds an entry to the error reasons from an {@link ErrorEntryTemplate}.
         *
         * @param errorEntry the {@link ErrorEntryTemplate} to add
         */
        public RegistryErrorExceptionBuilder AddReason(ErrorEntryTemplate errorEntry)
        {
            errorEntry = errorEntry ?? throw new ArgumentNullException(nameof(errorEntry));
            string reason = GetReason(errorEntry.Code, errorEntry.Message);
            AddReason(reason);
            return this;
        }

        /** Adds an entry to the error reasons. */
        public RegistryErrorExceptionBuilder AddReason(string reason)
        {
            if (!firstErrorReason)
            {
                JavaExtensions.Append(errorMessageBuilder, ", ");
            }
            JavaExtensions.Append(errorMessageBuilder, reason);
            firstErrorReason = false;
            return this;
        }

        public RegistryErrorException Build()
        {
            // Provides a feedback channel.
            JavaExtensions.Append(
errorMessageBuilder, " | If this is a bug, please file an issue at " + ProjectInfo.GitHubNewIssueUrl);
            return new RegistryErrorException(JavaExtensions.ToString(errorMessageBuilder), cause);
        }
    }
}
