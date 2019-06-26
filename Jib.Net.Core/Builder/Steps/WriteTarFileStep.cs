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
using com.google.cloud.tools.jib.builder;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.filesystem;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Images;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Jib.Net.Core.Builder.Steps
{
    public class WriteTarFileStep : IAsyncStep<BuildResult>
    {
        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly SystemPath outputPath;
        private readonly PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep;
        private readonly IAsyncStep<IReadOnlyList<ICachedLayer>> buildAndCacheApplicationLayersStep;
        private readonly BuildImageStep buildImageStep;

        private readonly Task<BuildResult> listenableFuture;

        public WriteTarFileStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            SystemPath outputPath,
            PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep,
            IAsyncStep<IReadOnlyList<ICachedLayer>> buildAndCacheApplicationLayersStep,
            BuildImageStep buildImageStep)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.outputPath = outputPath;
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
            string description = string.Format(
                CultureInfo.CurrentCulture,
                Resources.WriteTarFileStepDescriptionFormat,
                outputPath.GetFileName());
            buildConfiguration.getEventHandlers().Dispatch(LogEvent.progress(description));

            using (progressEventDispatcherFactory.Create(description, 1))
            {
                Image image = await buildImageStep.getFuture().ConfigureAwait(false);

                // Builds the image to a tarball.
                Files.createDirectories(outputPath.GetParent());
                using (Stream outputStream =
                    new BufferedStream(FileOperations.newLockingOutputStream(outputPath)))
                {
                    await new ImageTarball(image, buildConfiguration.getTargetImageConfiguration().getImage())
                        .WriteToAsync(outputStream).ConfigureAwait(false);
                }

                return await BuildResult.fromImageAsync(image, buildConfiguration.getTargetFormat()).ConfigureAwait(false);
            }
        }
    }
}
