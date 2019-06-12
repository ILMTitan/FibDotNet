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
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.image
{



    /** Tests for {@link ImageLayers}. */
    [RunWith(typeof(MockitoJUnitRunner))]
    public class ImageLayersTest
    {
        private Layer mockLayer = Mock.Of<Layer>();
        private ReferenceLayer mockReferenceLayer = Mock.Of<ReferenceLayer>();
        private DigestOnlyLayer mockDigestOnlyLayer = Mock.Of<DigestOnlyLayer>();
        private Layer mockLayer2 = Mock.Of<Layer>();

        [SetUp]
        public void setUpFakes()
        {
            DescriptorDigest mockDescriptorDigest1 = Mock.Of<DescriptorDigest>();
            DescriptorDigest mockDescriptorDigest2 = Mock.Of<DescriptorDigest>();
            DescriptorDigest mockDescriptorDigest3 = Mock.Of<DescriptorDigest>();

            BlobDescriptor layerBlobDescriptor = new BlobDescriptor(0, mockDescriptorDigest1);
            BlobDescriptor referenceLayerBlobDescriptor = new BlobDescriptor(0, mockDescriptorDigest2);
            BlobDescriptor referenceNoDiffIdLayerBlobDescriptor =
                new BlobDescriptor(0, mockDescriptorDigest3);
            // Intentionally the same digest as the mockLayer.
            BlobDescriptor anotherBlobDescriptor = new BlobDescriptor(0, mockDescriptorDigest1);

            Mock.Get(mockLayer).Setup(m => m.getBlobDescriptor()).Returns(layerBlobDescriptor);

            Mock.Get(mockReferenceLayer).Setup(m => m.getBlobDescriptor()).Returns(referenceLayerBlobDescriptor);

            Mock.Get(mockDigestOnlyLayer).Setup(m => m.getBlobDescriptor()).Returns(referenceNoDiffIdLayerBlobDescriptor);

            Mock.Get(mockLayer2).Setup(m => m.getBlobDescriptor()).Returns(anotherBlobDescriptor);
        }

        [Test]
        public void testAddLayer_success()
        {
            IList<Layer> expectedLayers = Arrays.asList(mockLayer, mockReferenceLayer, mockDigestOnlyLayer);

            ImageLayers imageLayers =
                ImageLayers.builder()
                    .add(mockLayer)
                    .add(mockReferenceLayer)
                    .add(mockDigestOnlyLayer)
                    .build();

            Assert.AreEqual(imageLayers.getLayers(), expectedLayers);
        }

        [Test]
        public void testAddLayer_maintainDuplicates()
        {
            // must maintain duplicate
            IList<Layer> expectedLayers =
                Arrays.asList(mockLayer, mockReferenceLayer, mockDigestOnlyLayer, mockLayer2, mockLayer);

            ImageLayers imageLayers =
                ImageLayers.builder()
                    .add(mockLayer)
                    .add(mockReferenceLayer)
                    .add(mockDigestOnlyLayer)
                    .add(mockLayer2)
                    .add(mockLayer)
                    .build();

            Assert.AreEqual(expectedLayers, imageLayers.getLayers());
        }

        [Test]
        public void testAddLayer_removeDuplicates()
        {
            // remove duplicates: last layer should be kept
            IList<Layer> expectedLayers =
                Arrays.asList(mockReferenceLayer, mockDigestOnlyLayer, mockLayer2, mockLayer);

            ImageLayers imageLayers =
                ImageLayers.builder()
                    .removeDuplicates()
                    .add(mockLayer)
                    .add(mockReferenceLayer)
                    .add(mockDigestOnlyLayer)
                    .add(mockLayer2)
                    .add(mockLayer)
                    .build();

            Assert.AreEqual(expectedLayers, imageLayers.getLayers());
        }
    }
}
