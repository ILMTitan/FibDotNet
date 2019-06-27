using Jib.Net.Core.Api;

namespace Jib.Net.Core.BuildSteps
{
    public interface IBuildResult
    {
        bool Equals(object other);
        int GetHashCode();
        DescriptorDigest GetImageDigest();
        DescriptorDigest GetImageId();
    }
}