/*
 * Copyright 2018 Google LLC.
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
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.cache
{





    /**
     * Cache for storing data to be shared between Jib executions.
     *
     * <p>This class is immutable and safe to use across threads.
     */

    public sealed class Cache
    {
        /**
         * Initializes the cache using {@code cacheDirectory} for storage.
         *
         * @param cacheDirectory the directory for the cache. Creates the directory if it does not exist.
         * @return a new {@link Cache}
         * @throws IOException if an I/O exception occurs
         */
        public static Cache withDirectory(SystemPath cacheDirectory)
        {
            Files.createDirectories(cacheDirectory);
            return new Cache(new CacheStorageFiles(cacheDirectory));
        }

        private readonly CacheStorageWriter cacheStorageWriter;
        private readonly CacheStorageReader cacheStorageReader;

        private Cache(CacheStorageFiles cacheStorageFiles)
        {
            this.cacheStorageWriter = new CacheStorageWriter(cacheStorageFiles);
            this.cacheStorageReader = new CacheStorageReader(cacheStorageFiles);
        }

        /**
         * Saves a manifest and container configuration for a V2.2 or OCI image.
         *
         * @param imageReference the image reference to save the manifest and container configuration for
         * @param manifestTemplate the V2.2 or OCI manifest
         * @param containerConfigurationTemplate the container configuration
         * @throws IOException if an I/O exception occurs
         */
        public void writeMetadata(
            ImageReference imageReference,
            BuildableManifestTemplate manifestTemplate,
            ContainerConfigurationTemplate containerConfigurationTemplate)
        {
            cacheStorageWriter.writeMetadata(
                imageReference, manifestTemplate, containerConfigurationTemplate);
        }

        /**
         * Saves a V2.1 image manifest.
         *
         * @param imageReference the image reference to save the manifest and container configuration for
         * @param manifestTemplate the V2.1 manifest
         * @throws IOException if an I/O exception occurs
         */
        public void writeMetadata(ImageReference imageReference, V21ManifestTemplate manifestTemplate)
        {
            cacheStorageWriter.writeMetadata(imageReference, manifestTemplate);
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
        public CachedLayer writeCompressedLayer(Blob compressedLayerBlob)
        {
            return cacheStorageWriter.writeCompressed(compressedLayerBlob);
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
        public CachedLayer writeUncompressedLayer(
            Blob uncompressedLayerBlob, ImmutableArray<LayerEntry> layerEntries)
        {
            return cacheStorageWriter.writeUncompressed(
                uncompressedLayerBlob, LayerEntriesSelector.generateSelector(layerEntries));
        }

        /**
         * Retrieves the cached manifest and container configuration for an image reference.
         *
         * @param imageReference the image reference
         * @return the manifest and container configuration for the image reference, if found
         * @throws IOException if an I/O exception occurs
         * @throws CacheCorruptedException if the cache is corrupted
         */
        public Optional<ManifestAndConfig> retrieveMetadata(ImageReference imageReference)
        {
            return cacheStorageReader.retrieveMetadata(imageReference);
        }

        /**
         * Retrieves the {@link CachedLayer} that was built from the {@code layerEntries}.
         *
         * @param layerEntries the layer entries to match against
         * @return a {@link CachedLayer} that was built from {@code layerEntries}, if found
         * @throws IOException if an I/O exception occurs
         * @throws CacheCorruptedException if the cache is corrupted
         */
        public Optional<CachedLayer> retrieve(ImmutableArray<LayerEntry> layerEntries)
        {
            Optional<DescriptorDigest> optionalSelectedLayerDigest =
                cacheStorageReader.select(LayerEntriesSelector.generateSelector(layerEntries));
            if (!optionalSelectedLayerDigest.isPresent())
            {
                return Optional.empty<CachedLayer>();
            }

            return cacheStorageReader.retrieve(optionalSelectedLayerDigest.get());
        }

        /**
         * Retrieves the {@link CachedLayer} for the layer with digest {@code layerDigest}.
         *
         * @param layerDigest the layer digest
         * @return the {@link CachedLayer} referenced by the layer digest, if found
         * @throws CacheCorruptedException if the cache was found to be corrupted
         * @throws IOException if an I/O exception occurs
         */
        public Optional<CachedLayer> retrieve(DescriptorDigest layerDigest)
        {
            return cacheStorageReader.retrieve(layerDigest);
        }
    }
}