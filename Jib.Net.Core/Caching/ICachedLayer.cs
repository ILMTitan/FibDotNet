using Jib.Net.Core.Api;
using Jib.Net.Core.Images;

namespace Jib.Net.Core.Caching
{
    public interface ICachedLayer : ILayer
    {
        DescriptorDigest GetDigest();
        long GetSize();
        string GetLayerType();
    }
}