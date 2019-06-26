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
using System.Text;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Tests for {@link BlobPusher}. */
    public class BlobPusherTest : IDisposable
    {
        private const string TEST_BLOB_CONTENT = "some BLOB content";
        private static readonly IBlob TEST_BLOB = Blobs.From(TEST_BLOB_CONTENT);

        private readonly Uri mockURL = new Uri("mock://someServerUrl/someImageName");
        private HttpResponseMessage mockResponse = new HttpResponseMessage();

        private DescriptorDigest fakeDescriptorDigest;
        private BlobPusher testBlobPusher;

        [SetUp]
        public void SetUpFakes()
        {
            fakeDescriptorDigest =
                DescriptorDigest.FromHash(
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
        public void TestInitializer_getContent()
        {
            Assert.IsNull(testBlobPusher.CreateInitializer().GetContent());
        }

        [Test]
        public void TestGetAccept()
        {
            Assert.AreEqual(0, testBlobPusher.CreateInitializer().GetAccept().Count);
        }

        [Test]
        public async Task TestInitializer_handleResponse_createdAsync()
        {
            mockResponse = new HttpResponseMessage(HttpStatusCode.Created);
            Assert.IsNull(await testBlobPusher.CreateInitializer().HandleResponseAsync(mockResponse).ConfigureAwait(false));
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
                await testBlobPusher.CreateInitializer().HandleResponseAsync(mockResponse).ConfigureAwait(false));
        }

        [Test]
        public async System.Threading.Tasks.Task TestInitializer_handleResponse_unrecognizedAsync()
        {
            mockResponse = new HttpResponseMessage(HttpStatusCode.Unused);
            try
            {
                await testBlobPusher.CreateInitializer().HandleResponseAsync(mockResponse).ConfigureAwait(false);
                Assert.Fail("Multiple 'Location' headers should be a registry error");
            }
            catch (RegistryErrorException ex)
            {
                Assert.That(ex.GetMessage(), Does.Contain("Received unrecognized status code Unused"));
            }
        }

        [Test]
        public void TestInitializer_getApiRoute_nullSource()
        {
            Assert.AreEqual(
                new Uri("http://someApiBase/someImageName/blobs/uploads/"),
                testBlobPusher.CreateInitializer().GetApiRoute("http://someApiBase/"));
        }

        [Test]
        public void TestInitializer_getApiRoute_sameSource()
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
                testBlobPusher.CreateInitializer().GetApiRoute("http://someApiBase/"));
        }

        [Test]
        public void TestInitializer_getHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Post, testBlobPusher.CreateInitializer().GetHttpMethod());
        }

        [Test]
        public void TestInitializer_getActionDescription()
        {
            Assert.AreEqual(
                "push BLOB for someServerUrl/someImageName with digest " + fakeDescriptorDigest,
                testBlobPusher.CreateInitializer().GetActionDescription());
        }

        [Test]
        public async System.Threading.Tasks.Task TestWriter_getContentAsync()
        {
            LongAdder byteCount = new LongAdder();
            BlobHttpContent body = testBlobPusher.CreateWriter(mockURL, byteCount.Add).GetContent();

            Assert.IsNotNull(body);
            Assert.AreEqual("application/octet-stream", body.Headers.ContentType.MediaType);

            MemoryStream byteArrayOutputStream = new MemoryStream();
            await body.WriteToAsync(byteArrayOutputStream).ConfigureAwait(false);

            Assert.AreEqual(
                TEST_BLOB_CONTENT, Encoding.UTF8.GetString(byteArrayOutputStream.ToArray()));
            Assert.AreEqual(TEST_BLOB_CONTENT.Length, byteCount.Sum());
        }

        [Test]
        public void TestWriter_GetAccept()
        {
            Assert.AreEqual(0, testBlobPusher.CreateWriter(mockURL, _ => { }).GetAccept().Count);
        }

        [Test]
        public async Task TestWriter_handleResponseAsync()
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
                await testBlobPusher.CreateWriter(mockURL, _ => { }).HandleResponseAsync(mockResponse).ConfigureAwait(false));
        }

        [Test]
        public void TestWriter_getApiRoute()
        {
            Uri fakeUrl = new Uri("http://someurl");
            Assert.AreEqual(fakeUrl, testBlobPusher.CreateWriter(fakeUrl, _ => { }).GetApiRoute(""));
        }

        [Test]
        public void TestWriter_getHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Patch, testBlobPusher.CreateWriter(mockURL, _ => { }).GetHttpMethod());
        }

        [Test]
        public void TestWriter_getActionDescription()
        {
            Assert.AreEqual(
                "push BLOB for someServerUrl/someImageName with digest " + fakeDescriptorDigest,
                testBlobPusher.CreateWriter(mockURL, _ => { }).GetActionDescription());
        }

        [Test]
        public void TestCommitter_getContent()
        {
            Assert.IsNull(testBlobPusher.CreateCommitter(mockURL).GetContent());
        }

        [Test]
        public void TestCommitter_GetAccept()
        {
            Assert.AreEqual(0, testBlobPusher.CreateCommitter(mockURL).GetAccept().Count);
        }

        [Test]
        public async Task TestCommitter_handleResponseAsync()
        {
            Assert.IsNull(await testBlobPusher.CreateCommitter(mockURL).HandleResponseAsync(Mock.Of<HttpResponseMessage>()).ConfigureAwait(false));
        }

        [Test]
        public void TestCommitter_getApiRoute()
        {
            Assert.AreEqual(
                new Uri("https://someurl?somequery=somevalue&digest=" + fakeDescriptorDigest),
                testBlobPusher.CreateCommitter(new Uri("https://someurl?somequery=somevalue")).GetApiRoute(""));
        }

        [Test]
        public void TestCommitter_getHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Put, testBlobPusher.CreateCommitter(mockURL).GetHttpMethod());
        }

        [Test]
        public void TestCommitter_getActionDescription()
        {
            Assert.AreEqual(
                "push BLOB for someServerUrl/someImageName with digest " + fakeDescriptorDigest,
                testBlobPusher.CreateCommitter(mockURL).GetActionDescription());
        }
    }
}
