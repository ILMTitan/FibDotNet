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

using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.hash
{
    /** A {@link DigestOutputStream} that also keeps track of the total number of bytes written. */
    public class CountingDigestOutputStream : DigestStream
    {
        public const string Sha256Algorithm = "SHA-256";

        private long bytesSoFar = 0;

        /**
         * Wraps the {@code innerStream}.
         *
         * @param innerStream the {@link OutputStream} to wrap.
         */
        public CountingDigestOutputStream(Stream outputStream, bool keepOpen = false) : base(outputStream, MessageDigest.getInstance(Sha256Algorithm), keepOpen) { }

        /**
         * Computes the hash and returns it along with the size of the bytes written to compute the hash.
         * The buffer resets after this method is called, so this method should only be called once per
         * computation.
         *
         * @return the computed hash and the size of the bytes consumed
         */
        public BlobDescriptor computeDigest()
        {
            Flush();
            try
            {
                byte[] hashedBytes = MessageDigest.digest();

                // Encodes each hashed byte into 2-character hexadecimal representation.
                StringBuilder stringBuilder = new StringBuilder(2 * hashedBytes.Length);
                foreach (byte b in hashedBytes)
                {
                    stringBuilder.append($"{b:x2}");
                }
                string hash = stringBuilder.toString();

                BlobDescriptor blobDescriptor =
                    new BlobDescriptor(bytesSoFar, DescriptorDigest.fromHash(hash));
                bytesSoFar = 0;
                return blobDescriptor;
            }
            catch (DigestException ex)
            {
                throw new Exception("SHA-256 algorithm produced invalid hash: " + ex.getMessage(), ex);
            }
        }

        public long getCount()
        {
            return bytesSoFar;
        }

        public override void Write(byte[] data, int offset, int count)
        {
            base.Write(data, offset, count);
            bytesSoFar += count;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            bytesSoFar += count;
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = base.Read(buffer, offset, count);
            bytesSoFar += bytesRead;
            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await base.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            bytesSoFar += bytesRead;
            return bytesRead;
        }
    }
}
