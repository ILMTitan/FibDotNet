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
using Fib.Net.Core.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;

namespace Fib.Net.Core.Images.Json
{
    /**
     * JSON template for Docker Manifest Schema V2.1
     *
     * <p>This is only for parsing manifests in the older V2.1 schema. Generated manifests should be in
     * the V2.2 schema using the {@link V22ManifestTemplate}.
     *
     * <p>Example manifest JSON (only the {@code fsLayers} and {@code history} fields are relevant for
     * parsing):
     *
     * <pre>{@code
     * {
     *   ...
     *   "fsLayers": {
     *     {
     *       "blobSum": "sha256:5f70bf18a086007016e948b04aed3b82103a36bea41755b6cddfaf10ace3c6ef"
     *     },
     *     {
     *       "blobSum": "sha256:5f70bf18a086007016e948b04aed3b82103a36bea41755b6cddfaf10ace3c6ef"
     *     }
     *   },
     *   "history": [
     *     {
     *       "v1Compatibility": "<some manifest V1 JSON object>"
     *     }
     *   ]
     *   ...
     * }
     * }</pre>
     *
     * @see <a href="https://docs.docker.com/registry/spec/manifest-v2-1/">Image Manifest Version 2,
     *     Schema 1</a>
     */
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class V21ManifestTemplate : IManifestTemplate
    {
        public static readonly string ManifestMediaType = "application/vnd.docker.distribution.manifest.v1+json";

        public int SchemaVersion { get; } = 1;

        /** The list of layer references. */
        public IList<LayerObjectTemplate> FsLayers { get; } = new List<LayerObjectTemplate>();

        public IList<HistoryObjectTemplate> History { get; } = new List<HistoryObjectTemplate>();

        /**
         * Template for inner JSON object representing a layer as part of the list of layer references.
         */
        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public class LayerObjectTemplate
        {
            public DescriptorDigest BlobSum { get; set; }

            public DescriptorDigest GetDigest()
            {
                return BlobSum;
            }
        }

        /** Template for inner JSON object representing history for a layer. */
        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public class HistoryObjectTemplate
        {
            // The value is basically free-form; they may be structured differently in practice, e.g.,
            // {"architecture": "amd64", "config": {"User": "1001", ...}, "parent": ...}
            // {"id": ..., "container_config": {"Cmd":[""]}}
            public string V1Compatibility { get; }

            [JsonConstructor]
            public HistoryObjectTemplate(string v1Compatibility)
            {
                V1Compatibility = v1Compatibility;
            }
        }

        public IEnumerable<DescriptorDigest> GetLayerDigests()
        {
            foreach (LayerObjectTemplate layerObjectTemplate in FsLayers)
            {
                yield return layerObjectTemplate.BlobSum;
            }
        }

        /**
         * Attempts to parse the container configuration JSON (of format {@code
         * application/vnd.docker.container.image.v1+json}) from the {@code v1Compatibility} value of the
         * first {@code history} entry, which corresponds to the latest layer.
         *
         * @return container configuration if the first history string holds it; {@code null} otherwise
         */
        public Maybe<ContainerConfigurationTemplate> GetContainerConfiguration()
        {
            try
            {
                if (History.Count == 0)
                {
                    return Maybe.Empty<ContainerConfigurationTemplate>();
                }
                string v1Compatibility = History[0].V1Compatibility;
                if (v1Compatibility == null)
                {
                    return Maybe.Empty<ContainerConfigurationTemplate>();
                }

                return Maybe.Of(
                    JsonTemplateMapper.ReadJson<ContainerConfigurationTemplate>(v1Compatibility));
            }
            catch (IOException)
            {
                // not a container configuration; ignore and continue
                return Maybe.Empty<ContainerConfigurationTemplate>();
            }
        }
    }
}
