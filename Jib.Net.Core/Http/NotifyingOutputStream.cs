/*
 * Copyright 2018 Google LLC.
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

using Jib.Net.Core.Api;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.http
{
    /** Counts the number of bytes written and reports the count to a callback. */
    public class NotifyingOutputStream : Stream
    {
        /** The underlying {@link OutputStream} to wrap and forward bytes to. */
        private readonly Stream underlyingOutputStream;

        /** Receives a count of bytes written since the last call. */
        private readonly Consumer<long> byteCountListener;

        /** Number of bytes to provide to {@link #byteCountListener}. */
        private long byteCount = 0;

        public override bool CanRead => underlyingOutputStream.CanRead;

        public override bool CanSeek => underlyingOutputStream.CanSeek;

        public override bool CanWrite => underlyingOutputStream.CanWrite;

        public override long Length => underlyingOutputStream.Length;

        public override long Position { get => underlyingOutputStream.Position; set => underlyingOutputStream.Position = value; }

        /**
         * Wraps the {@code underlyingOutputStream} to count the bytes written.
         *
         * @param underlyingOutputStream the wrapped {@link OutputStream}
         * @param byteCountListener the byte count {@link Consumer}
         */
        public NotifyingOutputStream(
      Stream underlyingOutputStream, Consumer<long> byteCountListener)
        {
            this.underlyingOutputStream = underlyingOutputStream;
            this.byteCountListener = byteCountListener;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                underlyingOutputStream.Dispose();
                countAndCallListener(0);
            }
        }

        private void countAndCallListener(int written)
        {
            this.byteCount += written;
            if (byteCount == 0)
            {
                return;
            }

            byteCountListener.accept(byteCount);
            byteCount = 0;
        }

        public override void Flush()
        {
            underlyingOutputStream.Flush();
            countAndCallListener(0);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = underlyingOutputStream.Read(buffer, offset, count);
            countAndCallListener(bytesRead);
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return underlyingOutputStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            underlyingOutputStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            underlyingOutputStream.Write(buffer, offset, count);
            countAndCallListener(count);
        }
    }
}
