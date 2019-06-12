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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.IO;

namespace com.google.cloud.tools.jib.registry
{






    /** Integration tests for {@link BlobPuller}. */
    public class BlobPullerIntegrationTest
    {
        [ClassRule] public static LocalRegistry localRegistry = new LocalRegistry(5000);

        [Rule] public TemporaryFolder temporaryFolder = new TemporaryFolder();

        [Test]
        public void testPull()
        {
            // Pulls the busybox image.
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            RegistryClient registryClient =
                RegistryClient.factory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .setAllowInsecureRegistries(true)
                    .newRegistryClient();
            V21ManifestTemplate manifestTemplate =
                registryClient.pullManifest<V21ManifestTemplate>("latest");

            DescriptorDigest realDigest = manifestTemplate.getLayerDigests().get(0);

            // Pulls a layer BLOB of the busybox image.
            LongAdder totalByteCount = new LongAdder();
            LongAdder expectedSize = new LongAdder();
            Blob pulledBlob =
                registryClient.pullBlob(
                    realDigest,
                    size =>
                    {
                        Assert.AreEqual(0, expectedSize.sum());
                        expectedSize.add(size);
                    },
                    totalByteCount.add);
            Assert.AreEqual(realDigest, pulledBlob.writeTo(Stream.Null).getDigest());
            Assert.IsTrue(expectedSize.sum() > 0);
            Assert.AreEqual(expectedSize.sum(), totalByteCount.sum());
        }

        [Test]
        public void testPull_unknownBlob()
        {
            localRegistry.pullAndPushToLocal("busybox", "busybox");
            DescriptorDigest nonexistentDigest =
                DescriptorDigest.fromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            RegistryClient registryClient =
                RegistryClient.factory(EventHandlers.NONE, "localhost:5000", "busybox")
                    .setAllowInsecureRegistries(true)
                    .newRegistryClient();

            try
            {
                registryClient
                    .pullBlob(nonexistentDigest, ignored => { }, ignored => { })
                    .writeTo(Stream.Null);
                Assert.Fail("Trying to pull nonexistent blob should have errored");
            }
            catch (IOException ex)
            {
                if (!(ex.getCause() is RegistryErrorException))
                {
                    throw;
                }
                StringAssert.Contains(
                    ex.getMessage(),
                        "pull BLOB for localhost:5000/busybox with digest " + nonexistentDigest);
            }
        }
    }
}