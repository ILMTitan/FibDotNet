/*
 * Copyright 2019 Google LLC.
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

namespace com.google.cloud.tools.jib.registry {


/** Thrown when the registry shut down the connection. */
class RegistryBrokenPipeException : RegistryException {

  public RegistryBrokenPipeException(Exception cause) : base(
        "I/O error due to broken pipe: the server shut down the connection. "
            + "Check the server log if possible. This could also be a proxy issue. For example,"
            + "a proxy may prevent sending packets that are too large.",
        cause) {
    
  }

        public RegistryBrokenPipeException(string message, Exception cause) : base(message, cause)
        {
        }

        public RegistryBrokenPipeException(string message) : base(message)
        {
        }

        public RegistryBrokenPipeException(string message, System.Net.Http.HttpResponseMessage cause) : base(message, cause)
        {
        }

        public RegistryBrokenPipeException() : base()
        {
        }
    }
}
