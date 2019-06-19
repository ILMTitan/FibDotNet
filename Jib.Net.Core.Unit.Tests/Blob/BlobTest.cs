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

using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.hash;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.blob
{







    /** Tests for {@link Blob}. */
    public class BlobTest
    {
        [Test]
        public async Task testFromInputStreamAsync()
        {
            const string expected = "crepecake";
            Stream inputStream = new MemoryStream(expected.getBytes(StandardCharsets.UTF_8));
            await verifyBlobWriteToAsync(expected, Blobs.from(inputStream, -1));
        }

        [Test]
        public async Task testFromFileAsync()
        {
            SystemPath fileA = Paths.get(Resources.getResource("core/fileA").toURI());
            string expected = StandardCharsets.UTF_8.GetString(Files.readAllBytes(fileA));
            await verifyBlobWriteToAsync(expected, Blobs.from(fileA));
        }

        [Test]
        public async Task testFromBytesAsync()
        {
            const string expected = "crepecake";
            byte[] content = (expected.getBytes(StandardCharsets.UTF_8));
            await verifyBlobWriteToAsync(expected, Blobs.from(content));
        }

        [Test]
        public async Task testFromStringAsync()
        {
            const string expected = "crepecake";
            await verifyBlobWriteToAsync(expected, Blobs.from(expected));
        }

        [Test]
        public async Task testFromWritableContentsAsync()
        {
            const string expected = "crepecake";

            WritableContentsAsync writableContents =
                async outputStream => await outputStream.WriteAsync(expected.getBytes(StandardCharsets.UTF_8));

            await verifyBlobWriteToAsync(expected, Blobs.from(writableContents, -1));
        }

        /** Checks that the {@link Blob} streams the expected string. */
        private async System.Threading.Tasks.Task verifyBlobWriteToAsync(string expected, Blob blob)
        {
            MemoryStream outputStream = new MemoryStream();
            BlobDescriptor blobDescriptor = await blob.writeToAsync(outputStream);

            string output = StandardCharsets.UTF_8.GetString(outputStream.ToArray());
            Assert.AreEqual(expected, output);

            byte[] expectedBytes = expected.getBytes(StandardCharsets.UTF_8);
            Assert.AreEqual(expectedBytes.Length, blobDescriptor.getSize());

            BlobDescriptor digestDescriptor = await Digests.computeDigestAsync(new MemoryStream(expectedBytes));
            DescriptorDigest expectedDigest =
                digestDescriptor.getDigest();
            Assert.AreEqual(expectedDigest, blobDescriptor.getDigest());
        }
    }
}
