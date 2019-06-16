using Jib.Net.Core.Api;

namespace com.google.cloud.tools.jib.builder.steps
{
    public interface IBuildResult
    {
        bool Equals(object other);
        int GetHashCode();
        DescriptorDigest getImageDigest();
        DescriptorDigest getImageId();
    }
}