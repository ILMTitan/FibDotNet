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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.async;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.image;
using Jib.Net.Core.Global;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{







    /** Adds image layers to a tarball and loads into Docker daemon. */
    internal class LoadDockerStep : AsyncStep<BuildResult>
    {
        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly DockerClient dockerClient;

        private readonly PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep;
        private readonly ImmutableArray<BuildAndCacheApplicationLayerStep> buildAndCacheApplicationLayerSteps;
        private readonly BuildImageStep buildImageStep;

        private readonly Task<BuildResult> listenableFuture;

        public LoadDockerStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            DockerClient dockerClient,
            PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep,
            ImmutableArray<BuildAndCacheApplicationLayerStep> buildAndCacheApplicationLayerSteps,
            BuildImageStep buildImageStep)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.dockerClient = dockerClient;
            this.pullAndCacheBaseImageLayersStep = pullAndCacheBaseImageLayersStep;
            this.buildAndCacheApplicationLayerSteps = buildAndCacheApplicationLayerSteps;
            this.buildImageStep = buildImageStep;

            listenableFuture =
                AsyncDependencies.@using()
                    .addStep(pullAndCacheBaseImageLayersStep)
                    .addStep(buildImageStep)
                    .whenAllSucceed(this);
        }

        public Task<BuildResult> getFuture()
        {
            return listenableFuture;
        }

        public BuildResult call()
        {
            return AsyncDependencies.@using()
                .addSteps(NonBlockingSteps.get(pullAndCacheBaseImageLayersStep))
                .addSteps(buildAndCacheApplicationLayerSteps)
                .addStep(NonBlockingSteps.get(buildImageStep))
                .whenAllSucceed(this.afterPushBaseImageLayerFuturesFuture)
                .get();
        }

        private BuildResult afterPushBaseImageLayerFuturesFuture()
        {
            buildConfiguration
                .getEventHandlers()
                .dispatch(LogEvent.progress("Loading to Docker daemon..."));

            using (ProgressEventDispatcher ignored =
                progressEventDispatcherFactory.create("loading to Docker daemon", 1))
            {
                Image image = NonBlockingSteps.get(NonBlockingSteps.get(buildImageStep));
                ImageReference targetImageReference =
                    buildConfiguration.getTargetImageConfiguration().getImage();

                // Load the image to docker daemon.
                buildConfiguration
                    .getEventHandlers()
                    .dispatch(
                        LogEvent.debug(dockerClient.load(new ImageTarball(image, targetImageReference))));

                // Tags the image with all the additional tags, skipping the one 'docker load' already loaded.
                foreach (string tag in buildConfiguration.getAllTargetImageTags())
                {
                    if (tag.Equals(targetImageReference.getTag()))
                    {
                        continue;
                    }

                    dockerClient.tag(targetImageReference, targetImageReference.withTag(tag));
                }

                return BuildResult.fromImage(image, buildConfiguration.getTargetFormat());
            }
        }
    }
}
