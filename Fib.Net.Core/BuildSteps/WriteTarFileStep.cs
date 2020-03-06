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
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Images;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Core.BuildSteps
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
            string description = string.Format(
                CultureInfo.CurrentCulture,
                Resources.WriteTarFileStepDescriptionFormat,
                outputPath.GetFileName());
            buildConfiguration.GetEventHandlers().Dispatch(LogEvent.Progress(description));

            using (progressEventDispatcherFactory.Create(description, 1))
            {
                Image image = await buildImageStep.GetFuture().ConfigureAwait(false);

                // Builds the image to a tarball.
                Files.CreateDirectories(outputPath.GetParent());
                using (Stream outputStream =
                    new BufferedStream(FileOperations.NewLockingOutputStream(outputPath)))
                {
                    await new ImageTarball(image, buildConfiguration.GetTargetImageConfiguration().GetImage())
                        .WriteToAsync(outputStream).ConfigureAwait(false);
                }

                return await BuildResult.FromImageAsync(image, buildConfiguration.GetTargetFormat()).ConfigureAwait(false);
            }
        }
    }
}
