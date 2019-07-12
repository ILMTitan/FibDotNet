using System.Collections.Immutable;

namespace Jib.Net.Core.Api
{
    public interface ILayerConfiguration
    {
        ImmutableArray<LayerEntry> LayerEntries { get; }
        string Name { get; }
    }
}