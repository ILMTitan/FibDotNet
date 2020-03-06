using Fib.Net.Core.Api;
using Fib.Net.Core.FileSystem;

namespace Fib.Net.Core.Registry.Credentials
{
    public interface IDockerCredentialHelper
    {
        SystemPath GetCredentialHelper();
        Credential Retrieve();
    }
}