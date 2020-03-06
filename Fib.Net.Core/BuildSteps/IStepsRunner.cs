using Fib.Net.Core.Docker;
using Fib.Net.Core.FileSystem;
using System.Threading.Tasks;

namespace Fib.Net.Core.BuildSteps
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