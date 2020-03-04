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

using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Hash;
using Jib.Net.Test.Common;
using NUnit.Framework;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Jib.Net.Core.Unit.Tests.Blob
{
    /** Tests for {@link Blob}. */
    public class BlobTest
    {
        [Test]
        public async Task TestFromInputStreamAsync()
        {
            const string expected = "crepecake";
            Stream inputStream = new MemoryStream(Encoding.UTF8.GetBytes(expected));
            await VerifyBlobWriteToAsync(expected, Blobs.From(inputStream, -1)).ConfigureAwait(false);
        }

        [Test]
        public async Task TestFromFileAsync()
        {
            SystemPath fileA = Paths.Get(TestResources.GetResource("core/fileA").ToURI());
            string expected = Encoding.UTF8.GetString(Files.ReadAllBytes(fileA));
            await VerifyBlobWriteToAsync(expected, Blobs.From(fileA)).ConfigureAwait(false);
        }

        [Test]
        public async Task TestFromBytesAsync()
        {
            const string expected = "crepecake";
            byte[] content = Encoding.UTF8.GetBytes(expected);
            await VerifyBlobWriteToAsync(expected, Blobs.From(content)).ConfigureAwait(false);
        }

        [Test]
        public async Task TestFromStringAsync()
        {
            const string expected = "crepecake";
            await VerifyBlobWriteToAsync(expected, Blobs.From(expected)).ConfigureAwait(false);
        }

        [Test]
        public async Task TestFromWritableContentsAsync()
        {
            const string expected = "crepecake";

            async Task writableContents(Stream outputStream) => await outputStream.WriteAsync(Encoding.UTF8.GetBytes(expected));

            await VerifyBlobWriteToAsync(expected, Blobs.From(writableContents, -1)).ConfigureAwait(false);
        }

        /** Checks that the {@link Blob} streams the expected string. */
        private async Task VerifyBlobWriteToAsync(string expected, IBlob blob)
        {
            MemoryStream outputStream = new MemoryStream();
            BlobDescriptor blobDescriptor = await blob.WriteToAsync(outputStream).ConfigureAwait(false);

            string output = Encoding.UTF8.GetString(outputStream.ToArray());
            Assert.AreEqual(expected, output);

            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
            Assert.AreEqual(expectedBytes.Length, blobDescriptor.GetSize());

            BlobDescriptor digestDescriptor = await Digests.ComputeDigestAsync(new MemoryStream(expectedBytes)).ConfigureAwait(false);
            DescriptorDigest expectedDigest =
                digestDescriptor.GetDigest();
            Assert.AreEqual(expectedDigest, blobDescriptor.GetDigest());
        }
    }
}
