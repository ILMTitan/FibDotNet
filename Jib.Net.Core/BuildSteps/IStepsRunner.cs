using com.google.cloud.tools.jib.docker;
using Jib.Net.Core.FileSystem;
using System.Threading.Tasks;

namespace Jib.Net.Core.BuildSteps
{
    public interface IStepsRunner
    {
        StepsRunner AuthenticatePush();
        StepsRunner BuildAndCacheApplicationLayers();
        StepsRunner BuildImage();
        StepsRunner LoadDocker(DockerClient dockerClient);
        StepsRunner PullAndCacheBaseImageLayers();
        StepsRunner PullBaseImage();
        StepsRunner PushApplicationLayers();
        StepsRunner PushBaseImageLayers();
        StepsRunner PushContainerConfiguration();
        StepsRunner PushImage();
        StepsRunner RetrieveTargetRegistryCredentials();
        Task<IBuildResult> RunAsync();
        StepsRunner WriteTarFile(SystemPath outputPath);
    }
}