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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using com.google.cloud.tools.jib.hash;
using Jib.Net.Core.Blob;

namespace com.google.cloud.tools.jib.blob
{
    internal class BytesBlob : Blob
    {
        private readonly byte[] _bytes;
        protected IReadOnlyList<byte> bytes => _bytes;

        public BytesBlob(byte[] bytes)
        {
            _bytes = bytes;
        }

        public long Size => _bytes.LongLength;

        public Task<BlobDescriptor> writeToAsync(Stream outputStream)
        {
            return Digests.computeDigestAsync(s => s.WriteAsync(_bytes, 0, _bytes.Length), outputStream);
        }
    }
}