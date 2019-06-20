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
using NUnit.Framework;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Integration tests for {@link ManifestPuller}. */
    public class ManifestPullerIntegrationTest : HttpRegistryTest
    {
        [Test]
        public async System.Threading.Tasks.Task testPull_v21Async()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.factory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .setAllowInsecureRegistries(true)
                    .newRegistryClient();
            V21ManifestTemplate manifestTemplate =
                await registryClient.pullManifestAsync<V21ManifestTemplate>("latest").ConfigureAwait(false);

            Assert.AreEqual(1, manifestTemplate.getSchemaVersion());
            Assert.IsTrue(manifestTemplate.getFsLayers().size() > 0);
        }

        [Test]
        public async Task testPull_v22Async()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.factory(EventHandlers.NONE, "gcr.io", "distroless/java").newRegistryClient();
            IManifestTemplate manifestTemplate = await registryClient.pullManifestAsync("latest").ConfigureAwait(false);

            Assert.AreEqual(2, manifestTemplate.getSchemaVersion());
            V22ManifestTemplate v22ManifestTemplate = (V22ManifestTemplate)manifestTemplate;
            Assert.IsTrue(v22ManifestTemplate.getLayers().size() > 0);
        }

        [Test]
        public async Task testPull_unknownManifestAsync()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            try
            {
                RegistryClient registryClient =
                    RegistryClient.factory(EventHandlers.NONE, "localhost:5000", "busybox")
                        .setAllowInsecureRegistries(true)
                        .newRegistryClient();
                await registryClient.pullManifestAsync("nonexistent-tag").ConfigureAwait(false);
                Assert.Fail("Trying to pull nonexistent image should have errored");
            }
            catch (RegistryErrorException ex)
            {
                Assert.That(ex.getMessage(),
                    Does.Contain("pull image manifest for localhost:5000/busybox:nonexistent-tag"));
            }
        }
    }
}
