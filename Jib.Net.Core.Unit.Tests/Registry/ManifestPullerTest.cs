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
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Core.Registry;
using Jib.Net.Test.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Tests for {@link ManifestPuller}. */
    public class ManifestPullerTest : IDisposable
    {
        private HttpResponseMessage mockResponse = Mock.Of<HttpResponseMessage>();

        private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
            new RegistryEndpointRequestProperties("someServerUrl", "someImageName");

        private readonly ManifestPuller testManifestPuller;

        public ManifestPullerTest()
        {
            testManifestPuller =
         new ManifestPuller(
             fakeRegistryEndpointRequestProperties, "test-image-tag");
        }

        public void Dispose()
        {
            mockResponse?.Dispose();
        }

        [Test]
        public async Task testHandleResponse_v21Async()
        {
            SystemPath v21ManifestFile = Paths.get(TestResources.getResource("core/json/v21manifest.json").toURI());
            Stream v21Manifest = new MemoryStream(Files.readAllBytes(v21ManifestFile));

            mockResponse = new HttpResponseMessage
            {
                Content = new StreamContent(v21Manifest)
            };

            IManifestTemplate manifestTemplate =
                await new ManifestPuller<V21ManifestTemplate>(
                        fakeRegistryEndpointRequestProperties, "test-image-tag")
                    .handleResponseAsync(mockResponse).ConfigureAwait(false);

            Assert.IsInstanceOf<V21ManifestTemplate>(manifestTemplate);
        }

        [Test]
        public async Task testHandleResponse_v22Async()
        {
            SystemPath v22ManifestFile = Paths.get(TestResources.getResource("core/json/v22manifest.json").toURI());
            Stream v22Manifest = new MemoryStream(Files.readAllBytes(v22ManifestFile));
            mockResponse = new HttpResponseMessage
            {
                Content = new StreamContent(v22Manifest)
            };

            IManifestTemplate manifestTemplate =
                await new ManifestPuller<V22ManifestTemplate>(
                        fakeRegistryEndpointRequestProperties, "test-image-tag")
                    .handleResponseAsync(mockResponse).ConfigureAwait(false);

            Assert.IsInstanceOf<V22ManifestTemplate>(manifestTemplate);
        }

        [Test]
        public async Task testHandleResponse_noSchemaVersionAsync()
        {
            mockResponse = new HttpResponseMessage
            {
                Content = new StringContent("{}")
            };

            try
            {
                await testManifestPuller.handleResponseAsync(mockResponse).ConfigureAwait(false);
                Assert.Fail("An empty manifest should throw an error");
            }
            catch (UnknownManifestFormatException ex)
            {
                Assert.AreEqual("Cannot find field 'schemaVersion' in manifest", ex.getMessage());
            }
        }

        [Test]
        public async Task testHandleResponse_invalidSchemaVersionAsync()
        {
            mockResponse = new HttpResponseMessage
            {
                Content = new StringContent("{\"schemaVersion\":\"not valid\"}")
            };

            try
            {
                await testManifestPuller.handleResponseAsync(mockResponse).ConfigureAwait(false);
                Assert.Fail("A non-integer schemaVersion should throw an error");
            }
            catch (UnknownManifestFormatException ex)
            {
                Assert.AreEqual("`schemaVersion` field is not an integer", ex.getMessage());
            }
        }

        [Test]
        public async Task testHandleResponse_unknownSchemaVersionAsync()
        {
            mockResponse = new HttpResponseMessage
            {
                Content = new StringContent("{\"schemaVersion\":0}")
            };

            try
            {
                await testManifestPuller.handleResponseAsync(mockResponse).ConfigureAwait(false);
                Assert.Fail("An unknown manifest schemaVersion should throw an error");
            }
            catch (UnknownManifestFormatException ex)
            {
                Assert.AreEqual("Unknown schemaVersion: 0 - only 1 and 2 are supported", ex.getMessage());
            }
        }

        [Test]
        public void testGetApiRoute()
        {
            Assert.AreEqual(
                new Uri("http://someApiBase/someImageName/manifests/test-image-tag"),
                testManifestPuller.getApiRoute("http://someApiBase/"));
        }

        [Test]
        public void testGetHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Get, testManifestPuller.getHttpMethod());
        }

        [Test]
        public void testGetActionDescription()
        {
            Assert.AreEqual(
                "pull image manifest for someServerUrl/someImageName:test-image-tag",
                testManifestPuller.getActionDescription());
        }

        [Test]
        public void testGetContent()
        {
            Assert.IsNull(testManifestPuller.getContent());
        }

        [Test]
        public void testGetAccept()
        {
            Assert.AreEqual(
                Arrays.asList(
                    OCIManifestTemplate.ManifestMediaType,
                    V22ManifestTemplate.ManifestMediaType,
                    V21ManifestTemplate.ManifestMediaType),
                testManifestPuller.getAccept());

            CollectionAssert.AreEqual(
                new List<string> { OCIManifestTemplate.ManifestMediaType },
                new ManifestPuller<OCIManifestTemplate>(
                        fakeRegistryEndpointRequestProperties, "test-image-tag")
                    .getAccept());
            CollectionAssert.AreEqual(
                new List<string> { V22ManifestTemplate.ManifestMediaType },
                new ManifestPuller<V22ManifestTemplate>(
                        fakeRegistryEndpointRequestProperties, "test-image-tag")
                    .getAccept());
            CollectionAssert.AreEqual(
                new List<string> { V21ManifestTemplate.ManifestMediaType },
                new ManifestPuller<V21ManifestTemplate>(
                        fakeRegistryEndpointRequestProperties, "test-image-tag")
                    .getAccept());
        }
    }
}
