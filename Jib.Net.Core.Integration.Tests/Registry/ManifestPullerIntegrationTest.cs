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
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Global;
using Jib.Net.Core.Registry;
using NUnit.Framework;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Integration tests for {@link ManifestPuller}. */
    public class ManifestPullerIntegrationTest : HttpRegistryTest
    {
        [Test]
        public async System.Threading.Tasks.Task TestPull_v21Async()
        {
            localRegistry.PullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.CreateFactory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .SetAllowInsecureRegistries(true)
                    .NewRegistryClient();
            V21ManifestTemplate manifestTemplate =
                await registryClient.PullManifestAsync<V21ManifestTemplate>("latest").ConfigureAwait(false);

            Assert.AreEqual(1, manifestTemplate.SchemaVersion);
            Assert.IsTrue(manifestTemplate.FsLayers.Size() > 0);
        }

        [Test]
        public async Task TestPull_v22Async()
        {
            localRegistry.PullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.CreateFactory(EventHandlers.NONE, "gcr.io", "distroless/java").NewRegistryClient();
            IManifestTemplate manifestTemplate = await registryClient.PullManifestAsync("latest").ConfigureAwait(false);

            Assert.AreEqual(2, manifestTemplate.SchemaVersion);
            V22ManifestTemplate v22ManifestTemplate = (V22ManifestTemplate)manifestTemplate;
            Assert.IsTrue(v22ManifestTemplate.Layers.Size() > 0);
        }

        [Test]
        public async Task TestPull_unknownManifestAsync()
        {
            localRegistry.PullAndPushToLocal("busybox", "busybox");
            try
            {
                RegistryClient registryClient =
                    RegistryClient.CreateFactory(EventHandlers.NONE, "localhost:5000", "busybox")
                        .SetAllowInsecureRegistries(true)
                        .NewRegistryClient();
                await registryClient.PullManifestAsync("nonexistent-tag").ConfigureAwait(false);
                Assert.Fail("Trying to pull nonexistent image should have errored");
            }
            catch (RegistryErrorException ex)
            {
                Assert.That(ex.GetMessage(),
                    Does.Contain("pull image manifest for localhost:5000/busybox:nonexistent-tag"));
            }
        }
    }
}
