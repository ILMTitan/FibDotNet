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
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.filesystem;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.image.json;
using com.google.cloud.tools.jib.json;
using ICSharpCode.SharpZipLib.GZip;
using Jib.Net.Core;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.cache
{





















    /** Writes to the default cache storage engine. */
    public class CacheStorageWriter
    {
        /** Holds information about a layer that was written. */
        public class WrittenLayer
        {
            public readonly DescriptorDigest layerDigest;
            public readonly DescriptorDigest layerDiffId;
            public readonly long layerSize;

            public WrittenLayer(
                DescriptorDigest layerDigest, DescriptorDigest layerDiffId, long layerSize)
            {
                this.layerDigest = layerDigest;
                this.layerDiffId = layerDiffId;
                this.layerSize = layerSize;
            }
        }

        /**
         * Attempts to move {@code source} to {@code destination}. If {@code destination} already exists,
         * this does nothing. Attempts an atomic move first, and falls back to non-atomic if the
         * filesystem does not support atomic moves.
         *
         * @param source the source path
         * @param destination the destination path
         * @throws IOException if an I/O exception occurs
         */
        private static void moveIfDoesNotExist(SystemPath source, SystemPath destination)
        {
            // If the file already exists, we skip renaming and use the existing file. This happens if a
            // new layer happens to have the same content as a previously-cached layer.
            if (Files.exists(destination))
            {
                return;
            }

            try
            {
                Files.move(source, destination);
            }
            catch (IOException)
            {
                if (!Files.exists(destination))
                {
                    // TODO to log that the destination exists
                    throw;
                }
            }
        }

        /**
         * Decompresses the file to obtain the diff ID.
         *
         * @param compressedFile the file containing the compressed contents
         * @return the digest of the decompressed file
         * @throws IOException if an I/O exception occurs
         */
        private static async Task<DescriptorDigest> getDiffIdByDecompressingFileAsync(SystemPath compressedFile)
        {
            using (CountingDigestOutputStream diffIdCaptureOutputStream =
                new CountingDigestOutputStream(Stream.Null))
            {
                using (GZipStream decompressorStream = new GZipStream(Files.newInputStream(compressedFile), CompressionMode.Decompress))
                {
                    await ByteStreams.copyAsync(decompressorStream, diffIdCaptureOutputStream);
                }
                return diffIdCaptureOutputStream.computeDigest().getDigest();
            }
        }

        /**
         * Writes a json template to the destination path by writing to a temporary file then moving the
         * file.
         *
         * @param jsonTemplate the json template
         * @param destination the destination path
         * @throws IOException if an I/O exception occurs
         */
        public static void writeMetadata(object jsonTemplate, SystemPath destination)
        {
            using (TemporaryFile temporaryFile = Files.createTempFile(destination.getParent(), null, null))
            {
                using (Stream outputStream = Files.newOutputStream(temporaryFile.Path))
                {
                    JsonTemplateMapper.writeTo(jsonTemplate, outputStream);
                }
                Files.move(
                    temporaryFile.Path,
                    destination,
                    StandardCopyOption.REPLACE_EXISTING);
            }
        }

        private readonly CacheStorageFiles cacheStorageFiles;

        public CacheStorageWriter(CacheStorageFiles cacheStorageFiles)
        {
            this.cacheStorageFiles = cacheStorageFiles;
        }

        /**
         * Writes a compressed layer {@link Blob}.
         *
         * <p>The {@code compressedLayerBlob} is written to the layer directory under the layers directory
         * corresponding to the layer blob.
         *
         * @param compressedLayerBlob the compressed layer {@link Blob} to write out
         * @return the {@link CachedLayer} representing the written entry
         * @throws IOException if an I/O exception occurs
         */
        public async Task<CachedLayer> writeCompressedAsync(Blob compressedLayerBlob)
        {
            // Creates the layers directory if it doesn't exist.
            Files.createDirectories(cacheStorageFiles.getLayersDirectory());

            // Creates the temporary directory.
            Files.createDirectories(cacheStorageFiles.getTemporaryDirectory());
            using (TemporaryDirectory temporaryDirectory =
                new TemporaryDirectory(cacheStorageFiles.getTemporaryDirectory()))
            {
                SystemPath temporaryLayerDirectory = temporaryDirectory.getDirectory();

                // Writes the layer file to the temporary directory.
                WrittenLayer writtenLayer =
                    await writeCompressedLayerBlobToDirectoryAsync(compressedLayerBlob, temporaryLayerDirectory);

                // Moves the temporary directory to the final location.
                temporaryDirectory.moveIfDoesNotExist(cacheStorageFiles.getLayerDirectory(writtenLayer.layerDigest));

                // Updates cachedLayer with the blob information.
                SystemPath layerFile =
                    cacheStorageFiles.getLayerFile(writtenLayer.layerDigest, writtenLayer.layerDiffId);
                return CachedLayer.builder()
                    .setLayerDigest(writtenLayer.layerDigest)
                    .setLayerDiffId(writtenLayer.layerDiffId)
                    .setLayerSize(writtenLayer.layerSize)
                    .setLayerBlob(Blobs.from(layerFile))
                    .build();
            }
        }

        /**
         * Writes an uncompressed {@link Blob} out to the cache directory in the form:
         *
         * <ul>
         *   <li>The {@code uncompressedLayerBlob} is written to the layer directory under the layers
         *       directory corresponding to the layer blob.
         *   <li>The {@code selector} is written to the selector file under the selectors directory.
         * </ul>
         *
         * @param uncompressedLayerBlob the {@link Blob} containing the uncompressed layer contents to
         *     write out
         * @param selector the optional selector digest to also reference this layer data. A selector
         *     digest may be a secondary identifier for a layer that is distinct from the default layer
         *     digest.
         * @return the {@link CachedLayer} representing the written entry
         * @throws IOException if an I/O exception occurs
         */
        public async Task<CachedLayer> writeUncompressedAsync(Blob uncompressedLayerBlob, DescriptorDigest selector)
        {
            // Creates the layers directory if it doesn't exist.
            Files.createDirectories(cacheStorageFiles.getLayersDirectory());

            // Creates the temporary directory. The temporary directory must be in the same FileStore as the
            // final location for Files.move to work.
            Files.createDirectories(cacheStorageFiles.getTemporaryDirectory());
            using (TemporaryDirectory temporaryDirectory =
                new TemporaryDirectory(cacheStorageFiles.getTemporaryDirectory()))
            {
                SystemPath temporaryLayerDirectory = temporaryDirectory.getDirectory();

                // Writes the layer file to the temporary directory.
                WrittenLayer writtenLayer =
                    await writeUncompressedLayerBlobToDirectoryAsync(uncompressedLayerBlob, temporaryLayerDirectory);

                // Moves the temporary directory to the final location.
                temporaryDirectory.moveIfDoesNotExist(cacheStorageFiles.getLayerDirectory(writtenLayer.layerDigest));

                // Updates cachedLayer with the blob information.
                SystemPath layerFile =
                    cacheStorageFiles.getLayerFile(writtenLayer.layerDigest, writtenLayer.layerDiffId);
                CachedLayer.Builder cachedLayerBuilder =
                    CachedLayer.builder()
                        .setLayerDigest(writtenLayer.layerDigest)
                        .setLayerDiffId(writtenLayer.layerDiffId)
                        .setLayerSize(writtenLayer.layerSize)
                        .setLayerBlob(Blobs.from(layerFile));

                // Write the selector file.
                if (selector != null)
                {
                    writeSelector(selector, writtenLayer.layerDigest);
                }

                return cachedLayerBuilder.build();
            }
        }

        /**
         * Saves the manifest and container configuration for a V2.2 or OCI image.
         *
         * @param imageReference the image reference to store the metadata for
         * @param manifestTemplate the manifest
         * @param containerConfiguration the container configuration
         */
        public void writeMetadata(
            IImageReference imageReference,
            BuildableManifestTemplate manifestTemplate,
            ContainerConfigurationTemplate containerConfiguration)
        {
            Preconditions.checkNotNull(manifestTemplate.getContainerConfiguration());
            Preconditions.checkNotNull(manifestTemplate.getContainerConfiguration().getDigest());

            SystemPath imageDirectory = cacheStorageFiles.getImageDirectory(imageReference);
            Files.createDirectories(imageDirectory);

            using (LockFile ignored1 = LockFile.@lock(imageDirectory.resolve("lock")))
            {
                writeMetadata(manifestTemplate, imageDirectory.resolve("manifest.json"));
                writeMetadata(containerConfiguration, imageDirectory.resolve("config.json"));
            }
        }

        /**
         * Writes a V2.1 manifest for a given image reference.
         *
         * @param imageReference the image reference to store the metadata for
         * @param manifestTemplate the manifest
         */
        public void writeMetadata(IImageReference imageReference, V21ManifestTemplate manifestTemplate)
        {
            SystemPath imageDirectory = cacheStorageFiles.getImageDirectory(imageReference);
            Files.createDirectories(imageDirectory);

            using (LockFile ignored1 = LockFile.@lock(imageDirectory.resolve("lock")))
            {
                writeMetadata(manifestTemplate, imageDirectory.resolve("manifest.json"));
            }
        }

        /**
         * Writes a compressed {@code layerBlob} to the {@code layerDirectory}.
         *
         * @param compressedLayerBlob the compressed layer {@link Blob}
         * @param layerDirectory the directory for the layer
         * @return a {@link WrittenLayer} with the written layer information
         * @throws IOException if an I/O exception occurs
         */
        private async Task<WrittenLayer> writeCompressedLayerBlobToDirectoryAsync(
            Blob compressedLayerBlob, SystemPath layerDirectory)
        {
            // Writes the layer file to the temporary directory.
            using (TemporaryFile temporaryLayerFile = cacheStorageFiles.getTemporaryLayerFile(layerDirectory))
            {

                BlobDescriptor layerBlobDescriptor;
                using (Stream fileOutputStream = Files.newOutputStream(temporaryLayerFile.Path))
                {
                    layerBlobDescriptor = await compressedLayerBlob.writeToAsync(fileOutputStream);
                }

                // Gets the diff ID.
                DescriptorDigest layerDiffId = await getDiffIdByDecompressingFileAsync(temporaryLayerFile.Path);

                // Renames the temporary layer file to the correct filename.
                SystemPath layerFile = layerDirectory.resolve(cacheStorageFiles.getLayerFilename(layerDiffId));
                temporaryLayerFile.moveIfDoesNotExist( layerFile);

            return new WrittenLayer(
                layerBlobDescriptor.getDigest(), layerDiffId, layerBlobDescriptor.getSize());
            }
        }

        /**
         * Writes an uncompressed {@code layerBlob} to the {@code layerDirectory}.
         *
         * @param uncompressedLayerBlob the uncompressed layer {@link Blob}
         * @param layerDirectory the directory for the layer
         * @return a {@link WrittenLayer} with the written layer information
         * @throws IOException if an I/O exception occurs
         */
        private async Task<WrittenLayer> writeUncompressedLayerBlobToDirectoryAsync(
            Blob uncompressedLayerBlob, SystemPath layerDirectory)
        {
            using (TemporaryFile temporaryLayerFile = cacheStorageFiles.getTemporaryLayerFile(layerDirectory)) {
                DescriptorDigest layerDiffId;
                BlobDescriptor blobDescriptor;

                // Writes the layer with GZIP compression. The original bytes are captured as the layer's
                // diff ID and the bytes outputted from the GZIP compression are captured as the layer's
                // content descriptor.
                using (CountingDigestOutputStream compressedDigestOutputStream =
                    new CountingDigestOutputStream(
                        Files.newOutputStream(temporaryLayerFile.Path))) {
                    using (GZipStream compressorStream = new GZipStream(compressedDigestOutputStream, CompressionMode.Compress, true))
                    {
                        BlobDescriptor descriptor = await uncompressedLayerBlob.writeToAsync(compressorStream);
                        layerDiffId = descriptor.getDigest();
                    }
                    // The GZIPOutputStream must be closed in order to write out the remaining compressed data.
                    blobDescriptor = compressedDigestOutputStream.computeDigest();
                }
                    DescriptorDigest layerDigest = blobDescriptor.getDigest();
                    long layerSize = blobDescriptor.getSize();

                    // Renames the temporary layer file to the correct filename.
                    SystemPath layerFile = layerDirectory.resolve(cacheStorageFiles.getLayerFilename(layerDiffId));
                temporaryLayerFile.moveIfDoesNotExist(layerFile);

                return new WrittenLayer(layerDigest, layerDiffId, layerSize);
            }
        }

        /**
         * Writes the {@code selector} to a file in the selectors directory, with contents {@code
         * layerDigest}.
         *
         * @param selector the selector
         * @param layerDigest the layer digest it selects
         * @throws IOException if an I/O exception occurs
         */
        private void writeSelector(DescriptorDigest selector, DescriptorDigest layerDigest)
        {
            SystemPath selectorFile = cacheStorageFiles.getSelectorFile(selector);

            // Creates the selectors directory if it doesn't exist.
            Files.createDirectories(selectorFile.getParent());

            // Writes the selector to a temporary file and then moves the file to the intended location.
            using (TemporaryFile temporarySelectorFile = Files.createTempFile(null, null))
            {
                using (Stream fileOut = FileOperations.newLockingOutputStream(temporarySelectorFile.Path))
                {
                    fileOut.write(layerDigest.getHash().getBytes(StandardCharsets.UTF_8));
                }

                // Attempts an atomic move first, and falls back to non-atomic if the file system does not
                // support atomic moves.
                try
                {
                    Files.move(
                        temporarySelectorFile.Path,
                        selectorFile,
                        StandardCopyOption.REPLACE_EXISTING);
                }
                catch (AtomicMoveNotSupportedException)
                {
                    Files.move(temporarySelectorFile.Path, selectorFile, StandardCopyOption.REPLACE_EXISTING);
                }
            }
        }
    }
}

namespace Jib.Net.Core
{
    [Flags]
    internal enum StandardCopyOption
    {
        ATOMIC_MOVE = 1 << 0,
        REPLACE_EXISTING =1<<1
    }
}