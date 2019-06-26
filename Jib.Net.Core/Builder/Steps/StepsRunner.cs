/*
 * Copyright 2018 Google LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using com.google.cloud.tools.jib.async;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.docker;
using Jib.Net.Core;
using Jib.Net.Core.Builder.Steps;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Runnable = System.Action;

namespace com.google.cloud.tools.jib.builder.steps
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
        public static StepsRunner begin(BuildConfiguration buildConfiguration)
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

        public StepsRunner retrieveTargetRegistryCredentials()
        {
            return enqueueStep(
                () =>
                    steps.retrieveTargetRegistryCredentialsStep =
                        RetrieveRegistryCredentialsStep.forTargetImage(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer()));
        }

        public StepsRunner authenticatePush()
        {
            return enqueueStep(
                () =>
                    steps.authenticatePushStep =
                        new AuthenticatePushStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.retrieveTargetRegistryCredentialsStep)));
        }

        public StepsRunner pullBaseImage()
        {
            return enqueueStep(
                () =>
                    steps.pullBaseImageStep =
                        new PullBaseImageStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer()));
        }

        public StepsRunner pullAndCacheBaseImageLayers()
        {
            return enqueueStep(
                () =>
                    steps.pullAndCacheBaseImageLayersStep =
                        new PullAndCacheBaseImageLayersStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.pullBaseImageStep)));
        }

        public StepsRunner pushBaseImageLayers()
        {
            return enqueueStep(
                () =>
                    steps.pushBaseImageLayersStep =
                        new PushLayersStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.authenticatePushStep),
                            Preconditions.CheckNotNull(steps.pullAndCacheBaseImageLayersStep)));
        }

        public StepsRunner buildAndCacheApplicationLayers()
        {
            return enqueueStep(
                () =>
                    steps.buildAndCacheApplicationLayerSteps =
                        BuildAndCacheApplicationLayerStep.makeList(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer()));
        }

        public StepsRunner buildImage()
        {
            return enqueueStep(
                () =>
                    steps.buildImageStep =
                        new BuildImageStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.pullBaseImageStep),
                            Preconditions.CheckNotNull(steps.pullAndCacheBaseImageLayersStep),
                            Preconditions.CheckNotNull(steps.buildAndCacheApplicationLayerSteps)));
        }

        public StepsRunner pushContainerConfiguration()
        {
            return enqueueStep(
                () =>
                    steps.pushContainerConfigurationStep =
                        new PushContainerConfigurationStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.authenticatePushStep),
                            Preconditions.CheckNotNull(steps.buildImageStep)));
        }

        public StepsRunner pushApplicationLayers()
        {
            return enqueueStep(
                () =>
                    steps.pushApplicationLayersStep =
                        new PushLayersStep(
                            buildConfiguration,
                            Preconditions.CheckNotNull(rootProgressEventDispatcher).NewChildProducer(),
                            Preconditions.CheckNotNull(steps.authenticatePushStep),
                            Preconditions.CheckNotNull(steps.buildAndCacheApplicationLayerSteps)));
        }

        public StepsRunner pushImage()
        {
            rootProgressAllocationDescription = "building image to registry";

            return enqueueStep(
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

        public StepsRunner loadDocker(DockerClient dockerClient)
        {
            rootProgressAllocationDescription = "building image to Docker daemon";

            return enqueueStep(
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

        public StepsRunner writeTarFile(SystemPath outputPath)
        {
            rootProgressAllocationDescription = "building image to tar file";

            return enqueueStep(
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

        public async Task<IBuildResult> runAsync()
        {
            Preconditions.CheckNotNull(rootProgressAllocationDescription);

            using (ProgressEventDispatcher progressEventDispatcher =
                ProgressEventDispatcher.NewRoot(
                    buildConfiguration.getEventHandlers(), rootProgressAllocationDescription, stepsCount))
            {
                rootProgressEventDispatcher = progressEventDispatcher;
                stepsRunnable.run();
                return await Preconditions.CheckNotNull(steps.finalStep).getFuture().ConfigureAwait(false);
            }
        }

        private StepsRunner enqueueStep(Runnable stepRunnable)
        {
            Runnable previousStepsRunnable = stepsRunnable;
            stepsRunnable =
                () =>
                {
                    previousStepsRunnable.run();
                    stepRunnable.run();
                };
            stepsCount++;
            return this;
        }
    }
}
