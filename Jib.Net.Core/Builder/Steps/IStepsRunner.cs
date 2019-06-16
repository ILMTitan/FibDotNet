using com.google.cloud.tools.jib.docker;
using Jib.Net.Core.FileSystem;

namespace com.google.cloud.tools.jib.builder.steps
{
    public interface IStepsRunner
    {
        StepsRunner authenticatePush();
        StepsRunner buildAndCacheApplicationLayers();
        StepsRunner buildImage();
        StepsRunner loadDocker(DockerClient dockerClient);
        StepsRunner pullAndCacheBaseImageLayers();
        StepsRunner pullBaseImage();
        StepsRunner pushApplicationLayers();
        StepsRunner pushBaseImageLayers();
        StepsRunner pushContainerConfiguration();
        StepsRunner pushImage();
        StepsRunner retrieveTargetRegistryCredentials();
        IBuildResult run();
        StepsRunner writeTarFile(SystemPath outputPath);
    }
}