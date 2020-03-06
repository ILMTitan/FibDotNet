// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using Fib.Net.Core.Async;
using Fib.Net.Core.Caching;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Docker;
using Fib.Net.Core.Events.Progress;
using Fib.Net.Core.FileSystem;
using System.Collections.Generic;
using System.Threading.Tasks;
using Runnable = System.Action;

namespace Fib.Net.Core.BuildSteps
{
    /**
     * Runs steps for building an image.
     *
     * <p>Use by first calling {@link #begin} and then calling the individual step running methods. Note
     * that order matters, so make sure that steps are run before other steps that depend on them. Wait
     * on the last step by calling the respective {@code wait...} methods.
     */
    public sealed class StepsRunner : IStepsRunner
    {
        /** Holds the individual steps. */
        private class Steps
        {
            public RetrieveRegistryCredentialsStep retrieveTargetRegistryCredentialsStep;
            public AuthenticatePushStep authenticatePushStep;
            public PullBaseImageStep pullBaseImageStep;
            public PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep;

            public IAsyncStep<IReadOnlyList<ICachedLayer>> buildAndCacheApplicationLayerSteps;

            public PushLayersStep pushBaseImageLayersStep;
            public PushLayersStep pushApplicationLayersStep;
            public BuildImageStep buildImageStep;
            public PushContainerConfigurationStep pushContainerConfigurationStep;

            public IAsyncStep<BuildResult> finalStep;
        }

        /**
         * Starts building the steps to run.
         *
         * @param buildConfiguration the {@link BuildConfiguration}
         * @return a new {@link StepsRunner}
         */
        public static StepsRunner Begin(BuildConfiguration buildConfiguration)
        {
            return new StepsRunner(buildConfiguration);
        }

        private readonly Steps steps = new Steps();

        private readonly BuildConfiguration buildConfiguration;

        /** Runnable to run all the steps. */
        private Runnable stepsRunnable = () => { };

        /** The total number of steps added. */
        private int stepsCount = 0;

        private string rootProgressAllocationDescription;
        private ProgressEventDispatcher rootProgressEventDispatcher;

        private StepsRunner(
            BuildConfiguration buildConfiguration)
        {
            this.buildConfiguration = buildConfiguration;
        }

        public StepsRunner RetrieveTargetRegistryCredentials()
        {
            return EnqueueStep(
                () =>
                    steps.retrieveTargetRegistryCredentialsStep =
                        RetrieveRegistryCredentialsStep.ForTargetImage(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer()));
        }

        public StepsRunner AuthenticatePush()
        {
            return EnqueueStep(
                () =>
                    steps.authenticatePushStep =
                        new AuthenticatePushStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.retrieveTargetRegistryCredentialsStep)));
        }

        public StepsRunner PullBaseImage()
        {
            return EnqueueStep(
                () =>
                    steps.pullBaseImageStep =
                        new PullBaseImageStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer()));
        }

        public StepsRunner PullAndCacheBaseImageLayers()
        {
            return EnqueueStep(
                () =>
                    steps.pullAndCacheBaseImageLayersStep =
                        new PullAndCacheBaseImageLayersStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.pullBaseImageStep)));
        }

        public StepsRunner PushBaseImageLayers()
        {
            return EnqueueStep(
                () =>
                    steps.pushBaseImageLayersStep =
                        new PushLayersStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.authenticatePushStep),
                            Preconditions.CheckNotNull(steps.pullAndCacheBaseImageLayersStep)));
        }

        public StepsRunner BuildAndCacheApplicationLayers()
        {
            return EnqueueStep(
                () =>
                    steps.buildAndCacheApplicationLayerSteps =
                        BuildAndCacheApplicationLayerStep.MakeList(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer()));
        }

        public StepsRunner BuildImage()
        {
            return EnqueueStep(
                () =>
                    steps.buildImageStep =
                        new BuildImageStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.pullBaseImageStep),
                            Preconditions.CheckNotNull(steps.pullAndCacheBaseImageLayersStep),
                            Preconditions.CheckNotNull(steps.buildAndCacheApplicationLayerSteps)));
        }

        public StepsRunner PushContainerConfiguration()
        {
            return EnqueueStep(
                () =>
                    steps.pushContainerConfigurationStep =
                        new PushContainerConfigurationStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.authenticatePushStep),
                            Preconditions.CheckNotNull(steps.buildImageStep)));
        }

        public StepsRunner PushApplicationLayers()
        {
            return EnqueueStep(
                () =>
                    steps.pushApplicationLayersStep =
                        new PushLayersStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.authenticatePushStep),
                            Preconditions.CheckNotNull(steps.buildAndCacheApplicationLayerSteps)));
        }

        public StepsRunner PushImage()
        {
            rootProgressAllocationDescription = "building image to registry";

            return EnqueueStep(
                () =>
                    steps.finalStep =
                        new PushImageStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.authenticatePushStep),
                            Preconditions.CheckNotNull(steps.pushBaseImageLayersStep),
                            Preconditions.CheckNotNull(steps.pushApplicationLayersStep),
                            Preconditions.CheckNotNull(steps.pushContainerConfigurationStep),
                            Preconditions.CheckNotNull(steps.buildImageStep)));
        }

        public StepsRunner LoadDocker(DockerClient dockerClient)
        {
            rootProgressAllocationDescription = "building image to Docker daemon";

            return EnqueueStep(
                () =>
                    steps.finalStep =
                        new LoadDockerStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            dockerClient,
                            Preconditions.CheckNotNull(steps.pullAndCacheBaseImageLayersStep),
                            Preconditions.CheckNotNull(steps.buildAndCacheApplicationLayerSteps),
                            Preconditions.CheckNotNull(steps.buildImageStep)));
        }

        public StepsRunner WriteTarFile(SystemPath outputPath)
        {
            rootProgressAllocationDescription = "building image to tar file";

            return EnqueueStep(
                () =>
                    steps.finalStep =
                        new WriteTarFileStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            outputPath,
                            Preconditions.CheckNotNull(steps.pullAndCacheBaseImageLayersStep),
                            Preconditions.CheckNotNull(steps.buildAndCacheApplicationLayerSteps),
                            Preconditions.CheckNotNull(steps.buildImageStep)));
        }

        public async Task<IBuildResult> RunAsync()
        {
            Preconditions.CheckNotNull(rootProgressAllocationDescription);

            using (ProgressEventDispatcher progressEventDispatcher =
                ProgressEventDispatcher.NewRoot(
                    buildConfiguration.GetEventHandlers(), rootProgressAllocationDescription, stepsCount))
            {
                rootProgressEventDispatcher = progressEventDispatcher;
                stepsRunnable();
                return await Preconditions.CheckNotNull(steps.finalStep).GetFuture().ConfigureAwait(false);
            }
        }

        private StepsRunner EnqueueStep(Runnable stepRunnable)
        {
            Runnable previousStepsRunnable = stepsRunnable;
            stepsRunnable =
                () =>
                {
                    previousStepsRunnable();
                    stepRunnable();
                };
            stepsCount++;
            return this;
        }
    }
}
