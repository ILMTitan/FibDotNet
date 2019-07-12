using Jib.Net.Core.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Jib.Net.Cli
{
    public abstract class JibCommandExecuter
    {
        private readonly string _toolName;
        private readonly JibCliConfiguration _configuration;
        private static readonly object _consoleLock = new object();

        protected JibCommandExecuter(string toolName, JibCliConfiguration configuration)
        {
            _toolName = toolName;
            _configuration = configuration;
        }

        public async Task ExecuteAsync()
        {
            var containerBuilder = GetContainerBuilder();
            var containerizer = GetContainerizer();
            var result = await containerBuilder.ContainerizeAsync(containerizer).ConfigureAwait(false);
            var imageDigest = result.GetDigest().ToString();
            var imageId = result.GetImageId().ToString();
        }

        protected ImageReference GetTargetImageReference()
        {
            var targetImageReference = ImageReference.Parse(_configuration.TargetImage);
            if (_configuration.TargetTags?.Count> 0)
            {
                targetImageReference = targetImageReference.WithTag(_configuration.TargetTags[0]);
            }
            return targetImageReference;
        }

        protected abstract IContainerizer CreateContainerizer();

        protected IContainerizer GetContainerizer()
        {
            IContainerizer containerizer = CreateContainerizer()
                .WithAdditionalTags(_configuration.TargetTags ?? Enumerable.Empty<string>())
                .SetToolName(_toolName)
                .SetAllowInsecureRegistries(_configuration.AllowInsecureRegistries)
                .SetOfflineMode(_configuration.OfflineMode)
                .SetApplicationLayersCache(_configuration.ApplicationLayersCacheDirectory)
                .SetBaseImageLayersCache(_configuration.BaseLayersCacheDirectory);
            containerizer.AddEventHandler((IJibEvent e) =>
            {
                lock (_consoleLock)
                {
                    Console.WriteLine(e.ToString());
                    Console.WriteLine();
                }
            });
            return containerizer;
        }

        protected JibContainerBuilder GetContainerBuilder()
        {
            var builder = JibContainerBuilder.From(_configuration.BaseImage)
                .SetLayers(_configuration.ImageLayers)
                .SetEntrypoint(_configuration.Entrypoint)
                .SetProgramArguments(_configuration.Cmd)
                .SetEnvironment(_configuration.Environment)
                .SetWorkingDirectory(_configuration.ImageWorkingDirectory)
                .SetUser(_configuration.ImageUser)
                .SetExposedPorts(_configuration.Ports)
                .SetVolumes(_configuration.Volumes)
                .SetLabels(_configuration.Labels)
                .SetFormat(_configuration.ImageFormat);

            if (!_configuration.ReproducableBuild)
            {
                builder.SetCreationTime(DateTimeOffset.UtcNow);
            }

            return builder;
        }
    }
}