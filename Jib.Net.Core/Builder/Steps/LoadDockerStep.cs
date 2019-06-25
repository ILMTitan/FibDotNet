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
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.image;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{
    /** Adds image layers to a tarball and loads into Docker daemon. */
    internal class LoadDockerStep : IAsyncStep<BuildResult>
    {
        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly DockerClient dockerClient;

        private readonly PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep;
        private readonly IAsyncStep<IReadOnlyList<ICachedLayer>> buildAndCacheApplicationLayersStep;
        private readonly BuildImageStep buildImageStep;

        private readonly Task<BuildResult> listenableFuture;

        public LoadDockerStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            DockerClient dockerClient,
            PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep,
            IAsyncStep<IReadOnlyList<ICachedLayer>> buildAndCacheApplicationLayersStep,
            BuildImageStep buildImageStep)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.dockerClient = dockerClient;
            this.pullAndCacheBaseImageLayersStep = pullAndCacheBaseImageLayersStep;
            this.buildAndCacheApplicationLayersStep = buildAndCacheApplicationLayersStep;
            this.buildImageStep = buildImageStep;

            listenableFuture = callAsync();
        }

        public Task<BuildResult> getFuture()
        {
            return listenableFuture;
        }

        public async Task<BuildResult> callAsync()
        {
            await pullAndCacheBaseImageLayersStep.getFuture().ConfigureAwait(false);
            await buildAndCacheApplicationLayersStep.getFuture().ConfigureAwait(false);
            await buildImageStep.getFuture().ConfigureAwait(false);
            buildConfiguration
                .getEventHandlers()
                .dispatch(LogEvent.progress("Loading to Docker daemon..."));

            using (ProgressEventDispatcher ignored =
                progressEventDispatcherFactory.create("loading to Docker daemon", 1))
            {
                Image image = await buildImageStep.getFuture().ConfigureAwait(false);
                IImageReference targetImageReference =
                    buildConfiguration.getTargetImageConfiguration().getImage();

                // Load the image to docker daemon.
                buildConfiguration
                    .getEventHandlers()
                    .dispatch(
                        LogEvent.debug(await dockerClient.loadAsync(new ImageTarball(image, targetImageReference)).ConfigureAwait(false)));

                // Tags the image with all the additional tags, skipping the one 'docker load' already loaded.
                foreach (string tag in buildConfiguration.getAllTargetImageTags())
                {
                    if (tag.Equals(targetImageReference.getTag(), StringComparison.Ordinal))
                    {
                        continue;
                    }

                    dockerClient.tag(targetImageReference, targetImageReference.withTag(tag));
                }

                return await BuildResult.fromImageAsync(image, buildConfiguration.getTargetFormat()).ConfigureAwait(false);
            }
        }
    }
}
