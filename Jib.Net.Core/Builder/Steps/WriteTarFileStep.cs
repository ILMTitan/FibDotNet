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
using com.google.cloud.tools.jib.filesystem;
using com.google.cloud.tools.jib.image;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{










    public class WriteTarFileStep : AsyncStep<BuildResult>
    {
        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly SystemPath outputPath;
        private readonly PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep;
        private readonly ImmutableArray<BuildAndCacheApplicationLayerStep> buildAndCacheApplicationLayerSteps;
        private readonly BuildImageStep buildImageStep;

        private readonly Task<BuildResult> listenableFuture;

        public WriteTarFileStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            SystemPath outputPath,
            PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep,
            ImmutableArray<BuildAndCacheApplicationLayerStep> buildAndCacheApplicationLayerSteps,
            BuildImageStep buildImageStep)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.outputPath = outputPath;
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
                .whenAllSucceed(writeTarFile)
                .get();
        }

        private BuildResult writeTarFile()
        {
            buildConfiguration
                .getEventHandlers()
                .dispatch(LogEvent.progress("Building image to tar file..."));

            using (ProgressEventDispatcher ignored =
                progressEventDispatcherFactory.create("writing to tar file", 1))
            {
                Image image = NonBlockingSteps.get(NonBlockingSteps.get(buildImageStep));

                // Builds the image to a tarball.
                Files.createDirectories(outputPath.getParent());
                using (Stream outputStream =
                    new BufferedStream(FileOperations.newLockingOutputStream(outputPath)))
                {
                    new ImageTarball(image, buildConfiguration.getTargetImageConfiguration().getImage())
                        .writeTo(outputStream);
                }

                return BuildResult.fromImage(image, buildConfiguration.getTargetFormat());
            }
        }
    }
}