// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using Fib.Net.Core.Api;
using Fib.Net.Core.Blob;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Images.Json;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Fib.Net.Core.Caching
{
    /**
     * Cache for storing data to be shared between Fib executions.
     *
     * <p>This class is immutable and safe to use across threads.
     */

    public sealed class LayersCache
    {
        /**
         * Initializes the cache using {@code cacheDirectory} for storage.
         *
         * @param cacheDirectory the directory for the cache. Creates the directory if it does not exist.
         * @return a new {@link Cache}
         * @throws IOException if an I/O exception occurs
         */
        public static LayersCache WithDirectory(SystemPath cacheDirectory)
        {
            Files.CreateDirectories(cacheDirectory);
            return new LayersCache(new CacheStorageFiles(cacheDirectory));
        }

        private readonly CacheStorageWriter cacheStorageWriter;
        private readonly CacheStorageReader cacheStorageReader;

        private LayersCache(CacheStorageFiles cacheStorageFiles)
        {
            cacheStorageWriter = new CacheStorageWriter(cacheStorageFiles);
            cacheStorageReader = new CacheStorageReader(cacheStorageFiles);
        }

        /**
         * Saves a manifest and container configuration for a V2.2 or OCI image.
         *
         * @param imageReference the image reference to save the manifest and container configuration for
         * @param manifestTemplate the V2.2 or OCI manifest
         * @param containerConfigurationTemplate the container configuration
         * @throws IOException if an I/O exception occurs
         */
        public async Task WriteMetadataAsync(
            IImageReference imageReference,
            IBuildableManifestTemplate manifestTemplate,
            ContainerConfigurationTemplate containerConfigurationTemplate)
        {
            await cacheStorageWriter.WriteMetadataAsync(
                imageReference, manifestTemplate, containerConfigurationTemplate).ConfigureAwait(false);
        }

        /**
         * Saves a V2.1 image manifest.
         *
         * @param imageReference the image reference to save the manifest and container configuration for
         * @param manifestTemplate the V2.1 manifest
         * @throws IOException if an I/O exception occurs
         */
        public async Task WriteMetadataAsync(IImageReference imageReference, V21ManifestTemplate manifestTemplate)
        {
            await cacheStorageWriter.WriteMetadataAsync(imageReference, manifestTemplate).ConfigureAwait(false);
        }

        /**
         * Saves a cache entry with a compressed layer {@link Blob}. Use {@link
         * #writeUncompressedLayer(Blob, ImmutableList)} to save a cache entry with an uncompressed layer
         * {@link Blob} and include a selector.
         *
         * @param compressedLayerBlob the compressed layer {@link Blob}
         * @return the {@link CachedLayer} for the written layer
         * @throws IOException if an I/O exception occurs
         */
        public async Task<CachedLayer> WriteCompressedLayerAsync(IBlob compressedLayerBlob)
        {
            return await cacheStorageWriter.WriteCompressedAsync(compressedLayerBlob).ConfigureAwait(false);
        }

        /**
         * Saves a cache entry with an uncompressed layer {@link Blob} and an additional selector digest.
         * Use {@link #writeCompressedLayer(Blob)} to save a compressed layer {@link Blob}.
         *
         * @param uncompressedLayerBlob the layer {@link Blob}
         * @param layerEntries the layer entries that make up the layer
         * @return the {@link CachedLayer} for the written layer
         * @throws IOException if an I/O exception occurs
         */
        public async Task<CachedLayer> WriteUncompressedLayerAsync(
            IBlob uncompressedLayerBlob, ImmutableArray<LayerEntry> layerEntries)
        {
            DescriptorDigest selector = await LayerEntriesSelector.GenerateSelectorAsync(layerEntries).ConfigureAwait(false);
            return await cacheStorageWriter.WriteUncompressedAsync(
                uncompressedLayerBlob, selector).ConfigureAwait(false);
        }

        /**
         * Retrieves the cached manifest and container configuration for an image reference.
         *
         * @param imageReference the image reference
         * @return the manifest and container configuration for the image reference, if found
         * @throws IOException if an I/O exception occurs
         * @throws CacheCorruptedException if the cache is corrupted
         */
        public Maybe<ManifestAndConfig> RetrieveMetadata(IImageReference imageReference)
        {
            return cacheStorageReader.RetrieveMetadata(imageReference);
        }

        /**
         * Retrieves the {@link CachedLayer} that was built from the {@code layerEntries}.
         *
         * @param layerEntries the layer entries to match against
         * @return a {@link CachedLayer} that was built from {@code layerEntries}, if found
         * @throws IOException if an I/O exception occurs
         * @throws CacheCorruptedException if the cache is corrupted
         */
        public async Task<Maybe<CachedLayer>> RetrieveAsync(ImmutableArray<LayerEntry> layerEntries)
        {
            DescriptorDigest selector = await LayerEntriesSelector.GenerateSelectorAsync(layerEntries).ConfigureAwait(false);
            Maybe<DescriptorDigest> optionalSelectedLayerDigest = cacheStorageReader.Select(selector);
            if (!optionalSelectedLayerDigest.IsPresent())
            {
                return Maybe.Empty<CachedLayer>();
            }

            return cacheStorageReader.Retrieve(optionalSelectedLayerDigest.Get());
        }

        /**
         * Retrieves the {@link CachedLayer} for the layer with digest {@code layerDigest}.
         *
         * @param layerDigest the layer digest
         * @return the {@link CachedLayer} referenced by the layer digest, if found
         * @throws CacheCorruptedException if the cache was found to be corrupted
         * @throws IOException if an I/O exception occurs
         */
        public Maybe<CachedLayer> Retrieve(DescriptorDigest layerDigest)
        {
            return cacheStorageReader.Retrieve(layerDigest);
        }
    }
}
