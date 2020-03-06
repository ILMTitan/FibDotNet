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
using Fib.Net.Core.Images.Json;
using Fib.Net.Core.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Fib.Net.Core.Caching
{
    /** Reads from the default cache storage engine. */
    public class CacheStorageReader
    {
        private readonly CacheStorageFiles cacheStorageFiles;

        public CacheStorageReader(CacheStorageFiles cacheStorageFiles)
        {
            this.cacheStorageFiles = cacheStorageFiles;
        }

        /**
         * Lists all the layer digests stored.
         *
         * @return the list of layer digests
         * @throws CacheCorruptedException if the cache was found to be corrupted
         * @throws IOException if an I/O exception occurs
         */
        public ISet<DescriptorDigest> FetchDigests()
        {
            IEnumerable<SystemPath> layerDirectories = Files.List(cacheStorageFiles.GetLayersDirectory());

            IList<SystemPath> layerDirectoriesList = layerDirectories.ToList();
            ISet<DescriptorDigest> layerDigests = new HashSet<DescriptorDigest>();
            foreach (SystemPath layerDirectory in layerDirectoriesList)
            {
                try
                {
                    layerDigests.Add(DescriptorDigest.FromHash(layerDirectory.GetFileName().ToString()));
                }
                catch (DigestException ex)
                {
                    throw new CacheCorruptedException(
                        cacheStorageFiles.GetCacheDirectory(),
                        Resources.CacheStorageReaderNonDigestFileExceptionMessage,
                        ex);
                }
            }
            return layerDigests;
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
            SystemPath imageDirectory = cacheStorageFiles.GetImageDirectory(imageReference);
            SystemPath manifestPath = imageDirectory.Resolve("manifest.json");
            if (!Files.Exists(manifestPath))
            {
                return Maybe.Empty<ManifestAndConfig>();
            }

            // TODO: Consolidate with ManifestPuller
            JToken token;
            using (JsonTextReader reader = new JsonTextReader(File.OpenText(manifestPath)))
            {
                token = JToken.ReadFrom(reader);
            }
            if (!(token is JObject node))
            {
                throw new CacheCorruptedException(
                    cacheStorageFiles.GetCacheDirectory(),
                    Resources.CacheStorageReaderNotJsonExecpetionMessage);
            }
            if (!node.ContainsKey("schemaVersion"))
            {
                throw new CacheCorruptedException(
                    cacheStorageFiles.GetCacheDirectory(),
                    Resources.CacheStorageReaderSchemaVersionMissingExecpetionMessage);
            }

            int schemaVersion = node["schemaVersion"].Value<int>();
            if (schemaVersion == -1)
            {
                throw new CacheCorruptedException(
                    cacheStorageFiles.GetCacheDirectory(),
                    Resources.CacheStorageReaderInvalidSchemaVersionExecpetionMessageFormat);
            }

            if (schemaVersion == 1)
            {
                return Maybe.Of(
                    new ManifestAndConfig(
                        JsonTemplateMapper.ReadJsonFromFile<V21ManifestTemplate>(manifestPath),
                        null));
            }
            if (schemaVersion == 2)
            {
                // 'schemaVersion' of 2 can be either Docker V2.2 or OCI.
                string mediaType = node["mediaType"].Value<string>();

                IManifestTemplate manifestTemplate;
                if (V22ManifestTemplate.ManifestMediaType == mediaType)
                {
                    manifestTemplate =
                        JsonTemplateMapper.ReadJsonFromFile<V22ManifestTemplate>(manifestPath);
                }
                else if (OCIManifestTemplate.ManifestMediaType == mediaType)
                {
                    manifestTemplate =
                        JsonTemplateMapper.ReadJsonFromFile<OCIManifestTemplate>(manifestPath);
                }
                else
                {
                    throw new CacheCorruptedException(
                        cacheStorageFiles.GetCacheDirectory(),
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.CacheStorageReaderUnknownMediaTypeExecpetionMessageFormat, mediaType));
                }

                SystemPath configPath = imageDirectory.Resolve("config.json");
                if (!Files.Exists(configPath))
                {
                    throw new CacheCorruptedException(
                        cacheStorageFiles.GetCacheDirectory(),
                       Resources.CacheStorageReaderContainerConfigurationMissingExecpetionMessage);
                }
                ContainerConfigurationTemplate config =
                    JsonTemplateMapper.ReadJsonFromFile<ContainerConfigurationTemplate>(configPath);

                return Maybe.Of(new ManifestAndConfig(manifestTemplate, config));
            }
            throw new CacheCorruptedException(
                cacheStorageFiles.GetCacheDirectory(),
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.CacheStorageReaderInvalidSchemaVersionExecpetionMessageFormat, schemaVersion));
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
            layerDigest = layerDigest ?? throw new ArgumentNullException(nameof(layerDigest));
            SystemPath layerDirectory = cacheStorageFiles.GetLayerDirectory(layerDigest);
            if (!Files.Exists(layerDirectory))
            {
                return Maybe.Empty<CachedLayer>();
            }

            CachedLayer.Builder cachedLayerBuilder = CachedLayer.CreateBuilder().SetLayerDigest(layerDigest);

            foreach (SystemPath fileInLayerDirectory in Files.List(layerDirectory))
            {
                if (CacheStorageFiles.IsLayerFile(fileInLayerDirectory))
                {
                    if (cachedLayerBuilder.HasLayerBlob())
                    {
                        throw new CacheCorruptedException(
                            cacheStorageFiles.GetCacheDirectory(),
                            "Multiple layer files found for layer with digest "
                                + layerDigest.GetHash()
                                + " in directory: "
                                + layerDirectory);
                    }
                    cachedLayerBuilder
                        .SetLayerBlob(Blobs.From(fileInLayerDirectory))
                        .SetLayerDiffId(cacheStorageFiles.GetDiffId(fileInLayerDirectory))
                        .SetLayerSize(Files.Size(fileInLayerDirectory));
                }
            }
            return Maybe.Of(cachedLayerBuilder.Build());
        }

        /**
         * Retrieves the layer digest selected by the {@code selector}.
         *
         * @param selector the selector
         * @return the layer digest {@code selector} selects, if found
         * @throws CacheCorruptedException if the selector file contents was not a valid layer digest
         * @throws IOException if an I/O exception occurs
         */
        public Maybe<DescriptorDigest> Select(DescriptorDigest selector)
        {
            selector = selector ?? throw new ArgumentNullException(nameof(selector));
            SystemPath selectorFile = cacheStorageFiles.GetSelectorFile(selector);
            if (!Files.Exists(selectorFile))
            {
                return Maybe.Empty<DescriptorDigest>();
            }
            string selectorFileContents =
                    File.ReadAllText(selectorFile.ToFile().FullName, Encoding.UTF8);
            try
            {
                return Maybe.Of(DescriptorDigest.FromHash(selectorFileContents));
            }
            catch (DigestException)
            {
                throw new CacheCorruptedException(
                    cacheStorageFiles.GetCacheDirectory(),
                    "Expected valid layer digest as contents of selector file `"
                        + selectorFile
                        + "` for selector `"
                        + selector.GetHash()
                        + "`, but got: "
                        + selectorFileContents);
            }
        }
    }
}
