using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Api;
using System;

namespace com.google.cloud.tools.jib.registry.credentials
{
    public interface IDockerConfigCredentialRetriever
    {
        Optional<Credential> retrieve(Action<LogEvent> logger);
        Optional<Credential> retrieve(IDockerConfig dockerConfig, Action<LogEvent> logger);
    }
}