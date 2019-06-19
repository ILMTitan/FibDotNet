/*
 * Copyright 2017 Google LLC.
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
using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.image.json
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
    public class OCIManifestTemplate : BuildableManifestTemplate
    {
        /** The OCI manifest media type. */
        public static readonly string MANIFEST_MEDIA_TYPE = "application/vnd.oci.image.manifest.v1+json";

        /** The OCI container configuration media type. */
        private const string CONTAINER_CONFIGURATION_MEDIA_TYPE =
            "application/vnd.oci.image.config.v1+json";

        /** The OCI layer media type. */
        private const string LAYER_MEDIA_TYPE = "application/vnd.oci.image.layer.v1.tar+gzip";

        public int SchemaVersion { get; } = 2;
        public string MediaType { get; } = MANIFEST_MEDIA_TYPE;

        /** The container configuration reference. */
        public ContentDescriptorTemplate Config { get; set; }

        /** The list of layer references. */
        public List<ContentDescriptorTemplate> Layers { get; } = new List<ContentDescriptorTemplate>();

        public int getSchemaVersion()
        {
            return SchemaVersion;
        }

        public string getManifestMediaType()
        {
            return MANIFEST_MEDIA_TYPE;
        }

        public ContentDescriptorTemplate getContainerConfiguration()
        {
            return Config;
        }

        public IReadOnlyList<ContentDescriptorTemplate> getLayers()
        {
            return Collections.unmodifiableList(Layers);
        }

        public void setContainerConfiguration(long size, DescriptorDigest digest)
        {
            Config = new ContentDescriptorTemplate(CONTAINER_CONFIGURATION_MEDIA_TYPE, size, digest);
        }

        public void addLayer(long size, DescriptorDigest digest)
        {
            Layers.add(new ContentDescriptorTemplate(LAYER_MEDIA_TYPE, size, digest));
        }

        public ManifestFormat getFormat()
        {
            return ManifestFormat.OCI;
        }
    }
}
