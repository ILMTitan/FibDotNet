using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Events;
using System;

namespace com.google.cloud.tools.jib.registry.credentials
{
    public interface IDockerConfigCredentialRetriever
    {
        Maybe<Credential> Retrieve(Action<LogEvent> logger);
        Maybe<Credential> Retrieve(IDockerConfig dockerConfig, Action<LogEvent> logger);
    }
}