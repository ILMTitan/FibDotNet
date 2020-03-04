namespace Jib.Net.Core.Registry.Credentials
{
    public interface IDockerConfig
    {
        string GetAuthFor(string registry);
        IDockerCredentialHelper GetCredentialHelperFor(string registry);
    }
}