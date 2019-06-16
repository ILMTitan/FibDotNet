namespace com.google.cloud.tools.jib.registry.credentials
{
    public interface IDockerConfig
    {
        string getAuthFor(string registry);
        IDockerCredentialHelper getCredentialHelperFor(string registry);
    }
}