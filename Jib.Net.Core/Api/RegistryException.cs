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

using com.google.cloud.tools.jib.registry;
using System;
using System.Net.Http;

namespace com.google.cloud.tools.jib.api
{
    /** Thrown when interacting with a registry. */
    public class RegistryException : Exception
    {
        public RegistryException(string message, HttpResponseMessage cause) : base(message)
        {
            Cause = cause;
        }

        public RegistryException(HttpResponseMessage cause)
        {
            Cause = cause;
        }

        public RegistryException(string message) : base(message)
        {
        }

        public RegistryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public HttpResponseMessage Cause { get; }
    }
}
