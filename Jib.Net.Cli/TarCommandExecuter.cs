using System.IO;
using Jib.Net.Core.Api;

namespace Jib.Net.Cli
{
    public class TarCommandExecuter : JibCommandExecuter
    {
        private readonly FileInfo _outputFile;

        public TarCommandExecuter(string toolName, FileInfo outputFile, JibCliConfiguration configuration)
            : base(toolName, configuration)
        {
            _outputFile = outputFile;
        }

        protected override IContainerizer CreateContainerizer()
        {
            return Containerizer.To(TarImage.Named(GetTargetImageReference()).SaveTo(_outputFile));
        }
    }
}