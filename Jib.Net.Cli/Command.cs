using CommandLine;
using Jib.Net.Core.Api;
using Jib.Net.Core.Events;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Jib.Net.Cli
{
    public abstract class Command
    {
        [Option(
            Default = "jib.net.cli",
            HelpText = "The name of the tool executing this program.")]
        public string ToolName { get; set; }

        [Option(
            Required = true,
            HelpText = "The json file of the configuration.")]
        public string ConfigFile { get; set; }

        public async Task ExecuteAsync(TextWriter output, TextWriter error)
        {
            output = output ?? throw new ArgumentNullException(nameof(output));
            error = error ?? throw new ArgumentNullException(nameof(error));

            var configuration = await JibCliConfiguration.LoadAsync(ConfigFile).ConfigureAwait(false);
            var containerBuilder = configuration.GetContainerBuilder();
            var containerizer = GetContainerizer(configuration.GetTargetImageReference(), output, error);
            configuration.ConfigureContainerizer(containerizer);
            var result = await containerBuilder.ContainerizeAsync(containerizer).ConfigureAwait(false);
            var imageDigest = result.GetDigest()?.ToString();
            var imageId = result.GetImageId()?.ToString();
            await output.WriteLineAsync($"ImageDigest:{imageDigest}").ConfigureAwait(false);
            await output.WriteLineAsync($"ImageId:{imageId}").ConfigureAwait(false);
        }

        private IContainerizer GetContainerizer(ImageReference imageReference, TextWriter output, TextWriter error)
        {
            var containerizer = CreateContainerizer(imageReference);
            containerizer.SetToolName(ToolName);
            containerizer.AddEventHandler((IJibEvent e) =>
            {
                if (e is LogEvent logEvent && logEvent.GetLevel() == LogEvent.Level.Error)
                {
                    error.WriteLine(e.ToString());
                }
                else
                {
                    output.WriteLine(e.ToString());
                }
            });
            return containerizer;
        }

        protected abstract IContainerizer CreateContainerizer(ImageReference imageReference);
    }
}