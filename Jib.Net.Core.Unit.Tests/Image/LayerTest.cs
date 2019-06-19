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

using com.google.cloud.tools.jib.cache;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.image
{
    /** Tests for {@link Layer}. */
    public class LayerTest
    {
        private readonly DescriptorDigest mockDescriptorDigest = DescriptorDigest.fromHash(new string('a', 64));
        private readonly BlobDescriptor mockBlobDescriptor = new BlobDescriptor(DescriptorDigest.fromHash(new string('b', 64)));
        private readonly DescriptorDigest mockDiffId = DescriptorDigest.fromHash(new string('c', 64));

        [Test]
        public void testNew_reference()
        {
            Layer layer = new ReferenceLayer(mockBlobDescriptor, mockDiffId);

            try
            {
                layer.getBlob();
                Assert.Fail("Blob content should not be available for reference layer");
            }
            catch (LayerPropertyNotFoundException ex)
            {
                Assert.AreEqual("Blob not available for reference layer", ex.getMessage());
            }

            Assert.AreEqual(mockBlobDescriptor, layer.getBlobDescriptor());
            Assert.AreEqual(mockDiffId, layer.getDiffId());
        }

        [Test]
        public void testNew_digestOnly()
        {
            Layer layer = new DigestOnlyLayer(mockDescriptorDigest);

            try
            {
                layer.getBlob();
                Assert.Fail("Blob content should not be available for digest-only layer");
            }
            catch (LayerPropertyNotFoundException ex)
            {
                Assert.AreEqual("Blob not available for digest-only layer", ex.getMessage());
            }

            Assert.IsFalse(layer.getBlobDescriptor().hasSize());
            Assert.AreEqual(mockDescriptorDigest, layer.getBlobDescriptor().getDigest());

            try
            {
                layer.getDiffId();
                Assert.Fail("Diff ID should not be available for digest-only layer");
            }
            catch (LayerPropertyNotFoundException ex)
            {
                Assert.AreEqual("Diff ID not available for digest-only layer", ex.getMessage());
            }
        }
    }
}
