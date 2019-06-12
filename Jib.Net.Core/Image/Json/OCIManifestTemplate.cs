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
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
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
    public class OCIManifestTemplate : BuildableManifestTemplate
    {
        /** The OCI manifest media type. */
        public static readonly string MANIFEST_MEDIA_TYPE = "application/vnd.oci.image.manifest.v1+json";

        /** The OCI container configuration media type. */
        private static readonly string CONTAINER_CONFIGURATION_MEDIA_TYPE =
            "application/vnd.oci.image.config.v1+json";

        /** The OCI layer media type. */
        private static readonly string LAYER_MEDIA_TYPE = "application/vnd.oci.image.layer.v1.tar+gzip";

        private readonly int schemaVersion = 2;
        private readonly string mediaType = MANIFEST_MEDIA_TYPE;

        /** The container configuration reference. */
        private ContentDescriptorTemplate config;

        /** The list of layer references. */
        private readonly List<ContentDescriptorTemplate> layers = new List<ContentDescriptorTemplate>();

        public int getSchemaVersion()
        {
            return schemaVersion;
        }

        public string getManifestMediaType()
        {
            return MANIFEST_MEDIA_TYPE;
        }

        public ContentDescriptorTemplate getContainerConfiguration()
        {
            return config;
        }

        public IReadOnlyList<ContentDescriptorTemplate> getLayers()
        {
            return Collections.unmodifiableList(layers);
        }

        public void setContainerConfiguration(long size, DescriptorDigest digest)
        {
            config = new ContentDescriptorTemplate(CONTAINER_CONFIGURATION_MEDIA_TYPE, size, digest);
        }

        public void addLayer(long size, DescriptorDigest digest)
        {
            layers.add(new ContentDescriptorTemplate(LAYER_MEDIA_TYPE, size, digest));
        }
    }
}
