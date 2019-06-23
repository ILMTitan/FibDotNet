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

using Jib.Net.Core.FileSystem;
using System;

namespace com.google.cloud.tools.jib.registry.credentials
{
    /** Thrown because the credential helper does not have credentials for the specified server Uri. */
    public class CredentialHelperUnhandledServerUrlException : CredentialRetrievalException
    {
        public CredentialHelperUnhandledServerUrlException(
            SystemPath credentialHelper,
            string serverUrl,
            string credentialHelperOutput,
            Exception cause)
            : base("The credential helper ("
                  + credentialHelper
                  + ") has nothing for server Uri: "
                  + serverUrl
                  + "\n\nGot output:\n\n"
                  + credentialHelperOutput, cause)
        {
        }

        public CredentialHelperUnhandledServerUrlException(SystemPath credentialHelper, string serverUrl, string credentialHelperOutput) : base(
        "The credential helper ("
            + credentialHelper
            + ") has nothing for server Uri: "
            + serverUrl
            + "\n\nGot output:\n\n"
            + credentialHelperOutput)
        {
        }
    }
}
