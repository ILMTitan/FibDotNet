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

using Fib.Net.Core.Caching;
using Fib.Net.Core.Events;
using Fib.Net.Core.Events.Progress;
using Fib.Net.Core.Events.Time;
using Fib.Net.Core.Images;
using Fib.Net.Core.Images.Json;
using NodaTime;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static Fib.Net.Core.BuildSteps.PullBaseImageStep;
using System.Web;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Async;

namespace Fib.Net.Core.BuildSteps
{
    /** Builds a model {@link Image}. */
    public class BuildImageStep : IAsyncStep<Image>
    {
        private const string DESCRIPTION = "Building container configuration";

        private readonly IBuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;
        private readonly IAsyncStep<BaseImageWithAuthorization> pullBaseImageStep;
        private readonly IAsyncStep<IReadOnlyList<ICachedLayer>> pullAndCacheBaseImageLayersStep;
        private readonly IAsyncStep<IReadOnlyList<ICachedLayer>> buildAndCacheApplicationLayersStep;

        private readonly Task<Image> listenableFuture;

        public BuildImageStep(
            IBuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            IAsyncStep<BaseImageWithAuthorization> pullBaseImageStep,
            IAsyncStep<IReadOnlyList<ICachedLayer>> pullAndCacheBaseImageLayersStep,
            IAsyncStep<IReadOnlyList<ICachedLayer>> buildAndCacheApplicationLayerSteps)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.pullBaseImageStep = pullBaseImageStep;
            this.pullAndCacheBaseImageLayersStep = pullAndCacheBaseImageLayersStep;
            buildAndCacheApplicationLayersStep = buildAndCacheApplicationLayerSteps;

            listenableFuture = CallAsync();
        }

        public Task<Image> GetFuture()
        {
            return listenableFuture;
        }

        public async Task<Image> CallAsync()
        {
            BaseImageWithAuthorization baseImageWithAuthorization = await pullBaseImageStep.GetFuture().ConfigureAwait(false);
            IReadOnlyList<ICachedLayer> baseImageLayers = await pullAndCacheBaseImageLayersStep.GetFuture().ConfigureAwait(false);
            IReadOnlyList<ICachedLayer> applicationLayers = await buildAndCacheApplicationLayersStep.GetFuture().ConfigureAwait(false);

            using (progressEventDispatcherFactory.Create("building image format", 1))
            using (new TimerEventDispatcher(buildConfiguration.GetEventHandlers(), DESCRIPTION))
            {
                // Constructs the image.
                Image.Builder imageBuilder = Image.CreateBuilder(buildConfiguration.GetTargetFormat());
                Image baseImage = baseImageWithAuthorization.GetBaseImage();
                IContainerConfiguration containerConfiguration =
                    buildConfiguration.GetContainerConfiguration();

                // Base image layers
                foreach (ICachedLayer pullAndCacheBaseImageLayer in baseImageLayers)
                {
                    imageBuilder.AddLayer(pullAndCacheBaseImageLayer);
                }

                // Passthrough config and count non-empty history entries
                int nonEmptyLayerCount = 0;
                foreach (HistoryEntry historyObject in baseImage.GetHistory())
                {
                    imageBuilder.AddHistory(historyObject);
                    if (!historyObject.HasCorrespondingLayer())
                    {
                        nonEmptyLayerCount++;
                    }
                }
                imageBuilder
                    .SetArchitecture(baseImage.GetArchitecture())
                    .SetOs(baseImage.GetOs())
                    .AddEnvironment(baseImage.GetEnvironment())
                    .AddLabels(baseImage.GetLabels())
                    .SetHealthCheck(baseImage.GetHealthCheck())
                    .AddExposedPorts(baseImage.GetExposedPorts())
                    .AddVolumes(baseImage.GetVolumes())
                    .SetWorkingDirectory(baseImage.GetWorkingDirectory());

                // Add history elements for non-empty layers that don't have one yet
                Instant layerCreationTime =
                    containerConfiguration == null
                        ? ContainerConfiguration.DefaultCreationTime
                        : containerConfiguration.GetCreationTime();
                for (int count = 0; count < baseImageLayers.Count - nonEmptyLayerCount; count++)
                {
                    imageBuilder.AddHistory(
                        HistoryEntry.CreateBuilder()
                            .SetCreationTimestamp(layerCreationTime)
                            .SetComment("auto-generated by Fib")
                            .Build());
                }

                // Add built layers/configuration
                foreach (ICachedLayer applicationLayer in applicationLayers)
                {
                    HistoryEntry.Builder historyBuilder = HistoryEntry.CreateBuilder();
                    if (buildConfiguration.GetToolName() != null)
                    {
                        historyBuilder.SetCreatedBy(buildConfiguration.GetToolName() + ":" + (buildConfiguration.GetToolVersion() ?? "null"));
                    }
                    else
                    {
                        historyBuilder.SetCreatedBy(ProjectInfo.TOOL_NAME + ":" + ProjectInfo.VERSION);
                    }
                    imageBuilder
                        .AddLayer(applicationLayer)
                        .AddHistory(
                            historyBuilder
                                .SetCreationTimestamp(layerCreationTime)
                                .SetAuthor("Fib")
                                .SetComment(applicationLayer.GetLayerType())
                                .Build());
                }
                if (containerConfiguration != null)
                {
                    imageBuilder
                        .AddEnvironment(containerConfiguration.GetEnvironmentMap())
                        .SetCreated(containerConfiguration.GetCreationTime())
                        .SetUser(containerConfiguration.GetUser())
                        .SetEntrypoint(ComputeEntrypoint(baseImage, containerConfiguration))
                        .SetProgramArguments(ComputeProgramArguments(baseImage, containerConfiguration))
                        .AddExposedPorts(containerConfiguration.GetExposedPorts())
                        .AddVolumes(containerConfiguration.GetVolumes())
                        .AddLabels(containerConfiguration.GetLabels());
                    if (containerConfiguration.GetWorkingDirectory() != null)
                    {
                        imageBuilder.SetWorkingDirectory(containerConfiguration.GetWorkingDirectory().ToString());
                    }
                }

                // Gets the container configuration content descriptor.
                return imageBuilder.Build();
            }
        }

