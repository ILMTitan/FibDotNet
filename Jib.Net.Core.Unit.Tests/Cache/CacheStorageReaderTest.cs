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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Cache;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Test.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.cache
{
    /** Tests for {@link CacheStorageReader}. */
    public class CacheStorageReaderTest : IDisposable
    {
        private static void SetupCachedMetadataV21(SystemPath cacheDirectory)
        {
            SystemPath imageDirectory = cacheDirectory.Resolve("images/test/image!tag");
            Files.CreateDirectories(imageDirectory);
            Files.Copy(
                Paths.Get(TestResources.GetResource("core/json/v21manifest.json").ToURI()),
                imageDirectory.Resolve("manifest.json"));
        }

        private static void SetupCachedMetadataV22(SystemPath cacheDirectory)
        {
            SystemPath imageDirectory = cacheDirectory.Resolve("images/test/image!tag");
            Files.CreateDirectories(imageDirectory);
            Files.Copy(
                Paths.Get(TestResources.GetResource("core/json/v22manifest.json").ToURI()),
                imageDirectory.Resolve("manifest.json"));
            Files.Copy(
                Paths.Get(TestResources.GetResource("core/json/containerconfig.json").ToURI()),
                imageDirectory.Resolve("config.json"));
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        private DescriptorDigest layerDigest1;
        private DescriptorDigest layerDigest2;

        [SetUp]
        public void SetUp()
        {
            layerDigest1 =
                DescriptorDigest.FromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            layerDigest2 =
                DescriptorDigest.FromHash(
                    "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        }

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [Test]
        public void TestListDigests()
        {
            CacheStorageFiles cacheStorageFiles =
                new CacheStorageFiles(temporaryFolder.NewFolder().ToPath());

            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            // Creates test layer directories.
            Files.CreateDirectories(cacheStorageFiles.GetLayersDirectory().Resolve(layerDigest1.GetHash()));
            Files.CreateDirectories(cacheStorageFiles.GetLayersDirectory().Resolve(layerDigest2.GetHash()));

            // Checks that layer directories created are all listed.
            Assert.AreEqual(
                new HashSet<DescriptorDigest>(Arrays.AsList(layerDigest1, layerDigest2)),
                cacheStorageReader.FetchDigests());

            // Checks that non-digest directories means the cache is corrupted.
            Files.CreateDirectory(cacheStorageFiles.GetLayersDirectory().Resolve("not a hash"));
            try
            {
                cacheStorageReader.FetchDigests();
                Assert.Fail("Listing digests should have failed");
            }
            catch (CacheCorruptedException ex)
            {
                Assert.That(
                    ex.GetMessage(),Does.StartWith("Found non-digest file in layers directory"));
                Assert.IsInstanceOf<DigestException>(ex.GetCause());
            }
        }

        [Test]
        public void TestRetrieveManifest_v21()
        {
            SystemPath cacheDirectory = temporaryFolder.NewFolder().ToPath();
            SetupCachedMetadataV21(cacheDirectory);

            CacheStorageFiles cacheStorageFiles = new CacheStorageFiles(cacheDirectory);
            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            V21ManifestTemplate manifestTemplate =
                (V21ManifestTemplate)
                    cacheStorageReader
                        .RetrieveMetadata(ImageReference.Of("test", "image", "tag"))
                        .Get()
                        .GetManifest();
            Assert.AreEqual(1, manifestTemplate.SchemaVersion);
        }

        [Test]
        public void TestRetrieveManifest_v22()
        {
            SystemPath cacheDirectory = temporaryFolder.NewFolder().ToPath();
            SetupCachedMetadataV22(cacheDirectory);

            CacheStorageFiles cacheStorageFiles = new CacheStorageFiles(cacheDirectory);
            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            V22ManifestTemplate manifestTemplate =
                (V22ManifestTemplate)
                    cacheStorageReader
                        .RetrieveMetadata(ImageReference.Of("test", "image", "tag"))
                        .Get()
                        .GetManifest();
            Assert.AreEqual(2, manifestTemplate.SchemaVersion);
        }

        [Test]
        public void TestRetrieveContainerConfiguration()
        {
            SystemPath cacheDirectory = temporaryFolder.NewFolder().ToPath();
            SetupCachedMetadataV22(cacheDirectory);

            CacheStorageFiles cacheStorageFiles = new CacheStorageFiles(cacheDirectory);
            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            ContainerConfigurationTemplate configurationTemplate =
                cacheStorageReader
                    .RetrieveMetadata(ImageReference.Of("test", "image", "tag"))
                    .Get()
                    .GetConfig()
                    .Get();
            Assert.AreEqual("wasm", configurationTemplate.Architecture);
            Assert.AreEqual("js", configurationTemplate.Os);
        }

        [Test]
        public async Task TestRetrieveAsync()
        {
            CacheStorageFiles cacheStorageFiles =
                new CacheStorageFiles(temporaryFolder.NewFolder().ToPath());

            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            // Creates the test layer directory.
            DescriptorDigest layerDigest = layerDigest1;
            DescriptorDigest layerDiffId = layerDigest2;
            Files.CreateDirectories(cacheStorageFiles.GetLayerDirectory(layerDigest));
            using (Stream @out =
                Files.NewOutputStream(cacheStorageFiles.GetLayerFile(layerDigest, layerDiffId)))
            {
                JavaExtensions.Write(@out, "layerBlob".GetBytes(Encoding.UTF8));
            }

            // Checks that the CachedLayer is retrieved correctly.
            Option<CachedLayer> optionalCachedLayer = cacheStorageReader.Retrieve(layerDigest);
            Assert.IsTrue(optionalCachedLayer.IsPresent());
            Assert.AreEqual(layerDigest, optionalCachedLayer.Get().GetDigest());
            Assert.AreEqual(layerDiffId, optionalCachedLayer.Get().GetDiffId());
            Assert.AreEqual("layerBlob".Length(), optionalCachedLayer.Get().GetSize());
            Assert.AreEqual("layerBlob", await Blobs.WriteToStringAsync(optionalCachedLayer.Get().GetBlob()).ConfigureAwait(false));

            // Checks that multiple .layer files means the cache is corrupted.
            Files.CreateFile(cacheStorageFiles.GetLayerFile(layerDigest, layerDigest));
            try
            {
                cacheStorageReader.Retrieve(layerDigest);
                Assert.Fail("Should have thrown CacheCorruptedException");
            }
            catch (CacheCorruptedException ex)
            {
                Assert.That(
                    ex.GetMessage(), Does.StartWith(
                        "Multiple layer files found for layer with digest "
                            + layerDigest.GetHash()
                            + " in directory: "
                            + cacheStorageFiles.GetLayerDirectory(layerDigest)));
            }
        }

        [Test]
        public void TestSelect_invalidLayerDigest()
        {
            CacheStorageFiles cacheStorageFiles =
                new CacheStorageFiles(temporaryFolder.NewFolder().ToPath());

            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            DescriptorDigest selector = layerDigest1;
            SystemPath selectorFile = cacheStorageFiles.GetSelectorFile(selector);
            Files.CreateDirectories(selectorFile.GetParent());
            Files.Write(selectorFile, "not a valid layer digest".GetBytes(Encoding.UTF8));

            try
            {
                cacheStorageReader.Select(selector);
                Assert.Fail("Should have thrown CacheCorruptedException");
            }
            catch (CacheCorruptedException ex)
            {
                Assert.That(
                    ex.GetMessage(),
                    Does.StartWith(
                        "Expected valid layer digest as contents of selector file `"
                            + selectorFile
                            + "` for selector `aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa`, but got: not a valid layer digest"));
            }
        }

        [Test]
        public void TestSelect()
        {
            CacheStorageFiles cacheStorageFiles =
                new CacheStorageFiles(temporaryFolder.NewFolder().ToPath());

            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            DescriptorDigest selector = layerDigest1;
            SystemPath selectorFile = cacheStorageFiles.GetSelectorFile(selector);
            Files.CreateDirectories(selectorFile.GetParent());
            Files.Write(selectorFile, layerDigest2.GetHash().GetBytes(Encoding.UTF8));

            Option<DescriptorDigest> selectedLayerDigest = cacheStorageReader.Select(selector);
            Assert.IsTrue(selectedLayerDigest.IsPresent());
            Assert.AreEqual(layerDigest2, selectedLayerDigest.Get());
        }
    }
}
