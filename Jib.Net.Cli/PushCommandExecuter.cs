using Jib.Net.Core.Api;

namespace Jib.Net.Cli
{
    internal class PushCommandExecuter : JibCommandExecuter
    {
        public PushCommandExecuter(string toolName, JibCliConfiguration configuration)
            : base(toolName, configuration)
        {
        }

        protected override IContainerizer CreateContainerizer()
        {
            return Containerizer.To(RegistryImage.Named(GetTargetImageReference()));
        }
    }
}