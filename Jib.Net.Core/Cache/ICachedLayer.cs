using com.google.cloud.tools.jib.blob;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Images;

namespace com.google.cloud.tools.jib.cache
{
    public interface ICachedLayer: ILayer
    {
        DescriptorDigest getDigest();
        long getSize();
        string getLayerType();
    }
}