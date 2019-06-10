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

namespace com.google.cloud.tools.jib.image {













/** Tests for {@link ImageLayers}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class ImageLayersTest {

  [Mock] private Layer mockLayer;
  [Mock] private ReferenceLayer mockReferenceLayer;
  [Mock] private DigestOnlyLayer mockDigestOnlyLayer;
  [Mock] private Layer mockLayer2;

  [TestInitialize]
  public void setUpFakes() {
    DescriptorDigest mockDescriptorDigest1 = Mockito.mock(typeof(DescriptorDigest));
    DescriptorDigest mockDescriptorDigest2 = Mockito.mock(typeof(DescriptorDigest));
    DescriptorDigest mockDescriptorDigest3 = Mockito.mock(typeof(DescriptorDigest));

    BlobDescriptor layerBlobDescriptor = new BlobDescriptor(0, mockDescriptorDigest1);
    BlobDescriptor referenceLayerBlobDescriptor = new BlobDescriptor(0, mockDescriptorDigest2);
    BlobDescriptor referenceNoDiffIdLayerBlobDescriptor =
        new BlobDescriptor(0, mockDescriptorDigest3);
    // Intentionally the same digest as the mockLayer.
    BlobDescriptor anotherBlobDescriptor = new BlobDescriptor(0, mockDescriptorDigest1);

    Mockito.when(mockLayer.getBlobDescriptor()).thenReturn(layerBlobDescriptor);
    Mockito.when(mockReferenceLayer.getBlobDescriptor()).thenReturn(referenceLayerBlobDescriptor);
    Mockito.when(mockDigestOnlyLayer.getBlobDescriptor())
        .thenReturn(referenceNoDiffIdLayerBlobDescriptor);
    Mockito.when(mockLayer2.getBlobDescriptor()).thenReturn(anotherBlobDescriptor);
  }

  [TestMethod]
  public void testAddLayer_success() {
    IList<Layer> expectedLayers = Arrays.asList(mockLayer, mockReferenceLayer, mockDigestOnlyLayer);

    ImageLayers imageLayers =
        ImageLayers.builder()
            .add(mockLayer)
            .add(mockReferenceLayer)
            .add(mockDigestOnlyLayer)
            .build();

    Assert.assertThat(imageLayers.getLayers(), CoreMatchers.is(expectedLayers));
  }

  [TestMethod]
  public void testAddLayer_maintainDuplicates() {
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

    Assert.assertEquals(expectedLayers, imageLayers.getLayers());
  }

  [TestMethod]
  public void testAddLayer_removeDuplicates() {
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

    Assert.assertEquals(expectedLayers, imageLayers.getLayers());
  }
}
}
