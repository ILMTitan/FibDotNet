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
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.FileSystem;
using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.blob
{
    /** A {@link Blob} that holds a {@link Path}. */
    internal class FileBlob : IBlob
    {
        private readonly SystemPath file;

        public FileBlob(SystemPath file)
        {
            this.file = file;
        }

        public long Size => new FileInfo(file).Length;

        public async Task<BlobDescriptor> WriteToAsync(Stream outputStream)
        {
            using (Stream fileIn = new BufferedStream(Files.NewInputStream(file)))
            {
                return await Digests.ComputeDigestAsync(fileIn, outputStream).ConfigureAwait(false);
            }
        }
    }
}
