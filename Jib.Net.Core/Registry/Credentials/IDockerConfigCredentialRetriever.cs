using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Api;

namespace com.google.cloud.tools.jib.registry.credentials
{
    public interface IDockerConfigCredentialRetriever
    {
        Optional<Credential> retrieve(Consumer<LogEvent> logger);
        Optional<Credential> retrieve(IDockerConfig dockerConfig, Consumer<LogEvent> logger);
    }
}