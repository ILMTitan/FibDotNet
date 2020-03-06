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

using CommandLine;
using Fib.Net.Core.Api;
using Fib.Net.Core.Events;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Cli
{
    public abstract class Command
    {
        [Option(
            Default = "fib.net.cli",
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

            var configuration = await FibCliConfiguration.LoadAsync(ConfigFile).ConfigureAwait(false);
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
            containerizer.AddEventHandler((IFibEvent e) =>
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