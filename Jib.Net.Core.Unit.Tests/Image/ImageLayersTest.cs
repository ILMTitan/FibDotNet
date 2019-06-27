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

using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Images;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.image
{
    /** Tests for {@link ImageLayers}. */
    public class ImageLayersTest
    {
        private readonly ILayer mockLayer = Mock.Of<ILayer>();
        private readonly ILayer mockReferenceLayer = Mock.Of<ILayer>();
        private readonly ILayer mockDigestOnlyLayer = Mock.Of<ILayer>();
        private readonly ILayer mockLayer2 = Mock.Of<ILayer>();

        [SetUp]
        public void SetUpFakes()
        {
            DescriptorDigest mockDescriptorDigest1 = DescriptorDigest.FromHash(new string('a', 64));
            DescriptorDigest mockDescriptorDigest2 = DescriptorDigest.FromHash(new string('b', 64));
            DescriptorDigest mockDescriptorDigest3 = DescriptorDigest.FromHash(new string('c', 64));

            BlobDescriptor layerBlobDescriptor = new BlobDescriptor(0, mockDescriptorDigest1);
            BlobDescriptor referenceLayerBlobDescriptor = new BlobDescriptor(0, mockDescriptorDigest2);
            BlobDescriptor referenceNoDiffIdLayerBlobDescriptor =
                new BlobDescriptor(0, mockDescriptorDigest3);
            // Intentionally the same digest as the mockLayer.
            BlobDescriptor anotherBlobDescriptor = new BlobDescriptor(0, mockDescriptorDigest1);

            Mock.Get(mockLayer).Setup(m => m.GetBlobDescriptor()).Returns(layerBlobDescriptor);

            Mock.Get(mockReferenceLayer).Setup(m => m.GetBlobDescriptor()).Returns(referenceLayerBlobDescriptor);

            Mock.Get(mockDigestOnlyLayer).Setup(m => m.GetBlobDescriptor()).Returns(referenceNoDiffIdLayerBlobDescriptor);

            Mock.Get(mockLayer2).Setup(m => m.GetBlobDescriptor()).Returns(anotherBlobDescriptor);
        }

        [Test]
        public void TestAddLayer_success()
        {
            IList<ILayer> expectedLayers = new []{mockLayer, mockReferenceLayer, mockDigestOnlyLayer};

            ImageLayers imageLayers =
                ImageLayers.CreateBuilder()
                    .Add(mockLayer)
                    .Add(mockReferenceLayer)
                    .Add(mockDigestOnlyLayer)
                    .Build();

            Assert.AreEqual(imageLayers.GetLayers(), expectedLayers);
        }

        [Test]
        public void TestAddLayer_maintainDuplicates()
        {
            // must maintain duplicate
            IList<ILayer> expectedLayers =
                new []{mockLayer, mockReferenceLayer, mockDigestOnlyLayer, mockLayer2, mockLayer};

            ImageLayers imageLayers =
                ImageLayers.CreateBuilder()
                    .Add(mockLayer)
                    .Add(mockReferenceLayer)
                    .Add(mockDigestOnlyLayer)
                    .Add(mockLayer2)
                    .Add(mockLayer)
                    .Build();

            Assert.AreEqual(expectedLayers, imageLayers.GetLayers());
        }

        [Test]
        public void TestAddLayer_removeDuplicates()
        {
            // remove duplicates: last layer should be kept
            IList<ILayer> expectedLayers =
                new []{mockReferenceLayer, mockDigestOnlyLayer, mockLayer2, mockLayer};

            ImageLayers imageLayers =
                ImageLayers.CreateBuilder()
                    .RemoveDuplicates()
                    .Add(mockLayer)
                    .Add(mockReferenceLayer)
                    .Add(mockDigestOnlyLayer)
                    .Add(mockLayer2)
                    .Add(mockLayer)
                    .Build();

            Assert.AreEqual(expectedLayers, imageLayers.GetLayers());
        }
    }
}
