using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.image;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;

namespace com.google.cloud.tools.jib.cache
{
    public interface ICachedLayer: Layer
    {
        DescriptorDigest getDigest();
        long getSize();
        string getLayerType();
    }
}