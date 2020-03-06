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
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Events;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Hash;
using Fib.Net.Core.Http;
using Fib.Net.Core.Images.Json;
using Fib.Net.Core.Json;
using Fib.Net.Core.Registry;
using Fib.Net.Test.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fib.Net.Core.Unit.Tests.Registry
{
    /** Tests for {@link ManifestPusher}. */
    public class ManifestPusherTest
    {
        private IEventHandlers mockEventHandlers;

        private SystemPath v22manifestJsonFile;
        private V22ManifestTemplate fakeManifestTemplate;
        private ManifestPusher testManifestPusher;

        [SetUp]
        public void SetUp()
        {
            mockEventHandlers = Mock.Of<IEventHandlers>();
            v22manifestJsonFile = Paths.Get(TestResources.GetResource("core/json/v22manifest.json").ToURI());
            fakeManifestTemplate =
                JsonTemplateMapper.ReadJsonFromFile<V22ManifestTemplate>(v22manifestJsonFile);

            testManifestPusher =
                new ManifestPusher(
                    new RegistryEndpointRequestProperties("someServerUrl", "someImageName"),
                    fakeManifestTemplate,
                    "test-image-tag",
                    mockEventHandlers);
        }

        [Test]
        public async Task TestGetContentAsync()
        {
            BlobHttpContent body = testManifestPusher.GetContent();

            Assert.IsNotNull(body);
            Assert.AreEqual(V22ManifestTemplate.ManifestMediaType, body.Headers.ContentType.MediaType);

            using (MemoryStream bodyCaptureStream = new MemoryStream())
            {
                await body.CopyToAsync(bodyCaptureStream).ConfigureAwait(false);
                string v22manifestJson =
                    Encoding.UTF8.GetString(Files.ReadAllBytes(v22manifestJsonFile));
                Assert.AreEqual(
                    v22manifestJson, Encoding.UTF8.GetString(bodyCaptureStream.ToArray()));
            }
        }

        [Test]
        public async Task TestHandleResponse_validAsync()
        {
            DescriptorDigest expectedDigest = await Digests.ComputeJsonDigestAsync(fakeManifestTemplate).ConfigureAwait(false);
            using (HttpResponseMessage mockResponse = new HttpResponseMessage
            {
                Headers = { { "Docker-Content-Digest", new List<string> { expectedDigest.ToString() } } }
            })
            {
                Assert.AreEqual(expectedDigest, await testManifestPusher.HandleResponseAsync(mockResponse).ConfigureAwait(false));
            }
        }

        [Test]
        public async Task TestHandleResponse_noDigestAsync()
        {
            DescriptorDigest expectedDigest = await Digests.ComputeJsonDigestAsync(fakeManifestTemplate).ConfigureAwait(false);
            using (HttpResponseMessage mockResponse = new HttpResponseMessage
            {
                Headers = { { "Docker-Content-Digest", new List<string>() } }
            })
            {
                Assert.AreEqual(expectedDigest, await testManifestPusher.HandleResponseAsync(mockResponse).ConfigureAwait(false));
                Mock.Get(mockEventHandlers).Verify(m => m.Dispatch(LogEvent.Warn("Expected image digest " + expectedDigest + ", but received none")));
            }
        }

        [Test]
        public async Task TestHandleResponse_multipleDigestsAsync()
        {
            DescriptorDigest expectedDigest = await Digests.ComputeJsonDigestAsync(fakeManifestTemplate).ConfigureAwait(false);
            using (HttpResponseMessage mockResponse = new HttpResponseMessage
            {
                Headers = { { "Docker-Content-Digest", new[] { "too", "many" } } }
            })
            {
                Assert.AreEqual(expectedDigest, await testManifestPusher.HandleResponseAsync(mockResponse).ConfigureAwait(false));
                Mock.Get(mockEventHandlers).Verify(m => m.Dispatch(
                        LogEvent.Warn("Expected image digest " + expectedDigest + ", but received: too, many")));
            }
        }

        [Test]
        public async Task TestHandleResponse_invalidDigestAsync()
        {
            DescriptorDigest expectedDigest = await Digests.ComputeJsonDigestAsync(fakeManifestTemplate).ConfigureAwait(false);
            using (HttpResponseMessage mockResponse = new HttpResponseMessage
            {
                Headers = { { "Docker-Content-Digest", new List<string> { "not valid" } } }
            })
            {
                Assert.AreEqual(expectedDigest, await testManifestPusher.HandleResponseAsync(mockResponse).ConfigureAwait(false));
                Mock.Get(mockEventHandlers).Verify(m => m.Dispatch(
                        LogEvent.Warn("Expected image digest " + expectedDigest + ", but received: not valid")));
            }
        }

        [Test]
        public void TestApiRoute()
        {
            Assert.AreEqual(
                new Uri("http://someApiBase/someImageName/manifests/test-image-tag"),
                testManifestPusher.GetApiRoute("http://someApiBase/"));
        }

        [Test]
        public void TestGetHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Put, testManifestPusher.GetHttpMethod());
        }

        [Test]
        public void TestGetActionDescription()
        {
            Assert.AreEqual(
                "push image manifest for someServerUrl/someImageName:test-image-tag",
                testManifestPusher.GetActionDescription());
        }

        [Test]
        public void TestGetAccept()
        {
            Assert.AreEqual(0, testManifestPusher.GetAccept().Count);
        }

        /** Docker Registry 2.0 and 2.1 return 400 / TAG_INVALID. */
        [Test]
        public async Task TestHandleHttpResponseException_dockerRegistry_tagInvalidAsync()
        {
            using (HttpResponseMessage exception = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"errors\":[{\"code\":\"TAG_INVALID\","
                            + "\"message\":\"manifest tag did not match URI\"}]}")
            })
            {
                try
                {
                    await testManifestPusher.HandleHttpResponseExceptionAsync(exception).ConfigureAwait(false);
                    Assert.Fail();
                }
                catch (RegistryErrorException ex)
                {
                    Assert.That(
                        ex.Message, Does.Contain(
                            "Registry may not support pushing OCI Manifest or "
                                + "Docker Image Manifest Version 2, Schema 2"));
                }
            }
        }

        /** Docker Registry 2.2 returns a 400 / MANIFEST_INVALID. */
        [Test]
        public async Task TestHandleHttpResponseException_dockerRegistry_manifestInvalidAsync()
        {
            using (HttpResponseMessage exception = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"errors\":[{\"code\":\"MANIFEST_INVALID\"," +
                    "\"message\":\"manifest invalid\",\"detail\":{}}]}")
            })
            {
                try
                {
                    await testManifestPusher.HandleHttpResponseExceptionAsync(exception).ConfigureAwait(false);
                    Assert.Fail();
                }
                catch (RegistryErrorException ex)
                {
                    Assert.That(
                        ex.Message, Does.Contain(
                            "Registry may not support pushing OCI Manifest or "
                                + "Docker Image Manifest Version 2, Schema 2"));
                }
            }
        }

        /** Quay.io returns an undocumented 415 / MANIFEST_INVALID. */
        [Test]
        public async Task TestHandleHttpResponseException_quayIoAsync()
        {
            using (HttpResponseMessage exception = new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType)
            {
                Content = new StringContent("{\"errors\":[{\"code\":\"MANIFEST_INVALID\","
                    + "\"detail\":{\"message\":\"manifest schema version not supported\"},"
                    + "\"message\":\"manifest invalid\"}]}")
            })
            {
                try
                {
                    await testManifestPusher.HandleHttpResponseExceptionAsync(exception).ConfigureAwait(false);
                    Assert.Fail();
                }
                catch (RegistryErrorException ex)
                {
                    Assert.That(
                        ex.Message, Does.Contain(
                            "Registry may not support pushing OCI Manifest or "
                                + "Docker Image Manifest Version 2, Schema 2"));
                }
            }
        }

        [Test]
        public async Task TestHandleHttpResponseException_otherErrorAsync()
        {
            using (HttpResponseMessage exception = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"errors\":[{\"code\":\"UNAUTHORIZED\",\"message\":\"Unauthorized\"]}}")
            })
            {
                try
                {
                    await testManifestPusher.HandleHttpResponseExceptionAsync(exception).ConfigureAwait(false);
                    Assert.Fail();
                }
                catch (HttpResponseException ex)
                {
                    Assert.AreSame(exception, ex.Cause);
                }
            }
        }
    }
}
