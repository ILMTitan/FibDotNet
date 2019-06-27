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
using Jib.Net.Core.Caching;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.cache
{
    /** Tests for {@link CacheStorageFiles}. */
    public class CacheStorageFilesTest
    {
        private static readonly CacheStorageFiles TEST_CACHE_STORAGE_FILES =
            new CacheStorageFiles(Paths.Get("cache/directory"));

        [Test]
        public void TestIsLayerFile()
        {
            Assert.IsTrue(
                CacheStorageFiles.IsLayerFile(
                    Paths.Get("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
            Assert.IsTrue(
                CacheStorageFiles.IsLayerFile(
                    Paths.Get("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")));
            Assert.IsFalse(CacheStorageFiles.IsLayerFile(Paths.Get("is.not.layer.file")));
        }

        [Test]
        public void TestGetDiffId()
        {
            Assert.AreEqual(
                DescriptorDigest.FromHash(
                    "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                TEST_CACHE_STORAGE_FILES.GetDiffId(
                    Paths.Get(
                        "layer",
                        "file",
                        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")));
            Assert.AreEqual(
                DescriptorDigest.FromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                TEST_CACHE_STORAGE_FILES.GetDiffId(
                    Paths.Get("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
        }

        [Test]
        public void TestGetDiffId_corrupted()
        {
            try
            {
                TEST_CACHE_STORAGE_FILES.GetDiffId(Paths.Get("not long enough"));
                Assert.Fail("Should have thrown CacheCorruptedException");
            }
            catch (CacheCorruptedException ex)
            {
                Assert.That(
                    ex.GetMessage(),
                    Does.StartWith("Layer file did not include valid diff ID: not long enough"));

                Assert.IsInstanceOf<DigestException>(ex.InnerException);
            }

            try
            {
                TEST_CACHE_STORAGE_FILES.GetDiffId(
                    Paths.Get(
                        "not valid hash bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
                Assert.Fail("Should have thrown CacheCorruptedException");
            }
            catch (CacheCorruptedException ex)
            {
                Assert.That(
                    ex.GetMessage(),
                    Does.StartWith(
                        "Layer file did not include valid diff ID: "
                            + "not valid hash bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));

                Assert.IsInstanceOf<DigestException>(ex.InnerException);
            }
        }

        [Test]
        public void TestGetLayerFile()
        {
            DescriptorDigest layerDigest =
                DescriptorDigest.FromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            DescriptorDigest diffId =
                DescriptorDigest.FromHash(
                    "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

            Assert.AreEqual(
                Paths.Get(
                    "cache",
                    "directory",
                    "layers",
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                TEST_CACHE_STORAGE_FILES.GetLayerFile(layerDigest, diffId));
        }

        [Test]
        public void TestGetLayerFilename()
        {
            DescriptorDigest diffId =
                DescriptorDigest.FromHash(
                    "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

            Assert.AreEqual(
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                TEST_CACHE_STORAGE_FILES.GetLayerFilename(diffId));
        }

        [Test]
        public void TestGetSelectorFile()
        {
            DescriptorDigest selector =
                DescriptorDigest.FromHash(
                    "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc");

            Assert.AreEqual(
                Paths.Get(
                    "cache",
                    "directory",
                    "selectors",
                    "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"),
                TEST_CACHE_STORAGE_FILES.GetSelectorFile(selector));
        }

        [Test]
        public void TestGetLayersDirectory()
        {
            Assert.AreEqual(
                Paths.Get("cache", "directory", "layers"), TEST_CACHE_STORAGE_FILES.GetLayersDirectory());
        }

        [Test]
        public void TestGetLayerDirectory()
        {
            DescriptorDigest layerDigest =
                DescriptorDigest.FromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            Assert.AreEqual(
                Paths.Get(
                    "cache",
                    "directory",
                    "layers",
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                TEST_CACHE_STORAGE_FILES.GetLayerDirectory(layerDigest));
        }

        [Test]
        public void TestGetTemporaryDirectory()
        {
            Assert.AreEqual(
                Paths.Get("cache/directory/tmp"), TEST_CACHE_STORAGE_FILES.GetTemporaryDirectory());
        }

        [Test]
        public void TestGetImagesDirectory()
        {
            Assert.AreEqual(
                Paths.Get("cache/directory/images"), TEST_CACHE_STORAGE_FILES.GetImagesDirectory());
        }

        [Test]
        public void TestGetImageDirectory()
        {
            SystemPath imagesDirectory = Paths.Get("cache", "directory", "images");
            Assert.AreEqual(imagesDirectory, TEST_CACHE_STORAGE_FILES.GetImagesDirectory());

            Assert.AreEqual(
                imagesDirectory.Resolve("reg.istry/repo/sitory!tag"),
                TEST_CACHE_STORAGE_FILES.GetImageDirectory(
                    ImageReference.Parse("reg.istry/repo/sitory:tag")));
            Assert.AreEqual(
                imagesDirectory.Resolve(
                    "reg.istry/repo!sha256!aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                TEST_CACHE_STORAGE_FILES.GetImageDirectory(
                    ImageReference.Parse(
                        "reg.istry/repo@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
            Assert.AreEqual(
                imagesDirectory.Resolve("reg.istry!5000/repo/sitory!tag"),
                TEST_CACHE_STORAGE_FILES.GetImageDirectory(
                    ImageReference.Parse("reg.istry:5000/repo/sitory:tag")));
        }
    }
}
