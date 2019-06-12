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
    /**
     * Throw when attempting to access an insecure registry when only secure connections are allowed.
     */
    public class InsecureRegistryException : RegistryException
    {
        public InsecureRegistryException(Uri insecureUrl) : base(
              "Failed to verify the server at "
                  + insecureUrl
                  + " because only secure connections are allowed.")
        {
        }

        public InsecureRegistryException(string message, Exception cause) : base(message, cause)
        {
        }

        public InsecureRegistryException(string message) : base(message)
        {
        }

        public InsecureRegistryException(string message, System.Net.Http.HttpResponseMessage cause) : base(message, cause)
        {
        }

        public InsecureRegistryException() : base()
        {
        }
    }
}