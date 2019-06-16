using System.Collections.Immutable;
using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Api;
using NodaTime;

namespace com.google.cloud.tools.jib.configuration
{
    public interface IContainerConfiguration
    {
        bool Equals(object other);
        Instant getCreationTime();
        ImmutableArray<string>? getEntrypoint();
        ImmutableDictionary<string, string> getEnvironmentMap();
        ImmutableHashSet<Port> getExposedPorts();
        int GetHashCode();
        ImmutableDictionary<string, string> getLabels();
        ImmutableArray<string>? getProgramArguments();
        string getUser();
        ImmutableHashSet<AbsoluteUnixPath> getVolumes();
        AbsoluteUnixPath getWorkingDirectory();
    }
}