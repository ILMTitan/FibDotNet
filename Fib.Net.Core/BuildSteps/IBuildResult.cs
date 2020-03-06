using Fib.Net.Core.Api;

namespace Fib.Net.Core.BuildSteps
{
    public interface IBuildResult
    {
        bool Equals(object other);
        int GetHashCode();
        DescriptorDigest GetImageDigest();
        DescriptorDigest GetImageId();
    }
}