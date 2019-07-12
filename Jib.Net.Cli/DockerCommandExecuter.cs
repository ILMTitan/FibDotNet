using Jib.Net.Core.Api;

namespace Jib.Net.Cli
{
    internal class DockerCommandExecuter : JibCommandExecuter
    {
        public DockerCommandExecuter(string toolName, JibCliConfiguration configuration)
            : base(toolName, configuration)
        {
        }

        protected override IContainerizer CreateContainerizer()
        {
            return Containerizer.To(DockerDaemonImage.Named(GetTargetImageReference()));
        }
    }
}