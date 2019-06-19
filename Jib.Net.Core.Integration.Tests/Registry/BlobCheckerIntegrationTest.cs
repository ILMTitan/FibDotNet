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
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Integration tests for {@link BlobChecker}. */
    public class BlobCheckerIntegrationTest : HttpRegistryTest {
        [Test]
        public async Task testCheck_existsAsync()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.factory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .setAllowInsecureRegistries(true)
                    .newRegistryClient();
            V22ManifestTemplate manifestTemplate =
                await registryClient.pullManifestAsync<V22ManifestTemplate>("latest").ConfigureAwait(false);
            DescriptorDigest blobDigest = manifestTemplate.getLayers().get(0).getDigest();

            Assert.IsTrue(await registryClient.checkBlobAsync(new BlobDescriptor(blobDigest)).ConfigureAwait(false));
        }

        [Test]
        public async Task testCheck_doesNotExistAsync()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.factory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .setAllowInsecureRegistries(true)
                    .newRegistryClient();
            DescriptorDigest fakeBlobDigest =
                DescriptorDigest.fromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            Assert.IsFalse(await registryClient.checkBlobAsync(new BlobDescriptor(fakeBlobDigest)).ConfigureAwait(false));
        }
    }
}
