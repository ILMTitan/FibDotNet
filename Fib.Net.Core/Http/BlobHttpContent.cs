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

using Fib.Net.Core.Blob;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Fib.Net.Core.Http
{
    /** {@link Blob}-backed {@link HttpContent}. */
    public class BlobHttpContent : HttpContent
    {
        private readonly IBlob blob;
        private readonly Action<long> writtenByteCountListener;

        public BlobHttpContent(IBlob blob, string contentType) : this(blob, contentType, _ => { })
        {
        }

        public BlobHttpContent(IBlob blob, string contentType, Action<long> writtenByteCountListener)
        {
            this.blob = blob;
            Headers.ContentType = new MediaTypeHeaderValue(contentType);
            this.writtenByteCountListener = writtenByteCountListener;
        }

        public long GetLength()
        {
            // Returns negative value for unknown length.
            return blob.Size;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            stream = stream ?? throw new ArgumentNullException(nameof(stream));
            using (NotifyingOutputStream outputStream = new NotifyingOutputStream(stream, writtenByteCountListener, true))
            {
                await blob.WriteToAsync(outputStream).ConfigureAwait(false);
                await outputStream.FlushAsync().ConfigureAwait(false);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = GetLength();
            return length >= 0;
        }
    }
}
