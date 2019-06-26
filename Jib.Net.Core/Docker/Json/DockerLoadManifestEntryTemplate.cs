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
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.docker.json
{
    /**
     * JSON Template for a loadable Docker Manifest entry. The repoTags property requires a tag; i.e. if
     * a tag is missing, it explicitly should use "latest".
     *
     * <p>Note that this is a template for a single Manifest entry, while the entire Docker Manifest
     * should be {@code List<DockerLoadManifestEntryTemplate>}.
     *
     * <p>Example manifest entry JSON:
     *
     * <pre>{@code
     * {
     *   "Config":"config.json",
     *   "RepoTags":["repository:tag"]
     *   "Layers": [
     *     "eb05f3dbdb543cc610527248690575bacbbcebabe6ecf665b189cf18b541e3ca.tar.gz",
     *     "ba7c544469e514f1a9a4dec59ab640540d50992b288adbb34a1a63c45bf19a24.tar.gz",
     *     "15705ab016593987662839b40f5a22fd1032996c90808d4a1371eb46974017d5.tar.gz"
     *   ]
     * }
     * }</pre>
     *
     * @see <a href="https://github.com/moby/moby/blob/master/image/tarexport/load.go">Docker load
     *     source</a>
     */
     [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class DockerLoadManifestEntryTemplate
    {
        public string Config { get; } = "config.json";
        public List<string> RepoTags { get; set; } = new List<string> { null };
        public IList<string> Layers { get; } = new List<string>();

        public void setRepoTags(string repoTags)
        {
            RepoTags = new List<string> { repoTags };
        }

        public void addLayerFile(string layer)
        {
            Layers.add(layer);
        }
    }
}
