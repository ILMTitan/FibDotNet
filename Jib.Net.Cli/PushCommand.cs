using CommandLine;
using Jib.Net.Core.Api;

namespace Jib.Net.Cli
{
    [Verb("push", HelpText = "Build an image and push to a remote registry.")]
    public class PushCommand : Command
    {
        protected override IContainerizer CreateContainerizer(ImageReference imageReference)
        {
            return Containerizer.To(RegistryImage.Named(imageReference));
        }
    }
}