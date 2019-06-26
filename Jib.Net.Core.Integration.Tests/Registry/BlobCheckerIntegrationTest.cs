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
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images.Json;
using Jib.Net.Core.Registry;
using NUnit.Framework;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Integration tests for {@link BlobChecker}. */
    public class BlobCheckerIntegrationTest : HttpRegistryTest {
        [Test]
        public async Task TestCheck_existsAsync()
        {
            localRegistry.PullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.CreateFactory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .SetAllowInsecureRegistries(true)
                    .NewRegistryClient();
            V22ManifestTemplate manifestTemplate =
                await registryClient.PullManifestAsync<V22ManifestTemplate>("latest").ConfigureAwait(false);
            DescriptorDigest blobDigest = manifestTemplate.Layers[0].Digest;

            Assert.IsTrue(await registryClient.CheckBlobAsync(new BlobDescriptor(blobDigest)).ConfigureAwait(false));
        }

        [Test]
        public async Task TestCheck_doesNotExistAsync()
        {
            localRegistry.PullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.CreateFactory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .SetAllowInsecureRegistries(true)
                    .NewRegistryClient();
            DescriptorDigest fakeBlobDigest =
                DescriptorDigest.FromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            Assert.IsFalse(await registryClient.CheckBlobAsync(new BlobDescriptor(fakeBlobDigest)).ConfigureAwait(false));
        }
    }
}
