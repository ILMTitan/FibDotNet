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

using Fib.Net.Core.Api;
using Fib.Net.Core.Blob;
using Fib.Net.Core.Caching;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Images.Json;
using Fib.Net.Core.Json;
using Fib.Net.Test.Common;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Fib.Net.Core.Unit.Tests.Cache
{
    /** Tests for {@link CacheStorageWriter}. */
    public class CacheStorageWriterTest : IDisposable
    {
        private static async Task<BlobDescriptor> GetDigestAsync(IBlob blob)
        {
            return await blob.WriteToAsync(Stream.Null).ConfigureAwait(false);
        }

        private static IBlob Compress(IBlob blob)
        {
            return Blobs.From(
                async outputStream =>
                {
                    using (GZipStream compressorStream = new GZipStream(outputStream, CompressionMode.Compress, true))
                    {
                        await blob.WriteToAsync(compressorStream).ConfigureAwait(false);
                    }
                }, -1);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Memory Streams do not need to be disposed")]
        private static async Task<IBlob> DecompressAsync(IBlob blob)
        {
            return Blobs.From(new GZipStream(new MemoryStream(await Blobs.WriteToByteArrayAsync(blob).ConfigureAwait(false)), CompressionMode.Decompress), -1);
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        private CacheStorageFiles cacheStorageFiles;
        private SystemPath cacheRoot;

        [SetUp]
        public void SetUp()
        {
            cacheRoot = temporaryFolder.NewFolder().ToPath();
            cacheStorageFiles = new CacheStorageFiles(cacheRoot);
        }

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [Test]
        public async Task TestWrite_compressedAsync()
        {
            IBlob uncompressedLayerBlob = Blobs.From("uncompressedLayerBlob");

            CachedLayer cachedLayer =
                await new CacheStorageWriter(cacheStorageFiles).WriteCompressedAsync(Compress(uncompressedLayerBlob)).ConfigureAwait(false);

            await VerifyCachedLayerAsync(cachedLayer, uncompressedLayerBlob).ConfigureAwait(false);
        }

        [Test]
        public async Task TestWrite_uncompressedAsync()
        {
            IBlob uncompressedLayerBlob = Blobs.From("uncompressedLayerBlob");
            BlobDescriptor layerDigestDescriptor = await GetDigestAsync(Compress(uncompressedLayerBlob)).ConfigureAwait(false);
            DescriptorDigest layerDigest = layerDigestDescriptor.GetDigest();
            BlobDescriptor selectorDescriptor = await GetDigestAsync(Blobs.From("selector")).ConfigureAwait(false);
            DescriptorDigest selector = selectorDescriptor.GetDigest();

            CachedLayer cachedLayer =
                await new CacheStorageWriter(cacheStorageFiles)
                    .WriteUncompressedAsync(uncompressedLayerBlob, selector).ConfigureAwait(false);

            await VerifyCachedLayerAsync(cachedLayer, uncompressedLayerBlob).ConfigureAwait(false);

            // Verifies that the files are present.
            SystemPath selectorFile = cacheStorageFiles.GetSelectorFile(selector);
            Assert.IsTrue(Files.Exists(selectorFile));
            Assert.AreEqual(layerDigest.GetHash(), await Blobs.WriteToStringAsync(Blobs.From(selectorFile)).ConfigureAwait(false));
        }

        [Test]
        public async Task TestWriteMetadata_v21Async()
        {
            SystemPath manifestJsonFile =
                Paths.Get(TestResources.GetResource("core/json/v21manifest.json").ToURI());
            V21ManifestTemplate manifestTemplate =
                JsonTemplateMapper.ReadJsonFromFile<V21ManifestTemplate>(manifestJsonFile);
            ImageReference imageReference = ImageReference.Parse("image.reference/project/thing:tag");

            await new CacheStorageWriter(cacheStorageFiles).WriteMetadataAsync(imageReference, manifestTemplate).ConfigureAwait(false);

            SystemPath savedManifestPath =
                cacheRoot.Resolve("images/image.reference/project/thing!tag/manifest.json");
            Assert.IsTrue(Files.Exists(savedManifestPath));

            V21ManifestTemplate savedManifest =
                JsonTemplateMapper.ReadJsonFromFile<V21ManifestTemplate>(savedManifestPath);
            Assert.AreEqual("amd64", savedManifest.GetContainerConfiguration().Get().Architecture);
        }

        [Test]
        public async Task TestWriteMetadata_v22Async()
        {
            SystemPath containerConfigurationJsonFile =
                Paths.Get(
                    TestResources.GetResource("core/json/containerconfig.json").ToURI());
            ContainerConfigurationTemplate containerConfigurationTemplate =
                JsonTemplateMapper.ReadJsonFromFile<ContainerConfigurationTemplate>(
                    containerConfigurationJsonFile);
            SystemPath manifestJsonFile =
                Paths.Get(TestResources.GetResource("core/json/v22manifest.json").ToURI());
            IBuildableManifestTemplate manifestTemplate =
                JsonTemplateMapper.ReadJsonFromFile<V22ManifestTemplate>(manifestJsonFile);
            ImageReference imageReference = ImageReference.Parse("image.reference/project/thing:tag");

            await new CacheStorageWriter(cacheStorageFiles)
                .WriteMetadataAsync(imageReference, manifestTemplate, containerConfigurationTemplate).ConfigureAwait(false);

            SystemPath savedManifestPath =
                cacheRoot.Resolve("images/image.reference/project/thing!tag/manifest.json");
            SystemPath savedConfigPath =
                cacheRoot.Resolve("images/image.reference/project/thing!tag/config.json");
            Assert.IsTrue(Files.Exists(savedManifestPath));
            Assert.IsTrue(Files.Exists(savedConfigPath));

            V22ManifestTemplate savedManifest =
                JsonTemplateMapper.ReadJsonFromFile<V22ManifestTemplate>(savedManifestPath);
            Assert.AreEqual(
                "8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad",
                savedManifest.GetContainerConfiguration().Digest.GetHash());

            ContainerConfigurationTemplate savedContainerConfig =
                JsonTemplateMapper.ReadJsonFromFile<ContainerConfigurationTemplate>(savedConfigPath);
            Assert.AreEqual("wasm", savedContainerConfig.Architecture);
        }

        private async Task VerifyCachedLayerAsync(CachedLayer cachedLayer, IBlob uncompressedLayerBlob)
        {
            BlobDescriptor layerBlobDescriptor = await GetDigestAsync(Compress(uncompressedLayerBlob)).ConfigureAwait(false);
            BlobDescriptor layerDiffDescriptor = await GetDigestAsync(uncompressedLayerBlob).ConfigureAwait(false);
            DescriptorDigest layerDiffId = layerDiffDescriptor.GetDigest();

            // Verifies cachedLayer is correct.
            Assert.AreEqual(layerBlobDescriptor.GetDigest(), cachedLayer.GetDigest());
            Assert.AreEqual(layerDiffId, cachedLayer.GetDiffId());
            Assert.AreEqual(layerBlobDescriptor.GetSize(), cachedLayer.GetSize());
            CollectionAssert.AreEqual(
                await Blobs.WriteToByteArrayAsync(uncompressedLayerBlob).ConfigureAwait(false),
                await Blobs.WriteToByteArrayAsync(await DecompressAsync(cachedLayer.GetBlob()).ConfigureAwait(false)).ConfigureAwait(false));

            // Verifies that the files are present.
            Assert.IsTrue(
                Files.Exists(
                    cacheStorageFiles.GetLayerFile(cachedLayer.GetDigest(), cachedLayer.GetDiffId())));
        }
    }
}
