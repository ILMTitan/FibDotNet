namespace Fib.Net.Core.Api
{
    public interface IImageReference
    {
        string GetRegistry();
        string GetRepository();
        string GetTag();
        bool IsScratch();
        bool IsTagDigest();
        string ToString();
        string ToStringWithTag();
        bool UsesDefaultTag();
        ImageReference WithTag(string newTag);
    }
}