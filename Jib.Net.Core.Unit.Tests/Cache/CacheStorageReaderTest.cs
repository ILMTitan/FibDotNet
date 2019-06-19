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
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.cache
{
    /** Tests for {@link CacheStorageReader}. */
    public class CacheStorageReaderTest
    {
        private static void setupCachedMetadataV21(SystemPath cacheDirectory)
        {
            SystemPath imageDirectory = cacheDirectory.resolve("images/test/image!tag");
            Files.createDirectories(imageDirectory);
            Files.copy(
                Paths.get(Resources.getResource("core/json/v21manifest.json").toURI()),
                imageDirectory.resolve("manifest.json"));
        }

        private static void setupCachedMetadataV22(SystemPath cacheDirectory)
        {
            SystemPath imageDirectory = cacheDirectory.resolve("images/test/image!tag");
            Files.createDirectories(imageDirectory);
            Files.copy(
                Paths.get(Resources.getResource("core/json/v22manifest.json").toURI()),
                imageDirectory.resolve("manifest.json"));
            Files.copy(
                Paths.get(Resources.getResource("core/json/containerconfig.json").toURI()),
                imageDirectory.resolve("config.json"));
        }

        public readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        private DescriptorDigest layerDigest1;
        private DescriptorDigest layerDigest2;

        [SetUp]
        public void setUp()
        {
            layerDigest1 =
                DescriptorDigest.fromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            layerDigest2 =
                DescriptorDigest.fromHash(
                    "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        }

        [Test]
        public void testListDigests()
        {
            CacheStorageFiles cacheStorageFiles =
                new CacheStorageFiles(temporaryFolder.newFolder().toPath());

            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            // Creates test layer directories.
            Files.createDirectories(cacheStorageFiles.getLayersDirectory().resolve(layerDigest1.getHash()));
            Files.createDirectories(cacheStorageFiles.getLayersDirectory().resolve(layerDigest2.getHash()));

            // Checks that layer directories created are all listed.
            Assert.AreEqual(
                new HashSet<DescriptorDigest>(Arrays.asList(layerDigest1, layerDigest2)),
                cacheStorageReader.fetchDigests());

            // Checks that non-digest directories means the cache is corrupted.
            Files.createDirectory(cacheStorageFiles.getLayersDirectory().resolve("not a hash"));
            try
            {
                cacheStorageReader.fetchDigests();
                Assert.Fail("Listing digests should have failed");
            }
            catch (CacheCorruptedException ex)
            {
                Assert.That(
                    ex.getMessage(),Does.StartWith("Found non-digest file in layers directory"));
                Assert.IsInstanceOf<DigestException>(ex.getCause());
            }
        }

        [Test]
        public void testRetrieveManifest_v21()
        {
            SystemPath cacheDirectory = temporaryFolder.newFolder().toPath();
            setupCachedMetadataV21(cacheDirectory);

            CacheStorageFiles cacheStorageFiles = new CacheStorageFiles(cacheDirectory);
            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            V21ManifestTemplate manifestTemplate =
                (V21ManifestTemplate)
                    cacheStorageReader
                        .retrieveMetadata(ImageReference.of("test", "image", "tag"))
                        .get()
                        .getManifest();
            Assert.AreEqual(1, manifestTemplate.getSchemaVersion());
        }

        [Test]
        public void testRetrieveManifest_v22()
        {
            SystemPath cacheDirectory = temporaryFolder.newFolder().toPath();
            setupCachedMetadataV22(cacheDirectory);

            CacheStorageFiles cacheStorageFiles = new CacheStorageFiles(cacheDirectory);
            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            V22ManifestTemplate manifestTemplate =
                (V22ManifestTemplate)
                    cacheStorageReader
                        .retrieveMetadata(ImageReference.of("test", "image", "tag"))
                        .get()
                        .getManifest();
            Assert.AreEqual(2, manifestTemplate.getSchemaVersion());
        }

        [Test]
        public void testRetrieveContainerConfiguration()
        {
            SystemPath cacheDirectory = temporaryFolder.newFolder().toPath();
            setupCachedMetadataV22(cacheDirectory);

            CacheStorageFiles cacheStorageFiles = new CacheStorageFiles(cacheDirectory);
            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            ContainerConfigurationTemplate configurationTemplate =
                cacheStorageReader
                    .retrieveMetadata(ImageReference.of("test", "image", "tag"))
                    .get()
                    .getConfig()
                    .get();
            Assert.AreEqual("wasm", configurationTemplate.getArchitecture());
            Assert.AreEqual("js", configurationTemplate.getOs());
        }

        [Test]
        public async Task testRetrieveAsync()
        {
            CacheStorageFiles cacheStorageFiles =
                new CacheStorageFiles(temporaryFolder.newFolder().toPath());

            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            // Creates the test layer directory.
            DescriptorDigest layerDigest = layerDigest1;
            DescriptorDigest layerDiffId = layerDigest2;
            Files.createDirectories(cacheStorageFiles.getLayerDirectory(layerDigest));
            using (Stream @out =
                Files.newOutputStream(cacheStorageFiles.getLayerFile(layerDigest, layerDiffId)))
            {
                @out.write("layerBlob".getBytes(StandardCharsets.UTF_8));
            }

            // Checks that the CachedLayer is retrieved correctly.
            Optional<CachedLayer> optionalCachedLayer = cacheStorageReader.retrieve(layerDigest);
            Assert.IsTrue(optionalCachedLayer.isPresent());
            Assert.AreEqual(layerDigest, optionalCachedLayer.get().getDigest());
            Assert.AreEqual(layerDiffId, optionalCachedLayer.get().getDiffId());
            Assert.AreEqual("layerBlob".length(), optionalCachedLayer.get().getSize());
            Assert.AreEqual("layerBlob", await Blobs.writeToStringAsync(optionalCachedLayer.get().getBlob()).ConfigureAwait(false));

            // Checks that multiple .layer files means the cache is corrupted.
            Files.createFile(cacheStorageFiles.getLayerFile(layerDigest, layerDigest));
            try
            {
                cacheStorageReader.retrieve(layerDigest);
                Assert.Fail("Should have thrown CacheCorruptedException");
            }
            catch (CacheCorruptedException ex)
            {
                Assert.That(
                    ex.getMessage(), Does.StartWith(
                        "Multiple layer files found for layer with digest "
                            + layerDigest.getHash()
                            + " in directory: "
                            + cacheStorageFiles.getLayerDirectory(layerDigest)));
            }
        }

        [Test]
        public void testSelect_invalidLayerDigest()
        {
            CacheStorageFiles cacheStorageFiles =
                new CacheStorageFiles(temporaryFolder.newFolder().toPath());

            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            DescriptorDigest selector = layerDigest1;
            SystemPath selectorFile = cacheStorageFiles.getSelectorFile(selector);
            Files.createDirectories(selectorFile.getParent());
            Files.write(selectorFile, "not a valid layer digest".getBytes(StandardCharsets.UTF_8));

            try
            {
                cacheStorageReader.select(selector);
                Assert.Fail("Should have thrown CacheCorruptedException");
            }
            catch (CacheCorruptedException ex)
            {
                Assert.That(
                    ex.getMessage(),
                    Does.StartWith(
                        "Expected valid layer digest as contents of selector file `"
                            + selectorFile
                            + "` for selector `aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa`, but got: not a valid layer digest"));
            }
        }

        [Test]
        public void testSelect()
        {
            CacheStorageFiles cacheStorageFiles =
                new CacheStorageFiles(temporaryFolder.newFolder().toPath());

            CacheStorageReader cacheStorageReader = new CacheStorageReader(cacheStorageFiles);

            DescriptorDigest selector = layerDigest1;
            SystemPath selectorFile = cacheStorageFiles.getSelectorFile(selector);
            Files.createDirectories(selectorFile.getParent());
            Files.write(selectorFile, layerDigest2.getHash().getBytes(StandardCharsets.UTF_8));

            Optional<DescriptorDigest> selectedLayerDigest = cacheStorageReader.select(selector);
            Assert.IsTrue(selectedLayerDigest.isPresent());
            Assert.AreEqual(layerDigest2, selectedLayerDigest.get());
        }
    }
}
