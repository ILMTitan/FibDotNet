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

using Fib.Net.Core.Api;
using Fib.Net.Core.Blob;
using Fib.Net.Core.Images;
using NUnit.Framework;

namespace Fib.Net.Core.Unit.Tests.Images
{
    /** Tests for {@link Layer}. */
    public class LayerTest
    {
        private readonly DescriptorDigest mockDescriptorDigest = DescriptorDigest.FromHash(new string('a', 64));
        private readonly BlobDescriptor mockBlobDescriptor = new BlobDescriptor(DescriptorDigest.FromHash(new string('b', 64)));
        private readonly DescriptorDigest mockDiffId = DescriptorDigest.FromHash(new string('c', 64));

        [Test]
        public void TestNew_reference()
        {
            ILayer layer = new ReferenceLayer(mockBlobDescriptor, mockDiffId);

            try
            {
                layer.GetBlob();
                Assert.Fail("Blob content should not be available for reference layer");
            }
            catch (LayerPropertyNotFoundException ex)
            {
                Assert.AreEqual("Blob not available for reference layer", ex.Message);
            }

            Assert.AreEqual(mockBlobDescriptor, layer.GetBlobDescriptor());
            Assert.AreEqual(mockDiffId, layer.GetDiffId());
        }

        [Test]
        public void TestNew_digestOnly()
        {
            ILayer layer = new DigestOnlyLayer(mockDescriptorDigest);

            try
            {
                layer.GetBlob();
                Assert.Fail("Blob content should not be available for digest-only layer");
            }
            catch (LayerPropertyNotFoundException ex)
            {
                Assert.AreEqual("Blob not available for digest-only layer", ex.Message);
            }

            Assert.IsFalse(layer.GetBlobDescriptor().HasSize());
            Assert.AreEqual(mockDescriptorDigest, layer.GetBlobDescriptor().GetDigest());

            try
            {
                layer.GetDiffId();
                Assert.Fail("Diff ID should not be available for digest-only layer");
            }
            catch (LayerPropertyNotFoundException ex)
            {
                Assert.AreEqual("Diff ID not available for digest-only layer", ex.Message);
            }
        }
    }
}
