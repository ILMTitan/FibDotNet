using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Jib.Net.Core.Api;
using Newtonsoft.Json;

namespace Jib.Net.Cli
{
    [JsonObject]
    public class JibCliConfiguration
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
        public IReadOnlyCollection<Port> Ports { get; set; }
        public IReadOnlyCollection<AbsoluteUnixPath> Volumes { get; set; }
        public IReadOnlyDictionary<string, string> Labels { get; set; }
        public string ApplicationLayersCacheDirectory { get; set; }
        public string BaseLayersCacheDirectory { get; set; }
        public bool ReproducableBuild { get; set; }
        public bool AllowInsecureRegistries { get; set; }
        public bool OfflineMode { get; set; }

        public static async Task<JibCliConfiguration> LoadAsync(FileInfo configFile)
        {
            configFile = configFile ?? throw new ArgumentNullException(nameof(configFile));
            string jsonString = await configFile.OpenText().ReadToEndAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<JibCliConfiguration>(jsonString);
        }
    }
}