using System;
using System.IO;
using CommandLine;
using Jib.Net.Core.Api;

namespace Jib.Net.Cli
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