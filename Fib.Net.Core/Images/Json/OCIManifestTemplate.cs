// Copyright 2017 Google LLC.
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
using Fib.Net.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Fib.Net.Core.Images.Json
{
    /**
     * JSON Template for OCI Manifest Schema
     *
     * <p>Example manifest JSON:
     *
     * <pre>{@code
     * {
     *   "schemaVersion": 2,
     *   "mediaType": "application/vnd.oci.image.manifest.v1+json",
     *   "config": {
     *     "mediaType": "application/vnd.oci.image.config.v1+json",
     *     "size": 631,
     *     "digest": "sha256:26b84ca5b9050d32e68f66ad0f3e2bbcd247198a6e6e09a7effddf126eb8d873"
     *   },
     *   "layers": [
     *     {
     *       "mediaType": "application/vnd.oci.image.layer.v1.tar+gzip",
     *       "size": 1991435,
     *       "digest": "sha256:b56ae66c29370df48e7377c8f9baa744a3958058a766793f821dadcb144a4647"
     *     },
     *     {
     *       "mediaType": "application/vnd.oci.image.layer.v1.tar+gzip",
     *       "size": 32,
     *       "digest": "sha256:a3ed95caeb02ffe68cdd9fd84406680ae93d633cb16422d00e8a7c22955b46d4"
     *     }
     *   ]
     * }
     * }</pre>
     *
     * @see <a href="https://github.com/opencontainers/image-spec/blob/master/manifest.md">OCI Image
     *     Manifest Specification</a>
     */
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class OCIManifestTemplate : IBuildableManifestTemplate
    {
        /** The OCI manifest media type. */
        public static readonly string ManifestMediaType = "application/vnd.oci.image.manifest.v1+json";
        private readonly List<ContentDescriptorTemplate> layers = new List<ContentDescriptorTemplate>();

        /** The OCI container configuration media type. */
        private const string CONTAINER_CONFIGURATION_MEDIA_TYPE =
            "application/vnd.oci.image.config.v1+json";

        /** The OCI layer media type. */
        private const string LAYER_MEDIA_TYPE = "application/vnd.oci.image.layer.v1.tar+gzip";

        public int SchemaVersion { get; } = 2;
        public string MediaType { get; } = ManifestMediaType;

        /** The container configuration reference. */
        public ContentDescriptorTemplate Config { get; set; }

        /** The list of layer references. */
        public IReadOnlyList<ContentDescriptorTemplate> Layers => layers;

        public string GetManifestMediaType()
        {
            return ManifestMediaType;
        }

        public ContentDescriptorTemplate GetContainerConfiguration()
        {
            return Config;
        }

        public void SetContainerConfiguration(long size, DescriptorDigest digest)
        {
            Config = new ContentDescriptorTemplate(CONTAINER_CONFIGURATION_MEDIA_TYPE, size, digest);
        }

        public void AddLayer(long size, DescriptorDigest digest)
        {
            layers.Add(new ContentDescriptorTemplate(LAYER_MEDIA_TYPE, size, digest));
        }

        public ManifestFormat GetFormat()
        {
            return ManifestFormat.OCI;
        }
    }
}
