using Fib.Net.Core.Api;
using Fib.Net.Core.Events;
using System;

namespace Fib.Net.Core.Registry.Credentials
{
    public interface IDockerConfigCredentialRetriever
    {
        Maybe<Credential> Retrieve(Action<LogEvent> logger);
        Maybe<Credential> Retrieve(IDockerConfig dockerConfig, Action<LogEvent> logger);
    }
}