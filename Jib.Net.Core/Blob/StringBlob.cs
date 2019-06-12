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

using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.hash;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using System.IO;

namespace com.google.cloud.tools.jib.blob
{
    /** A {@link Blob} that holds a {@link string}. Encodes in UTF-8 when writing in bytes. */
    internal class StringBlob : Blob
    {
        private readonly string content;

        public StringBlob(string content)
        {
            this.content = content;
        }

        public BlobDescriptor writeTo(Stream outputStream)
        {
            using (Stream stringIn =
                new MemoryStream(content.getBytes(StandardCharsets.UTF_8)))
            {
                return Digests.computeDigest(stringIn, outputStream);
            }
        }
    }
}
