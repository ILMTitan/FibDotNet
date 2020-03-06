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

using Fib.Net.Core.Api;
using Fib.Net.Core.Blob;
using Fib.Net.Core.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Core.Hash
{
    /**
     * Utility class for computing a digest for various inputs while optionally writing to an output
     * stream.
     */
    // Note: intentionally this class does not depend on Blob, as Blob classes depend on this class.
    // TODO: BlobDescriptor is merely a tuple of (size, digest). Rename BlobDescriptor to something
    // more general.
    public static class Digests
    {
        public static async Task<DescriptorDigest> ComputeJsonDigestAsync(object template)
        {
            var descriptor = await ComputeJsonDigestAsync(template, Stream.Null).ConfigureAwait(false);
            return descriptor.GetDigest();
        }

        public static async Task<BlobDescriptor> ComputeJsonDescriptorAsync(object template)
        {
            return await ComputeJsonDigestAsync(template, Stream.Null).ConfigureAwait(false);
        }

        public static async Task<BlobDescriptor> ComputeJsonDigestAsync(object template, Stream outStream)
        {
            async Task ContentsAsync(Stream contentsOut) =>
                await JsonTemplateMapper.WriteToAsync(template, contentsOut).ConfigureAwait(false);
            return await ComputeDigestAsync(ContentsAsync, outStream).ConfigureAwait(false);
        }

        public static async Task<BlobDescriptor> ComputeDigestAsync(Stream inStream)
        {
            return await ComputeDigestAsync(inStream, Stream.Null).ConfigureAwait(false);
        }

        /**
         * Computes the digest by consuming the contents.
         *
         * @param contents the contents for which the digest is computed
         * @return computed digest and bytes consumed
         * @throws IOException if reading fails
         */
        public static BlobDescriptor ComputeDigest(WritableContents contents)
        {
            return ComputeDigest(contents, Stream.Null);
        }

        /**
         * Computes the digest by consuming the contents.
         *
         * @param contents the contents for which the digest is computed
         * @return computed digest and bytes consumed
         * @throws IOException if reading fails
         */
        public static async Task<BlobDescriptor> ComputeDigestAsync(WritableContentsAsync contents)
        {
            return await ComputeDigestAsync(contents, Stream.Null).ConfigureAwait(false);
        }

        /**
         * Computes the digest by consuming the contents of an {@link InputStream} while copying it to an
         * {@link OutputStream}. Returns the computed digest along with the size of the bytes consumed to
         * compute the digest. Does not close either stream.
         *
         * @param inStream the stream to read the contents from
         * @param outStream the stream to which the contents are copied
         * @return computed digest and bytes consumed
         * @throws IOException if reading from or writing fails
         */
        public static async Task<BlobDescriptor> ComputeDigestAsync(Stream inStream, Stream outStream)
        {
            async Task Contents(Stream contentsOut) => await inStream.CopyToAsync(contentsOut).ConfigureAwait(false);
            return await ComputeDigestAsync(Contents, outStream).ConfigureAwait(false);
        }

        /**
         * Computes the digest by consuming the contents while copying it to an {@link OutputStream}.
         * Returns the computed digest along with the size of the bytes consumed to compute the digest.
         * Does not close the stream.
         *
         * @param contents the contents to compute digest for
         * @param outStream the stream to which the contents are copied
         * @return computed digest and bytes consumed
         * @throws IOException if reading from or writing fails
         */
        public static BlobDescriptor ComputeDigest(WritableContents contents, Stream outStream)
        {
            using (CountingDigestOutputStream digestOutStream = new CountingDigestOutputStream(outStream, true))
            {
                contents.WriteTo(digestOutStream);
                return digestOutStream.ComputeDigest();
            }
        }

        /**
         * Computes the digest by consuming the contents while copying it to an {@link OutputStream}.
         * Returns the computed digest along with the size of the bytes consumed to compute the digest.
         * Does not close the stream.
         *
         * @param contents the contents to compute digest for
         * @param outStream the stream to which the contents are copied
         * @return computed digest and bytes consumed
         * @throws IOException if reading from or writing fails
         */
        public static async Task<BlobDescriptor> ComputeDigestAsync(WritableContentsAsync contents, Stream outStream)
        {
            contents = contents ?? throw new ArgumentNullException(nameof(contents));
            using (CountingDigestOutputStream digestOutStream = new CountingDigestOutputStream(outStream, true))
            {
                await contents(digestOutStream).ConfigureAwait(false);
                return digestOutStream.ComputeDigest();
            }
        }
    }
}
