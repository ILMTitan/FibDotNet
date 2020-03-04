using Jib.Net.Core.Api;
using Jib.Net.Core.Events;
using System;

namespace Jib.Net.Core.Registry.Credentials
{
    public interface IDockerConfigCredentialRetriever
    {
        Maybe<Credential> Retrieve(Action<LogEvent> logger);
        Maybe<Credential> Retrieve(IDockerConfig dockerConfig, Action<LogEvent> logger);
    }
}