using System.Collections.Immutable;
using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.cache;
using Jib.Net.Core.Registry;

namespace com.google.cloud.tools.jib.configuration
{
    public interface IBuildConfiguration
    {
        bool GetAllowInsecureRegistries();
        ImmutableHashSet<string> GetAllTargetImageTags();
        Cache GetApplicationLayersCache();
        ImageConfiguration GetBaseImageConfiguration();
        Cache GetBaseImageLayersCache();
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