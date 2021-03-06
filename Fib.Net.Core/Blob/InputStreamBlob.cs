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

using Fib.Net.Core.Hash;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Core.Blob
{
    /** A {@link Blob} that holds an {@link InputStream}. */
    internal class InputStreamBlob : IBlob
    {
        private readonly Stream inputStream;

        /** Indicates if the {@link Blob} has already been written or not. */
        private bool isWritten = false;

        public InputStreamBlob(Stream inputStream, long size)
        {
            this.inputStream = inputStream;
            Size = size;
        }

        public long Size { get; }

        public async Task<BlobDescriptor> WriteToAsync(Stream outputStream)
        {
            // Cannot rewrite.
            if (isWritten)
            {
                throw new InvalidOperationException(Resources.InputStreamBlobRewriteExceptionMessage);
            }
            try
            {
                using (Stream inputStream = this.inputStream)
                {
                    return await Digests.ComputeDigestAsync(inputStream, outputStream).ConfigureAwait(false);
                }
            }
            finally
            {
                isWritten = true;
            }
        }
    }
}
