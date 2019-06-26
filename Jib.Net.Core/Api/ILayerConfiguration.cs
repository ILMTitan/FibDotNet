using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.api
{
    public interface ILayerConfiguration
    {
        ImmutableArray<LayerEntry> GetLayerEntries();
        string GetName();
    }
}