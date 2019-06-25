using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Api;
using System;

namespace com.google.cloud.tools.jib.registry.credentials
{
    public interface IDockerConfigCredentialRetriever
    {
        Option<Credential> retrieve(Action<LogEvent> logger);
        Option<Credential> retrieve(IDockerConfig dockerConfig, Action<LogEvent> logger);
    }
}