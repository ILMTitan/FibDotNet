using System.Collections.Immutable;
using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.registry;

namespace com.google.cloud.tools.jib.configuration
{
    public interface IBuildConfiguration
    {
        bool getAllowInsecureRegistries();
        ImmutableHashSet<string> getAllTargetImageTags();
        Cache getApplicationLayersCache();
        ImageConfiguration getBaseImageConfiguration();
        Cache getBaseImageLayersCache();
        IContainerConfiguration getContainerConfiguration();
        IEventHandlers getEventHandlers();
        ImmutableArray<ILayerConfiguration> getLayerConfigurations();
        ManifestFormat getTargetFormat();
        ImageConfiguration getTargetImageConfiguration();
        string getToolName();
        bool isOffline();
        RegistryClient.Factory newBaseImageRegistryClientFactory();
        RegistryClient.Factory newTargetImageRegistryClientFactory();
    }
}