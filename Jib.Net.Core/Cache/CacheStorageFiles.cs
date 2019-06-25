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
using com.google.cloud.tools.jib.filesystem;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.cache
{
    /** Resolves the files used in the default cache storage engine. */
    public class CacheStorageFiles
    {
        private const string LAYERS_DIRECTORY = "layers";
        private const string IMAGES_DIRECTORY = "images";
        private const string SELECTORS_DIRECTORY = "selectors";
        private const string TEMPORARY_DIRECTORY = "tmp";
        private const string TEMPORARY_LAYER_FILE_NAME = ".tmp.layer";

        /**
         * Returns whether or not {@code file} is a layer contents file.
         *
         * @param file the file to check
         * @return {@code true} if {@code file} is a layer contents file; {@code false} otherwise
         */
        public static bool isLayerFile(SystemPath file)
        {
            return file?.getFileName().toString().length() == DescriptorDigest.HASH_LENGTH;
        }

        private readonly SystemPath cacheDirectory;

        public CacheStorageFiles(SystemPath cacheDirectory)
        {
            this.cacheDirectory = cacheDirectory;
        }

        /**
         * Gets the diff ID portion of the layer filename.
         *
         * @param layerFile the layer file to parse for the diff ID
         * @return the diff ID portion of the layer file filename
         * @throws CacheCorruptedException if no valid diff ID could be parsed
         */
        public DescriptorDigest getDiffId(SystemPath layerFile)
        {
            try
            {

                layerFile = layerFile ?? throw new ArgumentNullException(nameof(layerFile));
                string diffId = layerFile.getFileName().toString();
                return DescriptorDigest.fromHash(diffId);
            }
            catch (Exception ex) when (ex is DigestException || ex is IndexOutOfRangeException)
            {
                throw new CacheCorruptedException(
                    cacheDirectory, "Layer file did not include valid diff ID: " + layerFile, ex);
            }
        }

        /**
         * Gets the cache directory.
         *
         * @return the cache directory
         */
        public SystemPath getCacheDirectory()
        {
            return cacheDirectory;
        }

        /**
         * Resolves the layer contents file.
         *
         * @param layerDigest the layer digest
         * @param layerDiffId the layer diff Id
         * @return the layer contents file
         */
        public SystemPath getLayerFile(DescriptorDigest layerDigest, DescriptorDigest layerDiffId)
        {
            return getLayerDirectory(layerDigest).resolve(getLayerFilename(layerDiffId));
        }

        /**
         * Gets the filename for the layer file. The filename is in the form {@code <layer diff
         * ID>.layer}.
         *
         * @param layerDiffId the layer's diff ID
         * @return the layer filename
         */
        public string getLayerFilename(DescriptorDigest layerDiffId)
        {

            layerDiffId = layerDiffId ?? throw new ArgumentNullException(nameof(layerDiffId));
            return layerDiffId.getHash();
        }

        /**
         * Resolves a selector file.
         *
         * @param selector the selector digest
         * @return the selector file
         */
        public SystemPath getSelectorFile(DescriptorDigest selector)
        {

            selector = selector ?? throw new ArgumentNullException(nameof(selector));
            return cacheDirectory.resolve(SELECTORS_DIRECTORY).resolve(selector.getHash());
        }

        /**
         * Resolves the {@link #LAYERS_DIRECTORY} in the {@link #cacheDirectory}.
         *
         * @return the directory containing all the layer directories
         */
        public SystemPath getLayersDirectory()
        {
            return cacheDirectory.resolve(LAYERS_DIRECTORY);
        }

        /**
         * Gets the directory for the layer with digest {@code layerDigest}.
         *
         * @param layerDigest the digest of the layer
         * @return the directory for that {@code layerDigest}
         */
        public SystemPath getLayerDirectory(DescriptorDigest layerDigest)
        {

            layerDigest = layerDigest ?? throw new ArgumentNullException(nameof(layerDigest));
            return getLayersDirectory().resolve(layerDigest.getHash());
        }

        /**
         * Gets the directory to store the image manifest and configuration.
         *
         * @return the directory for the image manifest and configuration
         */
        public SystemPath getImagesDirectory()
        {
            return cacheDirectory.resolve(IMAGES_DIRECTORY);
        }

        /**
         * Gets the directory corresponding to the given image reference.
         *
         * @param imageReference the image reference
         * @return a path in the form of {@code
         *     (jib-cache)/images/registry[!port]/repository!(tag|digest-type!digest)}
         */
        public SystemPath getImageDirectory(IImageReference imageReference)
        {

            imageReference = imageReference ?? throw new ArgumentNullException(nameof(imageReference));
            // Replace ':' and '@' with '!' to avoid directory-naming restrictions
            string replacedReference = imageReference.toStringWithTag().replace(':', '!').replace('@', '!');

            // Split image reference on '/' to build directory structure
            IEnumerable<string> directories = Splitter.on('/').split(replacedReference);
            SystemPath destination = getImagesDirectory();
            foreach (string dir in directories)
            {
                destination = destination.resolve(dir);
            }
            return destination;
        }

        /**
         * Gets the directory to store temporary files.
         *
         * @return the directory for temporary files
         */
        public SystemPath getTemporaryDirectory()
        {
            return cacheDirectory.resolve(TEMPORARY_DIRECTORY);
        }

        /**
         * Resolves a file to use as a temporary file to write layer contents to.
         *
         * @param layerDirectory the directory in which to resolve the temporary layer file
         * @return the temporary layer file
         */
        public TemporaryFile getTemporaryLayerFile(SystemPath layerDirectory)
        {

            layerDirectory = layerDirectory ?? throw new ArgumentNullException(nameof(layerDirectory));
            return new TemporaryFile(layerDirectory.resolve(TEMPORARY_LAYER_FILE_NAME));
        }
    }
}