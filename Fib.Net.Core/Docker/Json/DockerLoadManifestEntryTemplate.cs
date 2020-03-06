// Copyright 2018 Google LLC.
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

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Fib.Net.Core.Docker.Json
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
        public IList<string> RepoTags { get; set; } = new List<string> { null };
        public IList<string> Layers { get; set; } = new List<string>();

        public void SetRepoTags(string repoTags)
        {
            RepoTags.Clear();
            RepoTags.Add(repoTags);
        }

        public void AddLayerFile(string layer)
        {
            Layers.Add(layer);
        }
    }
}
