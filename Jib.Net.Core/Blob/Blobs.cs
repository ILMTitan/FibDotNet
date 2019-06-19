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
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.blob
{
    /** Static methods for {@link Blob}. */
    public sealed class Blobs
    {
        public static Blob from(Stream inputStream, long size)
        {
            return new InputStreamBlob(inputStream, size);
        }

        public static Blob from(byte[] bytes)
        {
            return new BytesBlob(bytes);
        }

        public static Blob from(SystemPath file)
        {
            return new FileBlob(file);
        }

        public static Blob fromJson(object template)
        {
            return new JsonBlob(template);
        }

        /**
         * Creates a {@link StringBlob} with UTF-8 encoding.
         *
         * @param content the string to create the blob from
         * @return the {@link StringBlob}
         */
        public static Blob from(string content)
        {
            return new StringBlob(content);
        }

        public static Blob from(WritableContentsAsync writable, long size)
        {
            return new AsyncWritableContentsBlob(writable, size);
        }

        /**
         * Writes the BLOB to a string with UTF-8 decoding.
         *
         * @param blob the BLOB to write
         * @return the BLOB contents as a string
         * @throws IOException if writing out the BLOB contents fails
         */
        public static async Task<string> writeToStringAsync(Blob blob)
        {
            return StandardCharsets.UTF_8.GetString(await writeToByteArrayAsync(blob));
        }

        /**
         * Writes the BLOB to a byte array.
         *
         * @param blob the BLOB to write
         * @return the BLOB contents as a byte array
         * @throws IOException if writing out the BLOB contents fails
         */
        public static async Task<byte[]> writeToByteArrayAsync(Blob blob)
        {
            MemoryStream byteArrayOutputStream = new MemoryStream();
            await blob.writeToAsync(byteArrayOutputStream);
            return byteArrayOutputStream.toByteArray();
        }

        private Blobs() { }
    }
}
