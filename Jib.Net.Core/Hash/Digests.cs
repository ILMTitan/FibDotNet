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

using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using System.Collections.Generic;
using System.IO;

namespace com.google.cloud.tools.jib.hash
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
        public static DescriptorDigest computeJsonDigest(JsonTemplate template)
        {
            return computeDigest(template, Stream.Null).getDigest();
        }

        public static DescriptorDigest computeJsonDigest(IReadOnlyList<JsonTemplate> templates)
        {
            WritableContents contents = contentsOut => JsonTemplateMapper.writeTo(templates, contentsOut);
            return computeDigest(contents, Stream.Null).getDigest();
        }

        public static BlobDescriptor computeDigest(JsonTemplate template)
        {
            return computeDigest(template, Stream.Null);
        }

        public static BlobDescriptor computeDigest(JsonTemplate template, Stream outStream)
        {
            WritableContents contents = contentsOut => JsonTemplateMapper.writeTo(template, contentsOut);
            return computeDigest(contents, outStream);
        }

        public static BlobDescriptor computeDigest(Stream inStream)
        {
            return computeDigest(inStream, Stream.Null);
        }

        /**
         * Computes the digest by consuming the contents.
         *
         * @param contents the contents for which the digest is computed
         * @return computed digest and bytes consumed
         * @throws IOException if reading fails
         */
        public static BlobDescriptor computeDigest(WritableContents contents)
        {
            return computeDigest(contents, Stream.Null);
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
        public static BlobDescriptor computeDigest(Stream inStream, Stream outStream)
        {
            WritableContents contents = contentsOut => ByteStreams.copy(inStream, contentsOut);
            return computeDigest(contents, outStream);
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
        public static BlobDescriptor computeDigest(WritableContents contents, Stream outStream)
        {
            CountingDigestOutputStream digestOutStream = new CountingDigestOutputStream(outStream);
            contents.writeTo(digestOutStream);
            digestOutStream.Flush();
            return digestOutStream.computeDigest();
        }
    }
}