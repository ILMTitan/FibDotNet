using System.IO;
using System.Threading.Tasks;

namespace Jib.Net.Cli
{
    public class PushCommandHandler
    {
        public async Task<int> FromConfigFileAsync(string toolName, FileInfo configFile)
        {
            var configuration = await JibCliConfiguration.LoadAsync(configFile).ConfigureAwait(false);
            var invoker = new PushCommandExecuter(toolName, configuration);
            await invoker.ExecuteAsync().ConfigureAwait(false);
            return 0;
        }
    }
}