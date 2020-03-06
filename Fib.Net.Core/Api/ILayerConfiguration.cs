using System.Collections.Immutable;

namespace Fib.Net.Core.Api
{
    public interface ILayerConfiguration
    {
        ImmutableArray<LayerEntry> LayerEntries { get; }
        string Name { get; }
    }
}