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
using Jib.Net.Core.Caching;
using Jib.Net.Core.Events;
using Jib.Net.Core.Events.Progress;
using Jib.Net.Core.Images;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jib.Net.Core.BuildSteps
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

            listenableFuture = CallAsync();
        }

        public Task<BuildResult> GetFuture()
        {
            return listenableFuture;
        }

        public async Task<BuildResult> CallAsync()
        {
            await pullAndCacheBaseImageLayersStep.GetFuture().ConfigureAwait(false);
            await buildAndCacheApplicationLayersStep.GetFuture().ConfigureAwait(false);
            await buildImageStep.GetFuture().ConfigureAwait(false);
            buildConfiguration
                .GetEventHandlers()
                .Dispatch(LogEvent.Progress(Resources.LoadDockerStepDescription));

            using (progressEventDispatcherFactory.Create(Resources.LoadDockerStepDescription, 1))
            {
                Image image = await buildImageStep.GetFuture().ConfigureAwait(false);
                IImageReference targetImageReference =
                    buildConfiguration.GetTargetImageConfiguration().GetImage();

                // Load the image to docker daemon.
                buildConfiguration
                    .GetEventHandlers()
                    .Dispatch(
                        LogEvent.Debug(await dockerClient.LoadAsync(new ImageTarball(image, targetImageReference)).ConfigureAwait(false)));

                // Tags the image with all the additional tags, skipping the one 'docker load' already loaded.
                foreach (string tag in buildConfiguration.GetAllTargetImageTags())
                {
                    if (tag.Equals(targetImageReference.GetTag(), StringComparison.Ordinal))
                    {
                        continue;
                    }

                    dockerClient.Tag(targetImageReference, targetImageReference.WithTag(tag));
                }

                return await BuildResult.FromImageAsync(image, buildConfiguration.GetTargetFormat()).ConfigureAwait(false);
            }
        }
    }
}
