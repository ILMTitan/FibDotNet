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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.Net;

namespace com.google.cloud.tools.jib.registry
{






    /** Integration tests for {@link ManifestPusher}. */
    public class ManifestPusherIntegrationTest
    {
        [ClassRule] public static LocalRegistry localRegistry = new LocalRegistry(5000);
        private static readonly EventHandlers EVENT_HANDLERS = EventHandlers.NONE;

        [Test]
        public void testPush_missingBlobs()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");

            RegistryClient registryClient =
                RegistryClient.factory(EVENT_HANDLERS, "gcr.io", "distroless/java").newRegistryClient();
            ManifestTemplate manifestTemplate = registryClient.pullManifest("latest");

            registryClient =
                RegistryClient.factory(EVENT_HANDLERS, "localhost:5000", "busybox")
                    .setAllowInsecureRegistries(true)
                    .newRegistryClient();
            try
            {
                registryClient.pushManifest((V22ManifestTemplate)manifestTemplate, "latest");
                Assert.Fail("Pushing manifest without its BLOBs should fail");
            }
            catch (RegistryErrorException ex)
            {
                HttpResponseException httpResponseException = (HttpResponseException)ex.getCause();
                Assert.AreEqual(
                    HttpStatusCode.BadRequest, httpResponseException.getStatusCode());
            }
        }

        /** Tests manifest pushing. This test is a comprehensive test of push and pull. */
        [Test]
        public void testPush()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            Blob testLayerBlob = Blobs.from("crepecake");
            // Known digest for 'crepecake'
            DescriptorDigest testLayerBlobDigest =
                DescriptorDigest.fromHash(
                    "52a9e4d4ba4333ce593707f98564fee1e6d898db0d3602408c0b2a6a424d357c");
            Blob testContainerConfigurationBlob = Blobs.from("12345");
            DescriptorDigest testContainerConfigurationBlobDigest =
                DescriptorDigest.fromHash(
                    "5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5");

            // Creates a valid image manifest.
            V22ManifestTemplate expectedManifestTemplate = new V22ManifestTemplate();
            expectedManifestTemplate.addLayer(9, testLayerBlobDigest);
            expectedManifestTemplate.setContainerConfiguration(5, testContainerConfigurationBlobDigest);

            // Pushes the BLOBs.
            RegistryClient registryClient =
                RegistryClient.factory(EVENT_HANDLERS, "localhost:5000", "testimage")
                    .setAllowInsecureRegistries(true)
                    .newRegistryClient();
            Assert.IsFalse(
                registryClient.pushBlob(testLayerBlobDigest, testLayerBlob, null, ignored => { }));
            Assert.IsFalse(
                registryClient.pushBlob(
                    testContainerConfigurationBlobDigest,
                    testContainerConfigurationBlob,
                    null,
                    ignored => { }));

            // Pushes the manifest.
            DescriptorDigest imageDigest = registryClient.pushManifest(expectedManifestTemplate, "latest");

            // Pulls the manifest.
            V22ManifestTemplate manifestTemplate =
                registryClient.pullManifest<V22ManifestTemplate>("latest");
            Assert.AreEqual(1, manifestTemplate.getLayers().size());
            Assert.AreEqual(testLayerBlobDigest, manifestTemplate.getLayers().get(0).getDigest());
            Assert.IsNotNull(manifestTemplate.getContainerConfiguration());
            Assert.AreEqual(
                testContainerConfigurationBlobDigest,
                manifestTemplate.getContainerConfiguration().getDigest());

            // Pulls the manifest by digest.
            V22ManifestTemplate manifestTemplateByDigest =
                registryClient.pullManifest<V22ManifestTemplate>(imageDigest.toString());
            Assert.AreEqual(
                Digests.computeJsonDigest(manifestTemplate),
                Digests.computeJsonDigest(manifestTemplateByDigest));
        }
    }
}
