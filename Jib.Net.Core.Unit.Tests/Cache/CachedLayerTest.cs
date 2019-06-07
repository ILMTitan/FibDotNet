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

namespace com.google.cloud.tools.jib.cache {










/** Tests for {@link CachedLayer}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class CachedLayerTest {

  [Mock] private DescriptorDigest mockLayerDigest;
  [Mock] private DescriptorDigest mockLayerDiffId;

  [TestMethod]
  public void testBuilder_fail() {
    try {
      CachedLayer.builder().build();
      Assert.fail("missing required");

    } catch (NullPointerException ex) {
      Assert.assertThat(ex.getMessage(), CoreMatchers.containsString("layerDigest"));
    }

    try {
      CachedLayer.builder().setLayerDigest(mockLayerDigest).build();
      Assert.fail("missing required");

    } catch (NullPointerException ex) {
      Assert.assertThat(ex.getMessage(), CoreMatchers.containsString("layerDiffId"));
    }

    try {
      CachedLayer.builder().setLayerDigest(mockLayerDigest).setLayerDiffId(mockLayerDiffId).build();
      Assert.fail("missing required");

    } catch (NullPointerException ex) {
      Assert.assertThat(ex.getMessage(), CoreMatchers.containsString("layerBlob"));
    }
  }

  [TestMethod]
  public void testBuilder_pass() {
    CachedLayer.Builder cachedLayerBuilder =
        CachedLayer.builder()
            .setLayerDigest(mockLayerDigest)
            .setLayerDiffId(mockLayerDiffId)
            .setLayerSize(1337);
    Assert.assertFalse(cachedLayerBuilder.hasLayerBlob());
    cachedLayerBuilder.setLayerBlob(Blobs.from("layerBlob"));
    Assert.assertTrue(cachedLayerBuilder.hasLayerBlob());
    CachedLayer cachedLayer = cachedLayerBuilder.build();
    Assert.assertEquals(mockLayerDigest, cachedLayer.getDigest());
    Assert.assertEquals(mockLayerDiffId, cachedLayer.getDiffId());
    Assert.assertEquals(1337, cachedLayer.getSize());
    Assert.assertEquals("layerBlob", Blobs.writeToString(cachedLayer.getBlob()));
  }
}
}
