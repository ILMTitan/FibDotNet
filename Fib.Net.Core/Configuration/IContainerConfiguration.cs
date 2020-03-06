using System.Collections.Immutable;
using Fib.Net.Core.Api;
using NodaTime;

namespace Fib.Net.Core.Configuration
{
    public interface IContainerConfiguration
    {
        bool Equals(object other);
        Instant GetCreationTime();
        ImmutableArray<string>? GetEntrypoint();
        ImmutableDictionary<string, string> GetEnvironmentMap();
        ImmutableHashSet<Port> GetExposedPorts();
        int GetHashCode();
        ImmutableDictionary<string, string> GetLabels();
        ImmutableArray<string>? GetProgramArguments();
        string GetUser();
        ImmutableHashSet<AbsoluteUnixPath> GetVolumes();
        AbsoluteUnixPath GetWorkingDirectory();
    }
}