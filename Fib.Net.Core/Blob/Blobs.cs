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
using Fib.Net.Core.Hash;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Fib.Net.Core.Blob
{
    /** Static methods for {@link Blob}. */
    public static class Blobs
    {
        public static IBlob From(Stream inputStream, long size)
        {
            return new InputStreamBlob(inputStream, size);
        }

        public static IBlob From(byte[] bytes)
        {
            return new BytesBlob(bytes);
        }

        public static IBlob From(SystemPath file)
        {
            return new FileBlob(file);
        }

        public static IBlob FromJson(object template)
        {
            return new JsonBlob(template);
        }

        /**
         * Creates a {@link StringBlob} with UTF-8 encoding.
         *
         * @param content the string to create the blob from
         * @return the {@link StringBlob}
         */
        public static IBlob From(string content)
        {
            return new StringBlob(content);
        }

        public static IBlob From(WritableContentsAsync writable, long size)
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
        public static async Task<string> WriteToStringAsync(IBlob blob)
        {
            return Encoding.UTF8.GetString(await WriteToByteArrayAsync(blob).ConfigureAwait(false));
        }

        /**
         * Writes the BLOB to a byte array.
         *
         * @param blob the BLOB to write
         * @return the BLOB contents as a byte array
         * @throws IOException if writing out the BLOB contents fails
         */
        public static async Task<byte[]> WriteToByteArrayAsync(IBlob blob)
        {
            blob = blob ?? throw new ArgumentNullException(nameof(blob));
            using (MemoryStream byteArrayOutputStream = new MemoryStream())
            {
                await blob.WriteToAsync(byteArrayOutputStream).ConfigureAwait(false);
                return byteArrayOutputStream.ToArray();
            }
        }
    }
}
