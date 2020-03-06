using System.Collections.Immutable;
using Fib.Net.Core.Api;
using Fib.Net.Core.Caching;
using Fib.Net.Core.Registry;

namespace Fib.Net.Core.Configuration
{
    public interface IBuildConfiguration
    {
        bool GetAllowInsecureRegistries();
        ImmutableHashSet<string> GetAllTargetImageTags();
        LayersCache GetApplicationLayersCache();
        ImageConfiguration GetBaseImageConfiguration();
        LayersCache GetBaseImageLayersCache();
        IContainerConfiguration GetContainerConfiguration();
        IEventHandlers GetEventHandlers();
        ImmutableArray<ILayerConfiguration> GetLayerConfigurations();
        ManifestFormat GetTargetFormat();
        ImageConfiguration GetTargetImageConfiguration();
        string GetToolName();
        bool IsOffline();
        RegistryClient.Factory NewBaseImageRegistryClientFactory();
        RegistryClient.Factory NewTargetImageRegistryClientFactory();
        string GetToolVersion();
    }
}