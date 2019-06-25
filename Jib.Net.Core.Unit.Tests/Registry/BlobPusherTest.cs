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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.http;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Tests for {@link BlobPusher}. */
    public class BlobPusherTest : IDisposable
    {
        private const string TEST_BLOB_CONTENT = "some BLOB content";
        private static readonly IBlob TEST_BLOB = Blobs.from(TEST_BLOB_CONTENT);

        private readonly Uri mockURL = new Uri("mock://someServerUrl/someImageName");
        private HttpResponseMessage mockResponse = new HttpResponseMessage();

        private DescriptorDigest fakeDescriptorDigest;
        private BlobPusher testBlobPusher;

        [SetUp]
        public void setUpFakes()
        {
            fakeDescriptorDigest =
                DescriptorDigest.fromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            testBlobPusher =
                new BlobPusher(
                    new RegistryEndpointRequestProperties("someServerUrl", "someImageName"),
                    fakeDescriptorDigest,
                    TEST_BLOB,
                    null);
        }

        public void Dispose()
        {
            mockResponse?.Dispose();
        }

        [Test]
        public void testInitializer_getContent()
        {
            Assert.IsNull(testBlobPusher.initializer().getContent());
        }

        [Test]
        public void testGetAccept()
        {
            Assert.AreEqual(0, testBlobPusher.initializer().getAccept().size());
        }

        [Test]
        public async Task testInitializer_handleResponse_createdAsync()
        {
            mockResponse = new HttpResponseMessage(HttpStatusCode.Created);
            Assert.IsNull(await testBlobPusher.initializer().handleResponseAsync(mockResponse).ConfigureAwait(false));
            mockResponse = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Headers = { Location = new Uri("location", UriKind.Relative) },
                RequestMessage = new HttpRequestMessage
                {
                    RequestUri = new Uri("https://someurl")
                }
            };

            Assert.AreEqual(
                new Uri("https://someurl/location"),
                await testBlobPusher.initializer().handleResponseAsync(mockResponse).ConfigureAwait(false));
        }

        [Test]
        public async System.Threading.Tasks.Task testInitializer_handleResponse_unrecognizedAsync()
        {
            mockResponse = new HttpResponseMessage(HttpStatusCode.Unused);
            try
            {
                await testBlobPusher.initializer().handleResponseAsync(mockResponse).ConfigureAwait(false);
                Assert.Fail("Multiple 'Location' headers should be a registry error");
            }
            catch (RegistryErrorException ex)
            {
                Assert.That(ex.getMessage(), Does.Contain("Received unrecognized status code Unused"));
            }
        }

        [Test]
        public void testInitializer_getApiRoute_nullSource()
        {
            Assert.AreEqual(
                new Uri("http://someApiBase/someImageName/blobs/uploads/"),
                testBlobPusher.initializer().getApiRoute("http://someApiBase/"));
        }

        [Test]
        public void testInitializer_getApiRoute_sameSource()
        {
            testBlobPusher =
                new BlobPusher(
                    new RegistryEndpointRequestProperties("someServerUrl", "someImageName"),
                    fakeDescriptorDigest,
                    TEST_BLOB,
                    "sourceImageName");

            Assert.AreEqual(
                new Uri(
                    "http://someApiBase/someImageName/blobs/uploads/?mount="
                        + fakeDescriptorDigest
                        + "&from=sourceImageName"),
                testBlobPusher.initializer().getApiRoute("http://someApiBase/"));
        }

        [Test]
        public void testInitializer_getHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Post, testBlobPusher.initializer().getHttpMethod());
        }

        [Test]
        public void testInitializer_getActionDescription()
        {
            Assert.AreEqual(
                "push BLOB for someServerUrl/someImageName with digest " + fakeDescriptorDigest,
                testBlobPusher.initializer().getActionDescription());
        }

        [Test]
        public async System.Threading.Tasks.Task testWriter_getContentAsync()
        {
            LongAdder byteCount = new LongAdder();
            BlobHttpContent body = testBlobPusher.writer(mockURL, byteCount.add).getContent();

            Assert.IsNotNull(body);
            Assert.AreEqual("application/octet-stream", body.Headers.ContentType.MediaType);

            MemoryStream byteArrayOutputStream = new MemoryStream();
            await body.writeToAsync(byteArrayOutputStream).ConfigureAwait(false);

            Assert.AreEqual(
                TEST_BLOB_CONTENT, StandardCharsets.UTF_8.GetString(byteArrayOutputStream.toByteArray()));
            Assert.AreEqual(TEST_BLOB_CONTENT.length(), byteCount.sum());
        }

        [Test]
        public void testWriter_GetAccept()
        {
            Assert.AreEqual(0, testBlobPusher.writer(mockURL, _ => { }).getAccept().size());
        }

        [Test]
        public async Task testWriter_handleResponseAsync()
        {
            UriBuilder requestUrl = new UriBuilder("https://someurl");
            mockResponse = new HttpResponseMessage
            {
                Headers =
                {
                    Location = new Uri("https://somenewurl/location")
                },
                RequestMessage = new HttpRequestMessage
                {
                    RequestUri = requestUrl.Uri
                }
            };

            Assert.AreEqual(
                new Uri("https://somenewurl/location"),
                await testBlobPusher.writer(mockURL, _ => { }).handleResponseAsync(mockResponse).ConfigureAwait(false));
        }

        [Test]
        public void testWriter_getApiRoute()
        {
            Uri fakeUrl = new Uri("http://someurl");
            Assert.AreEqual(fakeUrl, testBlobPusher.writer(fakeUrl, _ => { }).getApiRoute(""));
        }

        [Test]
        public void testWriter_getHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Patch, testBlobPusher.writer(mockURL, _ => { }).getHttpMethod());
        }

        [Test]
        public void testWriter_getActionDescription()
        {
            Assert.AreEqual(
                "push BLOB for someServerUrl/someImageName with digest " + fakeDescriptorDigest,
                testBlobPusher.writer(mockURL, _ => { }).getActionDescription());
        }

        [Test]
        public void testCommitter_getContent()
        {
            Assert.IsNull(testBlobPusher.committer(mockURL).getContent());
        }

        [Test]
        public void testCommitter_GetAccept()
        {
            Assert.AreEqual(0, testBlobPusher.committer(mockURL).getAccept().size());
        }

        [Test]
        public async Task testCommitter_handleResponseAsync()
        {
            Assert.IsNull(await testBlobPusher.committer(mockURL).handleResponseAsync(Mock.Of<HttpResponseMessage>()).ConfigureAwait(false));
        }

        [Test]
        public void testCommitter_getApiRoute()
        {
            Assert.AreEqual(
                new Uri("https://someurl?somequery=somevalue&digest=" + fakeDescriptorDigest),
                testBlobPusher.committer(new Uri("https://someurl?somequery=somevalue")).getApiRoute(""));
        }

        [Test]
        public void testCommitter_getHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Put, testBlobPusher.committer(mockURL).getHttpMethod());
        }

        [Test]
        public void testCommitter_getActionDescription()
        {
            Assert.AreEqual(
                "push BLOB for someServerUrl/someImageName with digest " + fakeDescriptorDigest,
                testBlobPusher.committer(mockURL).getActionDescription());
        }
    }
}
