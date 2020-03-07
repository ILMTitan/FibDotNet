// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fib.Net.Core.Api;
using Newtonsoft.Json;

namespace Fib.Net.Cli
{
    [JsonObject]
    public class FibCliConfiguration
    {
        [JsonProperty(Required = Required.Always)]
        public string BaseImage { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string TargetImage { get; set; }

        public IReadOnlyList<string> TargetTags { get; set; }
        public ImageFormat ImageFormat { get; set; }
        public IReadOnlyList<LayerConfiguration> ImageLayers { get; set; } = new List<LayerConfiguration>();
        public IReadOnlyList<string> Entrypoint { get; set; }
        public IReadOnlyList<string> Cmd { get; set; }
        public IReadOnlyDictionary<string, string> Environment { get; set; }
        public AbsoluteUnixPath ImageWorkingDirectory { get; set; }
        public string ImageUser { get; set; }

        [JsonConverter(typeof(JsonPortsConverter))]
        public IReadOnlyCollection<Port> Ports { get; set; }

        public IReadOnlyCollection<AbsoluteUnixPath> Volumes { get; set; }
        public IReadOnlyDictionary<string, string> Labels { get; set; }
        public string ApplicationLayersCacheDirectory { get; set; }
        public string BaseLayersCacheDirectory { get; set; }
        public bool ReproducableBuild { get; set; }
        public bool AllowInsecureRegistries { get; set; }
        public bool OfflineMode { get; set; }

        public static async Task<FibCliConfiguration> LoadAsync(string configFile)
        {
            configFile = configFile ?? throw new ArgumentNullException(nameof(configFile));
            string jsonString = await File.ReadAllTextAsync(configFile).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<FibCliConfiguration>(jsonString);
        }

        public ImageReference GetTargetImageReference()
        {
            var targetImageReference = ImageReference.Parse(TargetImage);
            if (TargetTags?.Count > 0)
            {
                targetImageReference = targetImageReference.WithTag(TargetTags[0]);
            }
            return targetImageReference;
        }

        public IContainerizer ConfigureContainerizer(IContainerizer containerizer)
        {
            containerizer = containerizer ?? throw new ArgumentNullException(nameof(containerizer));
            containerizer.WithAdditionalTags(TargetTags ?? Enumerable.Empty<string>())
                .SetAllowInsecureRegistries(AllowInsecureRegistries)
                .SetOfflineMode(OfflineMode)
                .SetApplicationLayersCache(ApplicationLayersCacheDirectory)
                .SetBaseImageLayersCache(BaseLayersCacheDirectory);
            return containerizer;
        }

        public FibContainerBuilder GetContainerBuilder()
        {
            var builder = FibContainerBuilder.From(BaseImage)
                .SetLayers(ImageLayers)
                .SetEntrypoint(Entrypoint)
                .SetProgramArguments(Cmd)
                .SetEnvironment(Environment)
                .SetWorkingDirectory(ImageWorkingDirectory)
                .SetUser(ImageUser)
                .SetExposedPorts(Ports)
                .SetVolumes(Volumes)
                .SetLabels(Labels)
                .SetFormat(ImageFormat);

            if (!ReproducableBuild)
            {
                builder.SetCreationTime(DateTimeOffset.UtcNow);
            }

            return builder;
        }
    }
}