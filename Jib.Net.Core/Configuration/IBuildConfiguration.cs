using System.Collections.Immutable;
using Jib.Net.Core.Api;
using Jib.Net.Core.Caching;
using Jib.Net.Core.Registry;

namespace com.google.cloud.tools.jib.configuration
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