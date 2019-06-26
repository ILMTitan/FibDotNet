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

using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.cache
{
    /** Tests for {@link CacheStorageFiles}. */
    public class CacheStorageFilesTest
    {
        private static readonly CacheStorageFiles TEST_CACHE_STORAGE_FILES =
            new CacheStorageFiles(Paths.get("cache/directory"));

        [Test]
        public void testIsLayerFile()
        {
            Assert.IsTrue(
                CacheStorageFiles.isLayerFile(
                    Paths.get("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
            Assert.IsTrue(
                CacheStorageFiles.isLayerFile(
                    Paths.get("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")));
            Assert.IsFalse(CacheStorageFiles.isLayerFile(Paths.get("is.not.layer.file")));
        }

        [Test]
        public void testGetDiffId()
        {
            Assert.AreEqual(
                DescriptorDigest.fromHash(
                    "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                TEST_CACHE_STORAGE_FILES.getDiffId(
                    Paths.get(
                        "layer",
                        "file",
                        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")));
            Assert.AreEqual(
                DescriptorDigest.fromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                TEST_CACHE_STORAGE_FILES.getDiffId(
                    Paths.get("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
        }

        [Test]
        public void testGetDiffId_corrupted()
        {
            try
            {
                TEST_CACHE_STORAGE_FILES.getDiffId(Paths.get("not long enough"));
                Assert.Fail("Should have thrown CacheCorruptedException");
            }
            catch (CacheCorruptedException ex)
            {
                Assert.That(
                    ex.getMessage(),
                    Does.StartWith("Layer file did not include valid diff ID: not long enough"));

                Assert.IsInstanceOf<DigestException>(ex.getCause());
            }

            try
            {
                TEST_CACHE_STORAGE_FILES.getDiffId(
                    Paths.get(
                        "not valid hash bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
                Assert.Fail("Should have thrown CacheCorruptedException");
            }
            catch (CacheCorruptedException ex)
            {
                Assert.That(
                    ex.getMessage(),
                    Does.StartWith(
                        "Layer file did not include valid diff ID: "
                            + "not valid hash bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));

                Assert.IsInstanceOf<DigestException>(ex.getCause());
            }
        }

        [Test]
        public void testGetLayerFile()
        {
            DescriptorDigest layerDigest =
                DescriptorDigest.fromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            DescriptorDigest diffId =
                DescriptorDigest.fromHash(
                    "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

            Assert.AreEqual(
                Paths.get(
                    "cache",
                    "directory",
                    "layers",
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                TEST_CACHE_STORAGE_FILES.getLayerFile(layerDigest, diffId));
        }

        [Test]
        public void testGetLayerFilename()
        {
            DescriptorDigest diffId =
                DescriptorDigest.fromHash(
                    "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

            Assert.AreEqual(
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                TEST_CACHE_STORAGE_FILES.getLayerFilename(diffId));
        }

        [Test]
        public void testGetSelectorFile()
        {
            DescriptorDigest selector =
                DescriptorDigest.fromHash(
                    "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc");

            Assert.AreEqual(
                Paths.get(
                    "cache",
                    "directory",
                    "selectors",
                    "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"),
                TEST_CACHE_STORAGE_FILES.getSelectorFile(selector));
        }

        [Test]
        public void testGetLayersDirectory()
        {
            Assert.AreEqual(
                Paths.get("cache", "directory", "layers"), TEST_CACHE_STORAGE_FILES.getLayersDirectory());
        }

        [Test]
        public void testGetLayerDirectory()
        {
            DescriptorDigest layerDigest =
                DescriptorDigest.fromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            Assert.AreEqual(
                Paths.get(
                    "cache",
                    "directory",
                    "layers",
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                TEST_CACHE_STORAGE_FILES.getLayerDirectory(layerDigest));
        }

        [Test]
        public void testGetTemporaryDirectory()
        {
            Assert.AreEqual(
                Paths.get("cache/directory/tmp"), TEST_CACHE_STORAGE_FILES.getTemporaryDirectory());
        }

        [Test]
        public void testGetImagesDirectory()
        {
            Assert.AreEqual(
                Paths.get("cache/directory/images"), TEST_CACHE_STORAGE_FILES.getImagesDirectory());
        }

        [Test]
        public void testGetImageDirectory()
        {
            SystemPath imagesDirectory = Paths.get("cache", "directory", "images");
            Assert.AreEqual(imagesDirectory, TEST_CACHE_STORAGE_FILES.getImagesDirectory());

            Assert.AreEqual(
                imagesDirectory.Resolve("reg.istry/repo/sitory!tag"),
                TEST_CACHE_STORAGE_FILES.getImageDirectory(
                    ImageReference.parse("reg.istry/repo/sitory:tag")));
            Assert.AreEqual(
                imagesDirectory.Resolve(
                    "reg.istry/repo!sha256!aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                TEST_CACHE_STORAGE_FILES.getImageDirectory(
                    ImageReference.parse(
                        "reg.istry/repo@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
            Assert.AreEqual(
                imagesDirectory.Resolve("reg.istry!5000/repo/sitory!tag"),
                TEST_CACHE_STORAGE_FILES.getImageDirectory(
                    ImageReference.parse("reg.istry:5000/repo/sitory:tag")));
        }
    }
}
