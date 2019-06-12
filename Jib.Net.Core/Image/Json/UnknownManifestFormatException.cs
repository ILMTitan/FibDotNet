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

using com.google.cloud.tools.jib.api;

namespace com.google.cloud.tools.jib.image.json
{
    /** Exception thrown when trying to parse an unknown image manifest format. */
    public class UnknownManifestFormatException : RegistryException
    {
        public UnknownManifestFormatException(string message) : base(message)
        {
        }

        public UnknownManifestFormatException(string message, System.Exception cause) : base(message, cause)
        {
        }

        public UnknownManifestFormatException(string message, System.Net.Http.HttpResponseMessage cause) : base(message, cause)
        {
        }

        public UnknownManifestFormatException() : base()
        {
        }
    }
}
