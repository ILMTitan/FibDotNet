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
        private readonly DescriptorDigest mockLayerDigest = DescriptorDigest.FromHash(new string('a', 64));
        private readonly DescriptorDigest mockLayerDiffId = DescriptorDigest.FromHash(new string('b', 64));

        [Test]
        public void TestBuilder_fail()
        {
            try
            {
                CachedLayer.CreateBuilder().Build();
                Assert.Fail("missing required");
            }
            catch (ArgumentNullException ex)
            {
                Assert.That(ex.GetMessage(), Does.Contain("layerDigest"));
            }

            try
            {
                CachedLayer.CreateBuilder().SetLayerDigest(mockLayerDigest).Build();
                Assert.Fail("missing required");
            }
            catch (ArgumentNullException ex)
            {
                Assert.That(ex.GetMessage(), Does.Contain("layerDiffId"));
            }

            try
            {
                CachedLayer.CreateBuilder().SetLayerDigest(mockLayerDigest).SetLayerDiffId(mockLayerDiffId).Build();
                Assert.Fail("missing required");
            }
            catch (ArgumentNullException ex)
            {
                Assert.That(ex.GetMessage(), Does.Contain("layerBlob"));
            }
        }

        [Test]
        public async Task TestBuilder_passAsync()
        {
            CachedLayer.Builder cachedLayerBuilder =
                CachedLayer.CreateBuilder()
                    .SetLayerDigest(mockLayerDigest)
                    .SetLayerDiffId(mockLayerDiffId)
                    .SetLayerSize(1337);
            Assert.IsFalse(cachedLayerBuilder.HasLayerBlob());
            cachedLayerBuilder.SetLayerBlob(Blobs.From("layerBlob"));
            Assert.IsTrue(cachedLayerBuilder.HasLayerBlob());
            CachedLayer cachedLayer = cachedLayerBuilder.Build();
            Assert.AreEqual(mockLayerDigest, cachedLayer.GetDigest());
            Assert.AreEqual(mockLayerDiffId, cachedLayer.GetDiffId());
            Assert.AreEqual(1337, cachedLayer.GetSize());
            Assert.AreEqual("layerBlob", await Blobs.WriteToStringAsync(cachedLayer.GetBlob()).ConfigureAwait(false));
        }
    }
}
