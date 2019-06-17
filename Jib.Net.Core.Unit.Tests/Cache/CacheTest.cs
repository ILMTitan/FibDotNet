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
using com.google.cloud.tools.jib.hash;
using ICSharpCode.SharpZipLib.GZip;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NodaTime;
using NUnit.Framework;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.cache
{














    /** Tests for {@link Cache}. */
    public class CacheTest
    {
        /**
         * Gets a {@link Blob} that is {@code blob} compressed.
         *
         * @param blob the {@link Blob} to compress
         * @return the compressed {@link Blob}
         */
        private static Blob compress(Blob blob)
        {
            return Blobs.from(
                async outputStream =>
                {
                    using (GZipStream compressorStream = new GZipStream(outputStream, CompressionMode.Compress, true))
                    {
                        await blob.writeToAsync(compressorStream);
                    }
                });
        }

        /**
         * Gets a {@link Blob} that is {@code blob} decompressed.
         *
         * @param blob the {@link Blob} to decompress
         * @return the decompressed {@link Blob}
         * @throws IOException if an I/O exception occurs
         */
        private static async Task<Blob> decompressAsync(Blob blob)
        {
            return Blobs.from(new GZipStream(new MemoryStream(await Blobs.writeToByteArrayAsync(blob)), CompressionMode.Decompress));
        }

        /**
         * Gets the digest of {@code blob}.
         *
         * @param blob the {@link Blob}
         * @return the {@link DescriptorDigest} of {@code blob}
         * @throws IOException if an I/O exception occurs
         */
        private static async Task<DescriptorDigest> digestOfAsync(Blob blob)
        {
            BlobDescriptor descriptor = await blob.writeToAsync(Stream.Null);
            return descriptor.getDigest();
        }

        /**
         * Gets the size of {@code blob}.
         *
         * @param blob the {@link Blob}
         * @return the size (in bytes) of {@code blob}
         * @throws IOException if an I/O exception occurs
         */
        private static async Task<long> sizeOfAsync(Blob blob)
        {
            CountingDigestOutputStream countingOutputStream =
                new CountingDigestOutputStream(Stream.Null);
            await blob.writeToAsync(countingOutputStream);
            return countingOutputStream.getCount();
        }

        private static LayerEntry defaultLayerEntry(SystemPath source, AbsoluteUnixPath destination)
        {
            return new LayerEntry(
                source,
                destination,
                LayerConfiguration.DEFAULT_FILE_PERMISSIONS_PROVIDER(source, destination),
                LayerConfiguration.DEFAULT_MODIFIED_TIME);
        }

        [Rule] public readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        private Blob layerBlob1;
        private DescriptorDigest layerDigest1;
        private DescriptorDigest layerDiffId1;
        private long layerSize1;
        private ImmutableArray<LayerEntry> layerEntries1;

        private Blob layerBlob2;
        private DescriptorDigest layerDigest2;
        private DescriptorDigest layerDiffId2;
        private long layerSize2;
        private ImmutableArray<LayerEntry> layerEntries2;

        [SetUp]
        public async Task setUpAsync()
        {
            SystemPath directory = temporaryFolder.newFolder().toPath();
            Files.createDirectory(directory.resolve("source"));
            Files.createFile(directory.resolve("source/file"));
            Files.createDirectories(directory.resolve("another/source"));
            Files.createFile(directory.resolve("another/source/file"));

            layerBlob1 = Blobs.from("layerBlob1");
            layerDigest1 = await digestOfAsync(compress(layerBlob1));
            layerDiffId1 = await digestOfAsync(layerBlob1);
            layerSize1 = await sizeOfAsync(compress(layerBlob1));
            layerEntries1 =
                ImmutableArray.Create(
                    defaultLayerEntry(
                        directory.resolve("source/file"), AbsoluteUnixPath.get("/extraction/path")),
                    defaultLayerEntry(
                        directory.resolve("another/source/file"),
                        AbsoluteUnixPath.get("/another/extraction/path")));

            layerBlob2 = Blobs.from("layerBlob2");
            layerDigest2 = await digestOfAsync(compress(layerBlob2));
            layerDiffId2 = await digestOfAsync(layerBlob2);
            layerSize2 = await sizeOfAsync(compress(layerBlob2));
            layerEntries2 = ImmutableArray.Create<LayerEntry>();
        }

        [Test]
        public void testWithDirectory_existsButNotDirectory()
        {
            SystemPath file = temporaryFolder.newFile().toPath();

            try
            {
                Cache.withDirectory(file);
                Assert.Fail();
            }
            catch (IOException)
            {
                // pass
            }
        }

        [Test]
        public async Task testWriteCompressed_retrieveByLayerDigestAsync()
        {
            Cache cache = Cache.withDirectory(temporaryFolder.newFolder().toPath());

            await verifyIsLayer1Async(await cache.writeCompressedLayerAsync(compress(layerBlob1)));
            await verifyIsLayer1Async(cache.retrieve(layerDigest1).orElseThrow(() => new AssertionException("")));
            Assert.IsFalse(cache.retrieve(layerDigest2).isPresent());
        }

        [Test]
        public async Task testWriteUncompressedWithLayerEntries_retrieveByLayerDigestAsync()
        {
            Cache cache = Cache.withDirectory(temporaryFolder.newFolder().toPath());

            await verifyIsLayer1Async(await cache.writeUncompressedLayerAsync(layerBlob1, layerEntries1));
            await verifyIsLayer1Async(cache.retrieve(layerDigest1).orElseThrow(() => new AssertionException("")));
            Assert.IsFalse(cache.retrieve(layerDigest2).isPresent());
        }

        [Test]
        public async Task testWriteUncompressedWithLayerEntries_retrieveByLayerEntriesAsync()
        {
            Cache cache = Cache.withDirectory(temporaryFolder.newFolder().toPath());

            await verifyIsLayer1Async(await cache.writeUncompressedLayerAsync(layerBlob1, layerEntries1));
            await verifyIsLayer1Async(cache.retrieve(layerEntries1).orElseThrow(() => new AssertionException("")));
            Assert.IsFalse(cache.retrieve(layerDigest2).isPresent());

            // A source file modification results in the cached layer to be out-of-date and not retrieved.
            Files.setLastModifiedTime(
                layerEntries1.get(0).getSourceFile(), FileTime.from(SystemClock.Instance.GetCurrentInstant().plusSeconds(1)));
            Assert.IsFalse(cache.retrieve(layerEntries1).isPresent());
        }

        [Test]
        public async Task testRetrieveWithTwoEntriesInCacheAsync()
        {
            Cache cache = Cache.withDirectory(temporaryFolder.newFolder().toPath());

            await verifyIsLayer1Async(await cache.writeUncompressedLayerAsync(layerBlob1, layerEntries1));
            await verifyIsLayer2Async(await cache.writeUncompressedLayerAsync(layerBlob2, layerEntries2));
            await verifyIsLayer1Async(cache.retrieve(layerDigest1).orElseThrow(() => new AssertionException("")));
            await verifyIsLayer2Async(cache.retrieve(layerDigest2).orElseThrow(() => new AssertionException("")));
            await verifyIsLayer1Async(cache.retrieve(layerEntries1).orElseThrow(() => new AssertionException("")));
            await verifyIsLayer2Async(cache.retrieve(layerEntries2).orElseThrow(() => new AssertionException("")));
        }

        /**
         * Verifies that {@code cachedLayer} corresponds to the first fake layer in {@link #setUp}.
         *
         * @param cachedLayer the {@link CachedLayer} to verify
         * @throws IOException if an I/O exception occurs
         */
        private async Task verifyIsLayer1Async(CachedLayer cachedLayer)
        {
            Assert.AreEqual("layerBlob1", await Blobs.writeToStringAsync(await decompressAsync(cachedLayer.getBlob())));
            Assert.AreEqual(layerDigest1, cachedLayer.getDigest());
            Assert.AreEqual(layerDiffId1, cachedLayer.getDiffId());
            Assert.AreEqual(layerSize1, cachedLayer.getSize());
        }

        /**
         * Verifies that {@code cachedLayer} corresponds to the second fake layer in {@link #setUp}.
         *
         * @param cachedLayer the {@link CachedLayer} to verify
         * @throws IOException if an I/O exception occurs
         */
        private async Task verifyIsLayer2Async(CachedLayer cachedLayer)
        {
            Assert.AreEqual("layerBlob2", await Blobs.writeToStringAsync(await decompressAsync(cachedLayer.getBlob())));
            Assert.AreEqual(layerDigest2, cachedLayer.getDigest());
            Assert.AreEqual(layerDiffId2, cachedLayer.getDiffId());
            Assert.AreEqual(layerSize2, cachedLayer.getSize());
        }
    }
}
