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

using Fib.Net.Core.Api;
using NUnit.Framework;
using System.IO;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using Fib.Net.Core.Blob;
using System.Text;
using Fib.Net.Core.Hash;
using Fib.Net.Core.Registry;
using Fib.Net.Test.Common;

namespace Fib.Net.Core.Unit.Tests.Registry
{
    /** Tests for {@link BlobPuller}. */
    public class BlobPullerTest : IDisposable
    {
        private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
            new RegistryEndpointRequestProperties("someServerUrl", "someImageName");

        private DescriptorDigest fakeDigest;

        private MemoryStream layerContentOutputStream;
        private CountingDigestOutputStream layerOutputStream;

        private BlobPuller testBlobPuller;

        [SetUp]
        public void SetUpFakes()
        {
            layerContentOutputStream = new MemoryStream();
            layerOutputStream = new CountingDigestOutputStream(layerContentOutputStream);

            fakeDigest =
                DescriptorDigest.FromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            testBlobPuller =
                new BlobPuller(
                    fakeRegistryEndpointRequestProperties,
                    fakeDigest,
                    layerOutputStream,
                    _ => { },
                    _ => { });
        }

        public void Dispose()
        {
            layerContentOutputStream?.Dispose();
            layerOutputStream?.Dispose();
        }

        [Test]
        public async Task TestHandleResponseAsync()
        {
            using (MemoryStream blobContent = new MemoryStream(Encoding.UTF8.GetBytes("some BLOB content")))
            {
                BlobDescriptor descriptor = await Digests.ComputeDigestAsync(blobContent).ConfigureAwait(false);
                DescriptorDigest testBlobDigest = descriptor.GetDigest();
                blobContent.Position = 0;

                using (HttpResponseMessage mockResponse = new HttpResponseMessage()
                {
                    Content = new StringContent("some BLOB content")
                })
                {
                    LongAdder byteCount = new LongAdder();
                    BlobPuller blobPuller =
                        new BlobPuller(
                            fakeRegistryEndpointRequestProperties,
                            testBlobDigest,
                            layerOutputStream,
                            size => Assert.AreEqual("some BLOB content".Length, size),
                            byteCount.Add);
                    await blobPuller.HandleResponseAsync(mockResponse).ConfigureAwait(false);
                    Assert.AreEqual(
                        "some BLOB content",
                        Encoding.UTF8.GetString(layerContentOutputStream.ToArray()));
                    Assert.AreEqual(testBlobDigest, layerOutputStream.ComputeDigest().GetDigest());
                    Assert.AreEqual("some BLOB content".Length, byteCount.Sum());
                }
            }
        }

        [Test]
        public async Task TestHandleResponse_unexpectedDigestAsync()
        {
            using (MemoryStream blobContent = new MemoryStream(Encoding.UTF8.GetBytes("some BLOB content")))
            {
                BlobDescriptor descriptor = await Digests.ComputeDigestAsync(blobContent).ConfigureAwait(false);
                DescriptorDigest testBlobDigest = descriptor.GetDigest();
                blobContent.Position = 0;

                using (HttpResponseMessage mockResponse = new HttpResponseMessage()
                {
                    Content = new StringContent("some BLOB content")
                })
                {
                    try
                    {
                        await testBlobPuller.HandleResponseAsync(mockResponse).ConfigureAwait(false);
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
                            ex.Message);
                    }
                }
            }
        }

        [Test]
        public void TestGetApiRoute()
        {
            Assert.AreEqual(
                new Uri("http://someApiBase/someImageName/blobs/" + fakeDigest),
                testBlobPuller.GetApiRoute("http://someApiBase/"));
        }

        [Test]
        public void TestGetActionDescription()
        {
            Assert.AreEqual(
                "pull BLOB for someServerUrl/someImageName with digest " + fakeDigest,
                testBlobPuller.GetActionDescription());
        }

        [Test]
        public void TestGetHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Get, testBlobPuller.GetHttpMethod());
        }

        [Test]
        public void TestGetContent()
        {
            Assert.IsNull(testBlobPuller.GetContent());
        }

        [Test]
        public void TestGetAccept()
        {
            Assert.AreEqual(0, testBlobPuller.GetAccept().Count);
        }
    }
}
