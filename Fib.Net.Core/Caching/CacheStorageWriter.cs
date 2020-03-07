// Copyright 2018 Google LLC. All rights reserved.
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
using Fib.Net.Core.Hash;
using Fib.Net.Core.Images.Json;
using Fib.Net.Core.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Fib.Net.Core.Caching
{
    /** Writes to the default cache storage engine. */
    public class CacheStorageWriter
    {
        /** Holds information about a layer that was written. */
        public class WrittenLayer
        {
            public DescriptorDigest LayerDigest { get; }

            public DescriptorDigest LayerDiffId { get; }

            public long LayerSize { get; }

            public WrittenLayer(
                DescriptorDigest layerDigest, DescriptorDigest layerDiffId, long layerSize)
            {
                LayerDigest = layerDigest;
                LayerDiffId = layerDiffId;
                LayerSize = layerSize;
            }
        }

        /**
         * Decompresses the file to obtain the diff ID.
         *
         * @param compressedFile the file containing the compressed contents
         * @return the digest of the decompressed file
         * @throws IOException if an I/O exception occurs
         */
        private static async Task<DescriptorDigest> GetDiffIdByDecompressingFileAsync(SystemPath compressedFile)
        {
            using (CountingDigestOutputStream diffIdCaptureOutputStream =
                new CountingDigestOutputStream(Stream.Null))
            {
                using (GZipStream decompressorStream = new GZipStream(Files.NewInputStream(compressedFile), CompressionMode.Decompress))
                {
                    await ByteStreams.CopyAsync(decompressorStream, diffIdCaptureOutputStream).ConfigureAwait(false);
                }
                return diffIdCaptureOutputStream.ComputeDigest().GetDigest();
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
        public static async Task WriteMetadataAsync(object jsonTemplate, SystemPath destination)
        {
            destination = destination ?? throw new ArgumentNullException(nameof(destination));
            using (TemporaryFile temporaryFile = Files.CreateTempFile(destination.GetParent()))
            {
                using (Stream outputStream = Files.NewOutputStream(temporaryFile.Path))
                {
                    await JsonTemplateMapper.WriteToAsync(jsonTemplate, outputStream).ConfigureAwait(false);
                }
                Files.Move(
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
        public async Task<CachedLayer> WriteCompressedAsync(IBlob compressedLayerBlob)
        {
            compressedLayerBlob = compressedLayerBlob ?? throw new ArgumentNullException(nameof(compressedLayerBlob));
            // Creates the layers directory if it doesn't exist.
            Files.CreateDirectories(cacheStorageFiles.GetLayersDirectory());

            // Creates the temporary directory.
            Files.CreateDirectories(cacheStorageFiles.GetTemporaryDirectory());
            using (TemporaryDirectory temporaryDirectory =
                new TemporaryDirectory(cacheStorageFiles.GetTemporaryDirectory()))
            {
                SystemPath temporaryLayerDirectory = temporaryDirectory.GetDirectory();

                // Writes the layer file to the temporary directory.
                WrittenLayer writtenLayer =
                    await WriteCompressedLayerBlobToDirectoryAsync(compressedLayerBlob, temporaryLayerDirectory).ConfigureAwait(false);

                // Moves the temporary directory to the final location.
                temporaryDirectory.MoveIfDoesNotExist(cacheStorageFiles.GetLayerDirectory(writtenLayer.LayerDigest));

                // Updates cachedLayer with the blob information.
                SystemPath layerFile =
                    cacheStorageFiles.GetLayerFile(writtenLayer.LayerDigest, writtenLayer.LayerDiffId);
                return CachedLayer.CreateBuilder()
                    .SetLayerDigest(writtenLayer.LayerDigest)
                    .SetLayerDiffId(writtenLayer.LayerDiffId)
                    .SetLayerSize(writtenLayer.LayerSize)
                    .SetLayerBlob(Blobs.From(layerFile))
                    .Build();
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
        public async Task<CachedLayer> WriteUncompressedAsync(IBlob uncompressedLayerBlob, DescriptorDigest selector)
        {
            uncompressedLayerBlob = uncompressedLayerBlob ?? throw new ArgumentNullException(nameof(uncompressedLayerBlob));
            // Creates the layers directory if it doesn't exist.
            Files.CreateDirectories(cacheStorageFiles.GetLayersDirectory());

            // Creates the temporary directory. The temporary directory must be in the same FileStore as the
            // final location for Files.move to work.
            Files.CreateDirectories(cacheStorageFiles.GetTemporaryDirectory());
            using (TemporaryDirectory temporaryDirectory =
                new TemporaryDirectory(cacheStorageFiles.GetTemporaryDirectory()))
            {
                SystemPath temporaryLayerDirectory = temporaryDirectory.GetDirectory();

                // Writes the layer file to the temporary directory.
                WrittenLayer writtenLayer =
                    await WriteUncompressedLayerBlobToDirectoryAsync(uncompressedLayerBlob, temporaryLayerDirectory).ConfigureAwait(false);

                // Moves the temporary directory to the final location.
                temporaryDirectory.MoveIfDoesNotExist(cacheStorageFiles.GetLayerDirectory(writtenLayer.LayerDigest));

                // Updates cachedLayer with the blob information.
                SystemPath layerFile =
                    cacheStorageFiles.GetLayerFile(writtenLayer.LayerDigest, writtenLayer.LayerDiffId);
                CachedLayer.Builder cachedLayerBuilder =
                    CachedLayer.CreateBuilder()
                        .SetLayerDigest(writtenLayer.LayerDigest)
                        .SetLayerDiffId(writtenLayer.LayerDiffId)
                        .SetLayerSize(writtenLayer.LayerSize)
                        .SetLayerBlob(Blobs.From(layerFile));

                // Write the selector file.
                if (selector != null)
                {
                    WriteSelector(selector, writtenLayer.LayerDigest);
                }

                return cachedLayerBuilder.Build();
            }
        }

        /**
         * Saves the manifest and container configuration for a V2.2 or OCI image.
         *
         * @param imageReference the image reference to store the metadata for
         * @param manifestTemplate the manifest
         * @param containerConfiguration the container configuration
         */
        public async Task WriteMetadataAsync(
            IImageReference imageReference,
            IBuildableManifestTemplate manifestTemplate,
            ContainerConfigurationTemplate containerConfiguration)
        {
            manifestTemplate = manifestTemplate ?? throw new ArgumentNullException(nameof(manifestTemplate));
            Preconditions.CheckNotNull(manifestTemplate.GetContainerConfiguration());
            Preconditions.CheckNotNull(manifestTemplate.GetContainerConfiguration().Digest);

            SystemPath imageDirectory = cacheStorageFiles.GetImageDirectory(imageReference);
            Files.CreateDirectories(imageDirectory);

            using (LockFile ignored1 = LockFile.Create(imageDirectory.Resolve("lock")))
            {
                await WriteMetadataAsync(manifestTemplate, imageDirectory.Resolve("manifest.json")).ConfigureAwait(false);
                await WriteMetadataAsync(containerConfiguration, imageDirectory.Resolve("config.json")).ConfigureAwait(false);
            }
        }

        /**
         * Writes a V2.1 manifest for a given image reference.
         *
         * @param imageReference the image reference to store the metadata for
         * @param manifestTemplate the manifest
         */
        public async Task WriteMetadataAsync(IImageReference imageReference, V21ManifestTemplate manifestTemplate)
        {
            SystemPath imageDirectory = cacheStorageFiles.GetImageDirectory(imageReference);
            Files.CreateDirectories(imageDirectory);

            using (LockFile ignored1 = LockFile.Create(imageDirectory.Resolve("lock")))
            {
                await WriteMetadataAsync(manifestTemplate, imageDirectory.Resolve("manifest.json")).ConfigureAwait(false);
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
        private async Task<WrittenLayer> WriteCompressedLayerBlobToDirectoryAsync(
            IBlob compressedLayerBlob, SystemPath layerDirectory)
        {
            // Writes the layer file to the temporary directory.
            using (TemporaryFile temporaryLayerFile = CacheStorageFiles.GetTemporaryLayerFile(layerDirectory))
            {
                BlobDescriptor layerBlobDescriptor;
                using (Stream fileOutputStream = Files.NewOutputStream(temporaryLayerFile.Path))
                {
                    layerBlobDescriptor = await compressedLayerBlob.WriteToAsync(fileOutputStream).ConfigureAwait(false);
                }

                // Gets the diff ID.
                DescriptorDigest layerDiffId = await GetDiffIdByDecompressingFileAsync(temporaryLayerFile.Path).ConfigureAwait(false);

                // Renames the temporary layer file to the correct filename.
                SystemPath layerFile = layerDirectory.Resolve(cacheStorageFiles.GetLayerFilename(layerDiffId));
                temporaryLayerFile.MoveIfDoesNotExist(layerFile);

                return new WrittenLayer(
                    layerBlobDescriptor.GetDigest(), layerDiffId, layerBlobDescriptor.GetSize());
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
        private async Task<WrittenLayer> WriteUncompressedLayerBlobToDirectoryAsync(
            IBlob uncompressedLayerBlob, SystemPath layerDirectory)
        {
            using (TemporaryFile temporaryLayerFile = CacheStorageFiles.GetTemporaryLayerFile(layerDirectory))
            {
                DescriptorDigest layerDiffId;
                BlobDescriptor blobDescriptor;

                // Writes the layer with GZIP compression. The original bytes are captured as the layer's
                // diff ID and the bytes outputted from the GZIP compression are captured as the layer's
                // content descriptor.
                using (CountingDigestOutputStream compressedDigestOutputStream =
                    new CountingDigestOutputStream(
                        Files.NewOutputStream(temporaryLayerFile.Path)))
                {
                    using (GZipStream compressorStream = new GZipStream(compressedDigestOutputStream, CompressionMode.Compress, true))
                    {
                        BlobDescriptor descriptor = await uncompressedLayerBlob.WriteToAsync(compressorStream).ConfigureAwait(false);
                        layerDiffId = descriptor.GetDigest();
                    }
                    // The GZIPOutputStream must be closed in order to write out the remaining compressed data.
                    blobDescriptor = compressedDigestOutputStream.ComputeDigest();
                }
                DescriptorDigest layerDigest = blobDescriptor.GetDigest();
                long layerSize = blobDescriptor.GetSize();

                // Renames the temporary layer file to the correct filename.
                SystemPath layerFile = layerDirectory.Resolve(cacheStorageFiles.GetLayerFilename(layerDiffId));
                temporaryLayerFile.MoveIfDoesNotExist(layerFile);

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
        private void WriteSelector(DescriptorDigest selector, DescriptorDigest layerDigest)
        {
            SystemPath selectorFile = cacheStorageFiles.GetSelectorFile(selector);

            // Creates the selectors directory if it doesn't exist.
            Files.CreateDirectories(selectorFile.GetParent());

            // Writes the selector to a temporary file and then moves the file to the intended location.
            using (TemporaryFile temporarySelectorFile = Files.CreateTempFile())
            {
                using (Stream fileOut = FileOperations.NewLockingOutputStream(temporarySelectorFile.Path))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(layerDigest.GetHash());
                    fileOut.Write(bytes, 0, bytes.Length);
                }

                // Attempts an atomic move first, and falls back to non-atomic if the file system does not
                // support atomic moves.
                Files.Move(
                    temporarySelectorFile.Path,
                    selectorFile,
                    StandardCopyOption.REPLACE_EXISTING);
            }
        }
    }
}

namespace Fib.Net.Core
{
    [Flags]
    internal enum StandardCopyOption
    {
        None = 0,
        ATOMIC_MOVE = 1 << 0,
        REPLACE_EXISTING = 1 << 1
    }
}