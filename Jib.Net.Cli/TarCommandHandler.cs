using System.IO;
using System.Threading.Tasks;

namespace Jib.Net.Cli
{
    public class TarCommandHandler
    {
        public async Task<int> BuildFromConfigFileAsync(string toolName, FileInfo outputFile, FileInfo configFile)
        {
            var configuration = await JibCliConfiguration.LoadAsync(configFile).ConfigureAwait(false);
            var invoker = new TarCommandExecuter(toolName, outputFile, configuration);
            await invoker.ExecuteAsync().ConfigureAwait(false);
            return 0;
        }
    }
}