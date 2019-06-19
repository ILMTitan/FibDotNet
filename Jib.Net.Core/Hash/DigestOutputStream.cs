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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.hash
{
    public class DigestStream : Stream
    {
        private readonly Stream innerStream;
        protected MessageDigest messageDigest;
        private readonly bool keepOpen;

        public DigestStream(Stream innerStream, MessageDigest messageDigest, bool keepOpen = false)
        {
            this.innerStream = innerStream;
            this.messageDigest = messageDigest;
            this.keepOpen = keepOpen;
        }

        public override bool CanRead => innerStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => innerStream.CanWrite;

        /// <exception cref="NotSupportedException">A class derived from Stream does not support seeking.</exception>
        public override long Length => throw new NotSupportedException();

        /// <exception cref="NotSupportedException">The stream does not support seeking.</exception>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public void setMessageDigest(MessageDigest messageDigest)
        {
            this.messageDigest = messageDigest;
        }

        public override void Flush()
        {
            innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = innerStream.Read(buffer, offset, count);
            return messageDigest.TransformBlock(buffer, offset, bytesRead, buffer, offset);
        }

        /// <exception cref="NotSupportedException">The stream does not support seeking.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <exception cref="NotSupportedException">The stream does not support seeking.</exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var newCount = messageDigest.TransformBlock(buffer, offset, count, buffer, offset);
            innerStream.Write(buffer, offset, newCount);
        }

        public async override Task FlushAsync(CancellationToken cancellationToken)
        {
            await innerStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await innerStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            return messageDigest.TransformBlock(buffer, offset, bytesRead, buffer, offset);
        }

        public async override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var newCount = messageDigest.TransformBlock(buffer, offset, count, buffer, offset);
            await innerStream.WriteAsync(buffer, offset, newCount, cancellationToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (!keepOpen)
                {
                    innerStream.Dispose();
                }
            }
        }
    }
}