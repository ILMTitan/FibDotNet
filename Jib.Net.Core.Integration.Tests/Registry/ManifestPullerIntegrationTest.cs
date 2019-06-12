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

namespace com.google.cloud.tools.jib.registry
{

    /** Integration tests for {@link ManifestPuller}. */
    public class ManifestPullerIntegrationTest
    {
        [ClassRule] public static LocalRegistry localRegistry = new LocalRegistry(5000);

        [Test]
        public void testPull_v21()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.factory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .setAllowInsecureRegistries(true)
                    .newRegistryClient();
            V21ManifestTemplate manifestTemplate =
                registryClient.pullManifest<V21ManifestTemplate>("latest");

            Assert.AreEqual(1, manifestTemplate.getSchemaVersion());
            Assert.IsTrue(manifestTemplate.getFsLayers().size() > 0);
        }

        [Test]
        public void testPull_v22()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.factory(EventHandlers.NONE, "gcr.io", "distroless/java").newRegistryClient();
            ManifestTemplate manifestTemplate = registryClient.pullManifest("latest");

            Assert.AreEqual(2, manifestTemplate.getSchemaVersion());
            V22ManifestTemplate v22ManifestTemplate = (V22ManifestTemplate)manifestTemplate;
            Assert.IsTrue(v22ManifestTemplate.getLayers().size() > 0);
        }

        [Test]
        public void testPull_unknownManifest()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            try
            {
                RegistryClient registryClient =
                    RegistryClient.factory(EventHandlers.NONE, "localhost:5000", "busybox")
                        .setAllowInsecureRegistries(true)
                        .newRegistryClient();
                registryClient.pullManifest("nonexistent-tag");
                Assert.Fail("Trying to pull nonexistent image should have errored");
            }
            catch (RegistryErrorException ex)
            {
                StringAssert.Contains(
                    ex.getMessage(),
                        "pull image manifest for localhost:5000/busybox:nonexistent-tag");
            }
        }
    }
}
