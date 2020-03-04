using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;

namespace Jib.Net.Core.Registry.Credentials
{
    public interface IDockerCredentialHelper
    {
        SystemPath GetCredentialHelper();
        Credential Retrieve();
    }
}