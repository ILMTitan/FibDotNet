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

using com.google.cloud.tools.jib.hash;
using Jib.Net.Core.Blob;
using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.blob
{
    /** A {@link Blob} that holds {@link WritableContents}. */
    internal class AsyncWritableContentsBlob : IBlob
    {
        private readonly WritableContentsAsync writableContents;

        public AsyncWritableContentsBlob(WritableContentsAsync writableContents, long size)
        {
            this.writableContents = writableContents;
            Size = size;
        }

        public long Size { get; }

        public async Task<BlobDescriptor> writeToAsync(Stream outputStream)
        {
            return await Digests.computeDigestAsync(writableContents, outputStream).ConfigureAwait(false);
        }
    }
}
