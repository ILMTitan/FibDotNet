namespace com.google.cloud.tools.jib.registry.credentials
{
    public interface IDockerConfig
    {
        string GetAuthFor(string registry);
        IDockerCredentialHelper GetCredentialHelperFor(string registry);
    }
}