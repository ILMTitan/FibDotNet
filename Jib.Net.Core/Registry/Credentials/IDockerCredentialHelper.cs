using com.google.cloud.tools.jib.api;
using Jib.Net.Core.FileSystem;

namespace com.google.cloud.tools.jib.registry.credentials
{
    public interface IDockerCredentialHelper
    {
        SystemPath getCredentialHelper();
        Credential retrieve();
    }
}