using CommandLine;
using Fib.Net.Core.Api;

namespace Fib.Net.Cli
{
    [Verb("daemon", HelpText = "Build an image and push to the local docker daemon.")]
    public class DaemonCommand : Command
    {
        protected override IContainerizer CreateContainerizer(ImageReference imageReference)
        {
            return Containerizer.To(DockerDaemonImage.Named(imageReference));
        }
    }
}