namespace com.google.cloud.tools.jib.api
{
    public interface IImageReference
    {
        string getRegistry();
        string getRepository();
        string getTag();
        bool isScratch();
        bool isTagDigest();
        string ToString();
        string toStringWithTag();
        bool usesDefaultTag();
        ImageReference withTag(string newTag);
    }
}