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

using Fib.Net.Core.FileSystem;
using System;
using System.Runtime.Serialization;

namespace Fib.Net.Core.Registry.Credentials
{
    /** Thrown because the credential helper does not have credentials for the specified server Uri. */
    [Serializable]
    public class CredentialHelperUnhandledServerUrlException : CredentialRetrievalException
    {
        public CredentialHelperUnhandledServerUrlException(
            SystemPath credentialHelper,
            string registry,
            string credentialHelperOutput,
            Exception cause)
            : base("The credential helper ("
                  + credentialHelper
                  + ") has nothing for server Uri: "
                  + registry
                  + "\n\nGot output:\n\n"
                  + credentialHelperOutput, cause)
        {
        }

        public CredentialHelperUnhandledServerUrlException(SystemPath credentialHelper, string registry, string credentialHelperOutput) : base(
        "The credential helper ("
            + credentialHelper
            + ") has nothing for server Uri: "
            + registry
            + "\n\nGot output:\n\n"
            + credentialHelperOutput)
        {
        }

        protected CredentialHelperUnhandledServerUrlException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
