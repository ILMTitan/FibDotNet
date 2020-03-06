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

using Fib.Net.Core.Api;
using Fib.Net.Core.Async;
using Fib.Net.Core.Caching;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Docker;
using Fib.Net.Core.Events;
using Fib.Net.Core.Events.Progress;
using Fib.Net.Core.Images;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fib.Net.Core.BuildSteps
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

                    ImageReference taggedImageReference = targetImageReference.WithTag(tag);
                    await dockerClient.TagAsync(targetImageReference, taggedImageReference).ConfigureAwait(false);
                }

                return await BuildResult.FromImageAsync(image, buildConfiguration.GetTargetFormat()).ConfigureAwait(false);
            }
        }
    }
}
