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

using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.hash;
using Jib.Net.Core.Global;
using Jib.Net.Core.Api;
using NUnit.Framework;
using System.IO;
using System.Net.Http;
using Moq;
using System;
using System.Threading.Tasks;
using Jib.Net.Core.Blob;

namespace com.google.cloud.tools.jib.registry
{
    /** Tests for {@link BlobPuller}. */
    public class BlobPullerTest
    {
        private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
            new RegistryEndpointRequestProperties("someServerUrl", "someImageName");

        private DescriptorDigest fakeDigest;

        private MemoryStream layerContentOutputStream;
        private CountingDigestOutputStream layerOutputStream;

        private BlobPuller testBlobPuller;

        [SetUp]
        public void setUpFakes()
        {
            layerContentOutputStream = new MemoryStream();
            layerOutputStream = new CountingDigestOutputStream(layerContentOutputStream);

            fakeDigest =
                DescriptorDigest.fromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            testBlobPuller =
                new BlobPuller(
                    fakeRegistryEndpointRequestProperties,
                    fakeDigest,
                    layerOutputStream,
                    _ => { },
                    _ => { });
        }

        [Test]
        public async Task testHandleResponseAsync()
        {
            MemoryStream blobContent = new MemoryStream("some BLOB content".getBytes(StandardCharsets.UTF_8));
            BlobDescriptor descriptor = await Digests.computeDigestAsync(blobContent).ConfigureAwait(false);
            DescriptorDigest testBlobDigest = descriptor.getDigest();
            blobContent.Position = 0;

            HttpResponseMessage mockResponse = new HttpResponseMessage()
            {
                Content = new StringContent("some BLOB content")
            };
            LongAdder byteCount = new LongAdder();
            BlobPuller blobPuller =
                new BlobPuller(
                    fakeRegistryEndpointRequestProperties,
                    testBlobDigest,
                    layerOutputStream,
                    size => Assert.AreEqual("some BLOB content".length(), size.longValue()),
                    byteCount.add);
            await blobPuller.handleResponseAsync(mockResponse).ConfigureAwait(false);
            Assert.AreEqual(
                "some BLOB content",
                StandardCharsets.UTF_8.GetString(layerContentOutputStream.toByteArray()));
            Assert.AreEqual(testBlobDigest, layerOutputStream.computeDigest().getDigest());
            Assert.AreEqual("some BLOB content".length(), byteCount.sum());
        }

        [Test]
        public async Task testHandleResponse_unexpectedDigestAsync()
        {
            MemoryStream blobContent = new MemoryStream("some BLOB content".getBytes(StandardCharsets.UTF_8));
            BlobDescriptor descriptor = await Digests.computeDigestAsync(blobContent).ConfigureAwait(false);
            DescriptorDigest testBlobDigest = descriptor.getDigest();
            blobContent.Position = 0;

            HttpResponseMessage mockResponse = new HttpResponseMessage()
            {
                Content = new StringContent("some BLOB content")
            };

            try
            {
                await testBlobPuller.handleResponseAsync(mockResponse).ConfigureAwait(false);
                Assert.Fail("Receiving an unexpected digest should fail");
            }
            catch (UnexpectedBlobDigestException ex)
            {
                Assert.AreEqual(
                    "The pulled BLOB has digest '"
                        + testBlobDigest
                        + "', but the request digest was '"
                        + fakeDigest
                        + "'",
                    ex.getMessage());
            }
        }

        [Test]
        public void testGetApiRoute()
        {
            Assert.AreEqual(
                new Uri("http://someApiBase/someImageName/blobs/" + fakeDigest),
                testBlobPuller.getApiRoute("http://someApiBase/"));
        }

        [Test]
        public void testGetActionDescription()
        {
            Assert.AreEqual(
                "pull BLOB for someServerUrl/someImageName with digest " + fakeDigest,
                testBlobPuller.getActionDescription());
        }

        [Test]
        public void testGetHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Get, testBlobPuller.getHttpMethod());
        }

        [Test]
        public void testGetContent()
        {
            Assert.IsNull(testBlobPuller.getContent());
        }

        [Test]
        public void testGetAccept()
        {
            Assert.AreEqual(0, testBlobPuller.getAccept().size());
        }
    }
}
