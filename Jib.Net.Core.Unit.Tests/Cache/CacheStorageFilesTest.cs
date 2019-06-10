/*
 * Copyright 2018 Google LLC. All rights reserved.
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










/** Tests for {@link CacheStorageFiles}. */
public class CacheStorageFilesTest {

  private static readonly CacheStorageFiles TEST_CACHE_STORAGE_FILES =
      new CacheStorageFiles(Paths.get("cache/directory"));

  [TestMethod]
  public void testIsLayerFile() {
    Assert.assertTrue(
        CacheStorageFiles.isLayerFile(
            Paths.get("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
    Assert.assertTrue(
        CacheStorageFiles.isLayerFile(
            Paths.get("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")));
    Assert.assertFalse(CacheStorageFiles.isLayerFile(Paths.get("is.not.layer.file")));
  }

  [TestMethod]
  public void testGetDiffId() {
    Assert.assertEquals(
        DescriptorDigest.fromHash(
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
        TEST_CACHE_STORAGE_FILES.getDiffId(
            Paths.get(
                "layer",
                "file",
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")));
    Assert.assertEquals(
        DescriptorDigest.fromHash(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
        TEST_CACHE_STORAGE_FILES.getDiffId(
            Paths.get("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
  }

  [TestMethod]
  public void testGetDiffId_corrupted() {
    try {
      TEST_CACHE_STORAGE_FILES.getDiffId(Paths.get("not long enough"));
      Assert.fail("Should have thrown CacheCorruptedException");

    } catch (CacheCorruptedException ex) {
      Assert.assertThat(
          ex.getMessage(),
          CoreMatchers.startsWith("Layer file did not include valid diff ID: not long enough"));
      Assert.assertThat(ex.getCause(), CoreMatchers.instanceOf(typeof(DigestException)));
    }

    try {
      TEST_CACHE_STORAGE_FILES.getDiffId(
          Paths.get(
              "not valid hash bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
      Assert.fail("Should have thrown CacheCorruptedException");

    } catch (CacheCorruptedException ex) {
      Assert.assertThat(
          ex.getMessage(),
          CoreMatchers.startsWith(
              "Layer file did not include valid diff ID: "
                  + "not valid hash bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
      Assert.assertThat(ex.getCause(), CoreMatchers.instanceOf(typeof(DigestException)));
    }
  }

  [TestMethod]
  public void testGetLayerFile() {
    DescriptorDigest layerDigest =
        DescriptorDigest.fromHash(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    DescriptorDigest diffId =
        DescriptorDigest.fromHash(
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

    Assert.assertEquals(
        Paths.get(
            "cache",
            "directory",
            "layers",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
        TEST_CACHE_STORAGE_FILES.getLayerFile(layerDigest, diffId));
  }

  [TestMethod]
  public void testGetLayerFilename() {
    DescriptorDigest diffId =
        DescriptorDigest.fromHash(
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

    Assert.assertEquals(
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        TEST_CACHE_STORAGE_FILES.getLayerFilename(diffId));
  }

  [TestMethod]
  public void testGetSelectorFile() {
    DescriptorDigest selector =
        DescriptorDigest.fromHash(
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc");

    Assert.assertEquals(
        Paths.get(
            "cache",
            "directory",
            "selectors",
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"),
        TEST_CACHE_STORAGE_FILES.getSelectorFile(selector));
  }

  [TestMethod]
  public void testGetLayersDirectory() {
    Assert.assertEquals(
        Paths.get("cache", "directory", "layers"), TEST_CACHE_STORAGE_FILES.getLayersDirectory());
  }

  [TestMethod]
  public void testGetLayerDirectory() {
    DescriptorDigest layerDigest =
        DescriptorDigest.fromHash(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    Assert.assertEquals(
        Paths.get(
            "cache",
            "directory",
            "layers",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
        TEST_CACHE_STORAGE_FILES.getLayerDirectory(layerDigest));
  }

  [TestMethod]
  public void testGetTemporaryDirectory() {
    Assert.assertEquals(
        Paths.get("cache/directory/tmp"), TEST_CACHE_STORAGE_FILES.getTemporaryDirectory());
  }

  [TestMethod]
  public void testGetImagesDirectory() {
    Assert.assertEquals(
        Paths.get("cache/directory/images"), TEST_CACHE_STORAGE_FILES.getImagesDirectory());
  }

  [TestMethod]
  public void testGetImageDirectory() {
    SystemPath imagesDirectory = Paths.get("cache", "directory", "images");
    Assert.assertEquals(imagesDirectory, TEST_CACHE_STORAGE_FILES.getImagesDirectory());

    Assert.assertEquals(
        imagesDirectory.resolve("reg.istry/repo/sitory!tag"),
        TEST_CACHE_STORAGE_FILES.getImageDirectory(
            ImageReference.parse("reg.istry/repo/sitory:tag")));
    Assert.assertEquals(
        imagesDirectory.resolve(
            "reg.istry/repo!sha256!aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
        TEST_CACHE_STORAGE_FILES.getImageDirectory(
            ImageReference.parse(
                "reg.istry/repo@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
    Assert.assertEquals(
        imagesDirectory.resolve("reg.istry!5000/repo/sitory!tag"),
        TEST_CACHE_STORAGE_FILES.getImageDirectory(
            ImageReference.parse("reg.istry:5000/repo/sitory:tag")));
  }
}
}
