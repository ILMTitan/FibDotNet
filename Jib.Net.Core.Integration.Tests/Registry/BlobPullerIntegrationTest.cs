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

using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images.Json;
using Jib.Net.Core.Registry;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Integration tests for {@link BlobPuller}. */
    public class BlobPullerIntegrationTest : HttpRegistryTest
    {
        [Test]
        public async Task TestPullAsync()
        {
            // Pulls the busybox image.
            localRegistry.PullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.CreateFactory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .SetAllowInsecureRegistries(true)
                    .NewRegistryClient();
            V21ManifestTemplate manifestTemplate =
                await registryClient.PullManifestAsync<V21ManifestTemplate>("latest").ConfigureAwait(false);

            DescriptorDigest realDigest = manifestTemplate.GetLayerDigests().First();

            // Pulls a layer BLOB of the busybox image.
            LongAdder totalByteCount = new LongAdder();
            LongAdder expectedSize = new LongAdder();
            IBlob pulledBlob =
                registryClient.PullBlob(
                    realDigest,
                    size =>
                    {
                        Assert.AreEqual(0, expectedSize.Sum());
                        expectedSize.Add(size);
                    },
                    totalByteCount.Add);
            BlobDescriptor blobDescriptor = await pulledBlob.WriteToAsync(Stream.Null).ConfigureAwait(false);
            Assert.AreEqual(realDigest, blobDescriptor.GetDigest());
            Assert.IsTrue(expectedSize.Sum() > 0);
            Assert.AreEqual(expectedSize.Sum(), totalByteCount.Sum());
        }

        [Test]
        public async Task TestPull_unknownBlobAsync()
        {
            localRegistry.PullAndPushToLocal("busybox", "busybox");
            DescriptorDigest nonexistentDigest =
                DescriptorDigest.FromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            RegistryClient registryClient =
                RegistryClient.CreateFactory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .SetAllowInsecureRegistries(true)
                    .NewRegistryClient();

            try
            {
                await registryClient
                    .PullBlob(nonexistentDigest, _ => { }, _ => { })
                    .WriteToAsync(Stream.Null).ConfigureAwait(false);
                Assert.Fail("Trying to pull nonexistent blob should have errored");
            }
            catch (IOException ex)
            {
                if (!(ex.InnerException is RegistryErrorException))
                {
                    throw;
                }
                StringAssert.Contains(
                    ex.GetMessage(),
                        "pull BLOB for localhost:5000/busybox with digest " + nonexistentDigest);
            }
        }
    }
}
