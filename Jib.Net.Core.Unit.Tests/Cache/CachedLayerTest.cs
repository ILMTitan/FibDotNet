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
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.cache
{
    /** Tests for {@link CachedLayer}. */
    public class CachedLayerTest
    {
        private readonly DescriptorDigest mockLayerDigest = DescriptorDigest.fromHash(new string('a', 64));
        private readonly DescriptorDigest mockLayerDiffId = DescriptorDigest.fromHash(new string('b', 64));

        [Test]
        public void testBuilder_fail()
        {
            try
            {
                CachedLayer.builder().build();
                Assert.Fail("missing required");
            }
            catch (ArgumentNullException ex)
            {
                Assert.That(ex.getMessage(), Does.Contain("layerDigest"));
            }

            try
            {
                CachedLayer.builder().setLayerDigest(mockLayerDigest).build();
                Assert.Fail("missing required");
            }
            catch (ArgumentNullException ex)
            {
                Assert.That(ex.getMessage(), Does.Contain("layerDiffId"));
            }

            try
            {
                CachedLayer.builder().setLayerDigest(mockLayerDigest).setLayerDiffId(mockLayerDiffId).build();
                Assert.Fail("missing required");
            }
            catch (ArgumentNullException ex)
            {
                Assert.That(ex.getMessage(), Does.Contain("layerBlob"));
            }
        }

        [Test]
        public async Task testBuilder_passAsync()
        {
            CachedLayer.Builder cachedLayerBuilder =
                CachedLayer.builder()
                    .setLayerDigest(mockLayerDigest)
                    .setLayerDiffId(mockLayerDiffId)
                    .setLayerSize(1337);
            Assert.IsFalse(cachedLayerBuilder.hasLayerBlob());
            cachedLayerBuilder.setLayerBlob(Blobs.from("layerBlob"));
            Assert.IsTrue(cachedLayerBuilder.hasLayerBlob());
            CachedLayer cachedLayer = cachedLayerBuilder.build();
            Assert.AreEqual(mockLayerDigest, cachedLayer.getDigest());
            Assert.AreEqual(mockLayerDiffId, cachedLayer.getDiffId());
            Assert.AreEqual(1337, cachedLayer.getSize());
            Assert.AreEqual("layerBlob", await Blobs.writeToStringAsync(cachedLayer.getBlob()).ConfigureAwait(false));
        }
    }
}
