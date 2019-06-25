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
using com.google.cloud.tools.jib.image.json;
using com.google.cloud.tools.jib.json;
using ICSharpCode.SharpZipLib.GZip;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.cache
{
    /** Tests for {@link CacheStorageWriter}. */
    public class CacheStorageWriterTest : IDisposable
    {
        private static async Task<BlobDescriptor> getDigestAsync(IBlob blob)
        {
            return await blob.writeToAsync(Stream.Null).ConfigureAwait(false);
        }

        private static IBlob compress(IBlob blob)
        {
            return Blobs.from(
                async outputStream =>
                {
                    using (GZipStream compressorStream = new GZipStream(outputStream, CompressionMode.Compress, true))
                    {
                        await blob.writeToAsync(compressorStream).ConfigureAwait(false);
                    }
                }, -1);
        }

        private static async Task<IBlob> decompressAsync(IBlob blob)
        {
            return Blobs.from(new GZipStream(new MemoryStream(await Blobs.writeToByteArrayAsync(blob).ConfigureAwait(false)), CompressionMode.Decompress), -1);
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        private CacheStorageFiles cacheStorageFiles;
        private SystemPath cacheRoot;

        [SetUp]
        public void setUp()
        {
            cacheRoot = temporaryFolder.newFolder().toPath();
            cacheStorageFiles = new CacheStorageFiles(cacheRoot);
        }

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [Test]
        public async Task testWrite_compressedAsync()
        {
            IBlob uncompressedLayerBlob = Blobs.from("uncompressedLayerBlob");

            CachedLayer cachedLayer =
                await new CacheStorageWriter(cacheStorageFiles).writeCompressedAsync(compress(uncompressedLayerBlob)).ConfigureAwait(false);

            await verifyCachedLayerAsync(cachedLayer, uncompressedLayerBlob).ConfigureAwait(false);
        }

        [Test]
        public async Task testWrite_uncompressedAsync()
        {
            IBlob uncompressedLayerBlob = Blobs.from("uncompressedLayerBlob");
            BlobDescriptor layerDigestDescriptor = await getDigestAsync(compress(uncompressedLayerBlob)).ConfigureAwait(false);
            DescriptorDigest layerDigest = layerDigestDescriptor.getDigest();
            BlobDescriptor selectorDescriptor = await getDigestAsync(Blobs.from("selector")).ConfigureAwait(false);
            DescriptorDigest selector = selectorDescriptor.getDigest();

            CachedLayer cachedLayer =
                await new CacheStorageWriter(cacheStorageFiles)
                    .writeUncompressedAsync(uncompressedLayerBlob, selector).ConfigureAwait(false);

            await verifyCachedLayerAsync(cachedLayer, uncompressedLayerBlob).ConfigureAwait(false);

            // Verifies that the files are present.
            SystemPath selectorFile = cacheStorageFiles.getSelectorFile(selector);
            Assert.IsTrue(Files.exists(selectorFile));
            Assert.AreEqual(layerDigest.getHash(), await Blobs.writeToStringAsync(Blobs.from(selectorFile)).ConfigureAwait(false));
        }

        [Test]
        public void testWriteMetadata_v21()
        {
            SystemPath manifestJsonFile =
                Paths.get(Resources.getResource("core/json/v21manifest.json").toURI());
            V21ManifestTemplate manifestTemplate =
                JsonTemplateMapper.readJsonFromFile<V21ManifestTemplate>(manifestJsonFile);
            ImageReference imageReference = ImageReference.parse("image.reference/project/thing:tag");

            new CacheStorageWriter(cacheStorageFiles).writeMetadata(imageReference, manifestTemplate);

            SystemPath savedManifestPath =
                cacheRoot.resolve("images/image.reference/project/thing!tag/manifest.json");
            Assert.IsTrue(Files.exists(savedManifestPath));

            V21ManifestTemplate savedManifest =
                JsonTemplateMapper.readJsonFromFile<V21ManifestTemplate>(savedManifestPath);
            Assert.AreEqual("amd64", savedManifest.getContainerConfiguration().get().getArchitecture());
        }

        [Test]
        public void testWriteMetadata_v22()
        {
            SystemPath containerConfigurationJsonFile =
                Paths.get(
                    Resources.getResource("core/json/containerconfig.json").toURI());
            ContainerConfigurationTemplate containerConfigurationTemplate =
                JsonTemplateMapper.readJsonFromFile<ContainerConfigurationTemplate>(
                    containerConfigurationJsonFile);
            SystemPath manifestJsonFile =
                Paths.get(Resources.getResource("core/json/v22manifest.json").toURI());
            IBuildableManifestTemplate manifestTemplate =
                JsonTemplateMapper.readJsonFromFile<V22ManifestTemplate>(manifestJsonFile);
            ImageReference imageReference = ImageReference.parse("image.reference/project/thing:tag");

            new CacheStorageWriter(cacheStorageFiles)
                .writeMetadata(imageReference, manifestTemplate, containerConfigurationTemplate);

            SystemPath savedManifestPath =
                cacheRoot.resolve("images/image.reference/project/thing!tag/manifest.json");
            SystemPath savedConfigPath =
                cacheRoot.resolve("images/image.reference/project/thing!tag/config.json");
            Assert.IsTrue(Files.exists(savedManifestPath));
            Assert.IsTrue(Files.exists(savedConfigPath));

            V22ManifestTemplate savedManifest =
                JsonTemplateMapper.readJsonFromFile<V22ManifestTemplate>(savedManifestPath);
            Assert.AreEqual(
                "8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad",
                savedManifest.getContainerConfiguration().getDigest().getHash());

            ContainerConfigurationTemplate savedContainerConfig =
                JsonTemplateMapper.readJsonFromFile<ContainerConfigurationTemplate>(savedConfigPath);
            Assert.AreEqual("wasm", savedContainerConfig.getArchitecture());
        }

        private async Task verifyCachedLayerAsync(CachedLayer cachedLayer, IBlob uncompressedLayerBlob)
        {
            BlobDescriptor layerBlobDescriptor = await getDigestAsync(compress(uncompressedLayerBlob)).ConfigureAwait(false);
            BlobDescriptor layerDiffDescriptor = await getDigestAsync(uncompressedLayerBlob).ConfigureAwait(false);
            DescriptorDigest layerDiffId = layerDiffDescriptor.getDigest();

            // Verifies cachedLayer is correct.
            Assert.AreEqual(layerBlobDescriptor.getDigest(), cachedLayer.getDigest());
            Assert.AreEqual(layerDiffId, cachedLayer.getDiffId());
            Assert.AreEqual(layerBlobDescriptor.getSize(), cachedLayer.getSize());
            CollectionAssert.AreEqual(
                await Blobs.writeToByteArrayAsync(uncompressedLayerBlob).ConfigureAwait(false),
                await Blobs.writeToByteArrayAsync(await decompressAsync(cachedLayer.getBlob()).ConfigureAwait(false)).ConfigureAwait(false));

            // Verifies that the files are present.
            Assert.IsTrue(
                Files.exists(
                    cacheStorageFiles.getLayerFile(cachedLayer.getDigest(), cachedLayer.getDiffId())));
        }
    }
}
