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

using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Blob;
using System.IO;

namespace com.google.cloud.tools.jib.blob
{
    /** A {@link Blob} that holds {@link JsonTemplate}. */
    internal class JsonBlob : Blob
    {
        private readonly JsonTemplate template;

        public JsonBlob(JsonTemplate template)
        {
            this.template = template;
        }

        public BlobDescriptor writeTo(Stream outputStream)
        {
            return Digests.computeDigest(template, outputStream);
        }
    }
}
