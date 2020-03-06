using Fib.Net.Core.Api;
using Fib.Net.Core.Images;

namespace Fib.Net.Core.Caching
{
    public interface ICachedLayer : ILayer
    {
        DescriptorDigest GetDigest();
        long GetSize();
        string GetLayerType();
    }
}