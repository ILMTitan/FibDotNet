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

using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Caching;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Core.Hash;
using Jib.Net.Test.Common;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Jib.Net.Core.Unit.Tests.Cache
{
    /** Tests for {@link Cache}. */
    public class CacheTest : IDisposable
    {
        /**
         * Gets a {@link Blob} that is {@code blob} compressed.
         *
         * @param blob the {@link Blob} to compress
         * @return the compressed {@link Blob}
         */
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

        /**
         * Gets a {@link Blob} that is {@code blob} decompressed.
         *
         * @param blob the {@link Blob} to decompress
         * @return the decompressed {@link Blob}
         * @throws IOException if an I/O exception occurs
         */
        private static async Task<IBlob> DecompressAsync(IBlob blob)
        {
            return Blobs.From(new GZipStream(new MemoryStream(await Blobs.WriteToByteArrayAsync(blob).ConfigureAwait(false)), CompressionMode.Decompress), -1);
        }

        /**
         * Gets the digest of {@code blob}.
         *
         * @param blob the {@link Blob}
         * @return the {@link DescriptorDigest} of {@code blob}
         * @throws IOException if an I/O exception occurs
         */
        private static async Task<DescriptorDigest> DigestOfAsync(IBlob blob)
        {
            BlobDescriptor descriptor = await blob.WriteToAsync(Stream.Null).ConfigureAwait(false);
            return descriptor.GetDigest();
        }

        /**
         * Gets the size of {@code blob}.
         *
         * @param blob the {@link Blob}
         * @return the size (in bytes) of {@code blob}
         * @throws IOException if an I/O exception occurs
         */
        private static async Task<long> SizeOfAsync(IBlob blob)
        {
            CountingDigestOutputStream countingOutputStream =
                new CountingDigestOutputStream(Stream.Null);
            await blob.WriteToAsync(countingOutputStream).ConfigureAwait(false);
            return countingOutputStream.GetCount();
        }

        private static LayerEntry DefaultLayerEntry(SystemPath source, AbsoluteUnixPath destination)
        {
            return new LayerEntry(
                source,
                destination,
                LayerConfiguration.DefaultFilePermissionsProvider(source, destination),
                LayerConfiguration.DefaultModifiedTime);
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        private IBlob layerBlob1;
        private DescriptorDigest layerDigest1;
        private DescriptorDigest layerDiffId1;
        private long layerSize1;
        private ImmutableArray<LayerEntry> layerEntries1;

        private IBlob layerBlob2;
        private DescriptorDigest layerDigest2;
        private DescriptorDigest layerDiffId2;
        private long layerSize2;
        private ImmutableArray<LayerEntry> layerEntries2;

        [SetUp]
        public async Task SetUpAsync()
        {
            SystemPath directory = temporaryFolder.NewFolder().ToPath();
            Files.CreateDirectory(directory.Resolve("source"));
            Files.CreateFile(directory.Resolve("source/file"));
            Files.CreateDirectories(directory.Resolve("another/source"));
            Files.CreateFile(directory.Resolve("another/source/file"));

            layerBlob1 = Blobs.From("layerBlob1");
            layerDigest1 = await DigestOfAsync(Compress(layerBlob1)).ConfigureAwait(false);
            layerDiffId1 = await DigestOfAsync(layerBlob1).ConfigureAwait(false);
            layerSize1 = await SizeOfAsync(Compress(layerBlob1)).ConfigureAwait(false);
            layerEntries1 =
                ImmutableArray.Create(
                    DefaultLayerEntry(
                        directory.Resolve("source/file"), AbsoluteUnixPath.Get("/extraction/path")),
                    DefaultLayerEntry(
                        directory.Resolve("another/source/file"),
                        AbsoluteUnixPath.Get("/another/extraction/path")));

            layerBlob2 = Blobs.From("layerBlob2");
            layerDigest2 = await DigestOfAsync(Compress(layerBlob2)).ConfigureAwait(false);
            layerDiffId2 = await DigestOfAsync(layerBlob2).ConfigureAwait(false);
            layerSize2 = await SizeOfAsync(Compress(layerBlob2)).ConfigureAwait(false);
            layerEntries2 = ImmutableArray.Create<LayerEntry>();
        }

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [Test]
        public void TestWithDirectory_existsButNotDirectory()
        {
            SystemPath file = temporaryFolder.NewFile().ToPath();

            try
            {
                LayersCache.WithDirectory(file);
                Assert.Fail();
            }
            catch (IOException)
            {
                // pass
            }
        }

        [Test]
        public async Task TestWriteCompressed_retrieveByLayerDigestAsync()
        {
            LayersCache cache = LayersCache.WithDirectory(temporaryFolder.NewFolder().ToPath());

            await VerifyIsLayer1Async(await cache.WriteCompressedLayerAsync(Compress(layerBlob1)).ConfigureAwait(false)).ConfigureAwait(false);
            await VerifyIsLayer1Async(cache.Retrieve(layerDigest1).OrElseThrow(() => new AssertionException(""))).ConfigureAwait(false);
            Assert.IsFalse(cache.Retrieve(layerDigest2).IsPresent());
        }

        [Test]
        public async Task TestWriteUncompressedWithLayerEntries_retrieveByLayerDigestAsync()
        {
            LayersCache cache = LayersCache.WithDirectory(temporaryFolder.NewFolder().ToPath());

            await VerifyIsLayer1Async(await cache.WriteUncompressedLayerAsync(layerBlob1, layerEntries1).ConfigureAwait(false)).ConfigureAwait(false);
            await VerifyIsLayer1Async(cache.Retrieve(layerDigest1).OrElseThrow(() => new AssertionException(""))).ConfigureAwait(false);
            Assert.IsFalse(cache.Retrieve(layerDigest2).IsPresent());
        }

        [Test]
        public async Task TestWriteUncompressedWithLayerEntries_retrieveByLayerEntriesAsync()
        {
            LayersCache cache = LayersCache.WithDirectory(temporaryFolder.NewFolder().ToPath());

            await VerifyIsLayer1Async(await cache.WriteUncompressedLayerAsync(layerBlob1, layerEntries1).ConfigureAwait(false)).ConfigureAwait(false);
            Maybe<CachedLayer> layer = await cache.RetrieveAsync(layerEntries1).ConfigureAwait(false);
            await VerifyIsLayer1Async(layer.OrElseThrow(() => new AssertionException(""))).ConfigureAwait(false);
            Assert.IsFalse(cache.Retrieve(layerDigest2).IsPresent());

            // A source file modification results in the cached layer to be out-of-date and not retrieved.
            Files.SetLastModifiedTime(
                layerEntries1[0].SourceFile, FileTime.From(SystemClock.Instance.GetCurrentInstant() + Duration.FromSeconds(1)));
            Maybe<CachedLayer> outOfDateLayer = await cache.RetrieveAsync(layerEntries1).ConfigureAwait(false);
            Assert.IsFalse(outOfDateLayer.IsPresent());
        }

        [Test]
        public async Task TestRetrieveWithTwoEntriesInCacheAsync()
        {
            LayersCache cache = LayersCache.WithDirectory(temporaryFolder.NewFolder().ToPath());

            await VerifyIsLayer1Async(await cache.WriteUncompressedLayerAsync(layerBlob1, layerEntries1).ConfigureAwait(false)).ConfigureAwait(false);
            await VerifyIsLayer2Async(await cache.WriteUncompressedLayerAsync(layerBlob2, layerEntries2).ConfigureAwait(false)).ConfigureAwait(false);
            await VerifyIsLayer1Async(cache.Retrieve(layerDigest1).OrElseThrow(() => new AssertionException(""))).ConfigureAwait(false);
            await VerifyIsLayer2Async(cache.Retrieve(layerDigest2).OrElseThrow(() => new AssertionException(""))).ConfigureAwait(false);
            Maybe<CachedLayer> cachedLayer1 = await cache.RetrieveAsync(layerEntries1).ConfigureAwait(false);
            await VerifyIsLayer1Async(cachedLayer1.OrElseThrow(() => new AssertionException(""))).ConfigureAwait(false);
            Maybe<CachedLayer> cachedLayer2 = await cache.RetrieveAsync(layerEntries2).ConfigureAwait(false);
            await VerifyIsLayer2Async(cachedLayer2.OrElseThrow(() => new AssertionException(""))).ConfigureAwait(false);
        }

        /**
         * Verifies that {@code cachedLayer} corresponds to the first fake layer in {@link #setUp}.
         *
         * @param cachedLayer the {@link CachedLayer} to verify
         * @throws IOException if an I/O exception occurs
         */
        private async Task VerifyIsLayer1Async(CachedLayer cachedLayer)
        {
            Assert.AreEqual("layerBlob1", await Blobs.WriteToStringAsync(await DecompressAsync(cachedLayer.GetBlob()).ConfigureAwait(false)).ConfigureAwait(false));
            Assert.AreEqual(layerDigest1, cachedLayer.GetDigest());
            Assert.AreEqual(layerDiffId1, cachedLayer.GetDiffId());
            Assert.AreEqual(layerSize1, cachedLayer.GetSize());
        }

        /**
         * Verifies that {@code cachedLayer} corresponds to the second fake layer in {@link #setUp}.
         *
         * @param cachedLayer the {@link CachedLayer} to verify
         * @throws IOException if an I/O exception occurs
         */
        private async Task VerifyIsLayer2Async(CachedLayer cachedLayer)
        {
            Assert.AreEqual("layerBlob2", await Blobs.WriteToStringAsync(await DecompressAsync(cachedLayer.GetBlob()).ConfigureAwait(false)).ConfigureAwait(false));
            Assert.AreEqual(layerDigest2, cachedLayer.GetDigest());
            Assert.AreEqual(layerDiffId2, cachedLayer.GetDiffId());
            Assert.AreEqual(layerSize2, cachedLayer.GetSize());
        }
    }
}
