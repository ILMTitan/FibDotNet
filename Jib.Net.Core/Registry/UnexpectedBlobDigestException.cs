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

namespace com.google.cloud.tools.jib.registry {


/** Thrown when a pulled BLOB did not have the same digest as requested. */
class UnexpectedBlobDigestException : RegistryException {

  public UnexpectedBlobDigestException(string message) : base(message) {
    
  }

        public UnexpectedBlobDigestException(string message, System.Exception cause) : base(message, cause)
        {
        }

        public UnexpectedBlobDigestException(string message, System.Net.Http.HttpResponseMessage cause) : base(message, cause)
        {
        }

        public UnexpectedBlobDigestException() : base()
        {
        }
    }
}
