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

using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.hash;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images.Json;
using Jib.Net.Core.Registry;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Integration tests for {@link ManifestPusher}. */
    public class ManifestPusherIntegrationTest : HttpRegistryTest
    {
        private static readonly EventHandlers EVENT_HANDLERS = EventHandlers.NONE;

        [Test]
        public async Task TestPush_missingBlobsAsync()
        {
            localRegistry.PullAndPushToLocal("busybox", "busybox");

            RegistryClient registryClient =
                RegistryClient.CreateFactory(EVENT_HANDLERS, "gcr.io", "distroless/java").NewRegistryClient();
            IManifestTemplate manifestTemplate = await registryClient.PullManifestAsync("latest").ConfigureAwait(false);

            registryClient =
                RegistryClient.CreateFactory(EVENT_HANDLERS, "localhost:5000", "busybox")
                    .SetAllowInsecureRegistries(true)
                    .NewRegistryClient();
            try
            {
                await registryClient.PushManifestAsync((V22ManifestTemplate)manifestTemplate, "latest").ConfigureAwait(false);
                Assert.Fail("Pushing manifest without its BLOBs should fail");
            }
            catch (RegistryErrorException ex)
            {
                HttpResponseMessage httpResponse = ex.Cause;
                Assert.AreEqual(
                    HttpStatusCode.BadRequest, httpResponse.StatusCode);
            }
        }

        /** Tests manifest pushing. This test is a comprehensive test of push and pull. */
        [Test]
        public async Task TestPushAsync()
        {
            localRegistry.PullAndPushToLocal("busybox", "busybox");
            IBlob testLayerBlob = Blobs.From("crepecake");
            // Known digest for 'crepecake'
            DescriptorDigest testLayerBlobDigest =
                DescriptorDigest.FromHash(
                    "52a9e4d4ba4333ce593707f98564fee1e6d898db0d3602408c0b2a6a424d357c");
            IBlob testContainerConfigurationBlob = Blobs.From("12345");
            DescriptorDigest testContainerConfigurationBlobDigest =
                DescriptorDigest.FromHash(
                    "5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5");

            // Creates a valid image manifest.
            V22ManifestTemplate expectedManifestTemplate = new V22ManifestTemplate();
            expectedManifestTemplate.AddLayer(9, testLayerBlobDigest);
            expectedManifestTemplate.SetContainerConfiguration(5, testContainerConfigurationBlobDigest);

            // Pushes the BLOBs.
            RegistryClient registryClient =
                RegistryClient.CreateFactory(EVENT_HANDLERS, "localhost:5000", "testimage")
                    .SetAllowInsecureRegistries(true)
                    .NewRegistryClient();
            Assert.IsFalse(
                await registryClient.PushBlobAsync(testLayerBlobDigest, testLayerBlob, null, _ => { }).ConfigureAwait(false));
            Assert.IsFalse(
                await registryClient.PushBlobAsync(
                    testContainerConfigurationBlobDigest,
                    testContainerConfigurationBlob,
                    null,
                    _ => { }).ConfigureAwait(false));

            // Pushes the manifest.
                DescriptorDigest imageDigest = await registryClient.PushManifestAsync(expectedManifestTemplate, "latest").ConfigureAwait(false);

            // Pulls the manifest.
            V22ManifestTemplate manifestTemplate =
                await registryClient.PullManifestAsync<V22ManifestTemplate>("latest").ConfigureAwait(false);
            Assert.AreEqual(1, manifestTemplate.Layers.Size());
            Assert.AreEqual(testLayerBlobDigest, manifestTemplate.Layers[0].Digest);
            Assert.IsNotNull(manifestTemplate.GetContainerConfiguration());
            Assert.AreEqual(
                testContainerConfigurationBlobDigest,
                manifestTemplate.GetContainerConfiguration().Digest);

            // Pulls the manifest by digest.
            V22ManifestTemplate manifestTemplateByDigest =
                await registryClient.PullManifestAsync<V22ManifestTemplate>(JavaExtensions.ToString(imageDigest)).ConfigureAwait(false);
            Assert.AreEqual(
                await Digests.ComputeJsonDigestAsync(manifestTemplate).ConfigureAwait(false),
                await Digests.ComputeJsonDigestAsync(manifestTemplateByDigest).ConfigureAwait(false));
        }
    }
}