        /**
         * Computes the image entrypoint. If {@link ContainerConfiguration#getEntrypoint()} is null, the
         * entrypoint is inherited from the base image. Otherwise {@link
         * ContainerConfiguration#getEntrypoint()} is returned.
         *
         * @param baseImage the base image
         * @param containerConfiguration the container configuration
         * @return the container entrypoint
         */
        private ImmutableArray<string>? ComputeEntrypoint(
            Image baseImage, IContainerConfiguration containerConfiguration)
        {
            bool shouldInherit =
                baseImage.GetEntrypoint() != null && containerConfiguration.GetEntrypoint() == null;

            ImmutableArray<string>? entrypointToUse =
                shouldInherit ? baseImage.GetEntrypoint() : containerConfiguration.GetEntrypoint();

            if (entrypointToUse != null)
            {
                string logSuffix = shouldInherit ? " (inherited from base image)" : "";
                string message = "Container entrypoint set to " +
                    $"[{string.Join(", ", entrypointToUse?.Select(ToJavascriptString))}]" +
                    logSuffix;
                buildConfiguration.GetEventHandlers().Dispatch(LogEvent.Lifecycle(""));
                buildConfiguration.GetEventHandlers().Dispatch(LogEvent.Lifecycle(message));
            }

            return entrypointToUse;
        }

        private static string ToJavascriptString(string s)
        {
            return $"\"{HttpUtility.JavaScriptStringEncode(s)}\"";
        }

        /**
         * Computes the image program arguments. If {@link ContainerConfiguration#getEntrypoint()} and
         * {@link ContainerConfiguration#getProgramArguments()} are null, the program arguments are
         * inherited from the base image. Otherwise {@link ContainerConfiguration#getProgramArguments()}
         * is returned.
         *
         * @param baseImage the base image
         * @param containerConfiguration the container configuration
         * @return the container program arguments
         */
        private ImmutableArray<string>? ComputeProgramArguments(
            Image baseImage, IContainerConfiguration containerConfiguration)
        {
            bool shouldInherit =
                baseImage.GetProgramArguments() != null
                    // Inherit CMD only when inheriting ENTRYPOINT.
                    && containerConfiguration.GetEntrypoint() == null
                    && containerConfiguration.GetProgramArguments() == null;

            ImmutableArray<string>? programArgumentsToUse =
                shouldInherit
                    ? baseImage.GetProgramArguments()
                    : containerConfiguration.GetProgramArguments();

            if (programArgumentsToUse != null)
            {
                string logSuffix = shouldInherit ? " (inherited from base image)" : "";
                string message = "Container program arguments set to " +
                    $"[{string.Join(", ", programArgumentsToUse?.Select(ToJavascriptString))}]" +
                    logSuffix;
                buildConfiguration.GetEventHandlers().Dispatch(LogEvent.Lifecycle(message));
            }

            return programArgumentsToUse;
        }
    }
}
