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

using System.IO;
using CommandLine;
using Fib.Net.Core.Api;

namespace Fib.Net.Cli
{
    [Verb("tar", HelpText = "Build an image to a compressed tar file.")]
    public class TarCommand : Command
    {
        [Option(
            Required = true,
            HelpText = "The file to write the resulting tar file to.")]
        public string OutputFile { get; set; }

        protected override IContainerizer CreateContainerizer(ImageReference imageReference)
        {
            if (!Path.IsPathRooted(OutputFile))
            {
                OutputFile = Path.Combine(Directory.GetCurrentDirectory(), OutputFile);
            }
            return Containerizer.To(TarImage.Named(imageReference).SaveTo(OutputFile));
        }
    }
}