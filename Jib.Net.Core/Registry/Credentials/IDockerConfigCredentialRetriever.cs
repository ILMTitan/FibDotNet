using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Api;
using Jib.Net.Core.Events;
using System;

namespace com.google.cloud.tools.jib.registry.credentials
{
    public interface IDockerConfigCredentialRetriever
    {
        Option<Credential> Retrieve(Action<LogEvent> logger);
        Option<Credential> Retrieve(IDockerConfig dockerConfig, Action<LogEvent> logger);
    }
}