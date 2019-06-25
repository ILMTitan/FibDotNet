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
using com.google.cloud.tools.jib.image.json;
using com.google.cloud.tools.jib.registry;
using Jib.Net.Core;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images;
using NodaTime;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using static Jib.Net.Core.Builder.Steps.PullBaseImageStep;

namespace com.google.cloud.tools.jib.builder.steps
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
            this.buildAndCacheApplicationLayersStep = buildAndCacheApplicationLayerSteps;

            listenableFuture = callAsync();
        }

        public Task<Image> getFuture()
        {
            return listenableFuture;
        }

        public async Task<Image> callAsync()
        {
            BaseImageWithAuthorization baseImageWithAuthorization = await pullBaseImageStep.getFuture().ConfigureAwait(false);
            IReadOnlyList<ICachedLayer> baseImageLayers = await pullAndCacheBaseImageLayersStep.getFuture().ConfigureAwait(false);
            IReadOnlyList<ICachedLayer> applicationLayers = await buildAndCacheApplicationLayersStep.getFuture().ConfigureAwait(false);

            using (progressEventDispatcherFactory.create("building image format", 1))
            using (new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION))
            {
                // Constructs the image.
                Image.Builder imageBuilder = Image.builder(buildConfiguration.getTargetFormat());
                Image baseImage = baseImageWithAuthorization.getBaseImage();
                IContainerConfiguration containerConfiguration =
                    buildConfiguration.getContainerConfiguration();

                // Base image layers
                foreach (ICachedLayer pullAndCacheBaseImageLayer in baseImageLayers)
                {
                    imageBuilder.addLayer(pullAndCacheBaseImageLayer);
                }

                // Passthrough config and count non-empty history entries
                int nonEmptyLayerCount = 0;
                foreach (HistoryEntry historyObject in baseImage.getHistory())
                {
                    imageBuilder.addHistory(historyObject);
                    if (!historyObject.hasCorrespondingLayer())
                    {
                        nonEmptyLayerCount++;
                    }
                }
                imageBuilder
                    .setArchitecture(baseImage.getArchitecture())
                    .setOs(baseImage.getOs())
                    .addEnvironment(baseImage.getEnvironment())
                    .addLabels(baseImage.getLabels())
                    .setHealthCheck(baseImage.getHealthCheck())
                    .addExposedPorts(baseImage.getExposedPorts())
                    .addVolumes(baseImage.getVolumes())
                    .setWorkingDirectory(baseImage.getWorkingDirectory());

                // Add history elements for non-empty layers that don't have one yet
                Instant layerCreationTime =
                    containerConfiguration == null
                        ? ContainerConfiguration.DefaultCreationTime
                        : containerConfiguration.getCreationTime();
                for (int count = 0; count < baseImageLayers.size() - nonEmptyLayerCount; count++)
                {
                    imageBuilder.addHistory(
                        HistoryEntry.builder()
                            .setCreationTimestamp(layerCreationTime)
                            .setComment("auto-generated by Jib")
                            .build());
                }

                // Add built layers/configuration
                foreach (ICachedLayer applicationLayer in applicationLayers)
                {
                    HistoryEntry.Builder historyBuilder = HistoryEntry.builder();
                    if (buildConfiguration.getToolName() != null) {
                        historyBuilder.setCreatedBy(buildConfiguration.getToolName() + ":" + (buildConfiguration.getToolVersion()??"null"));
                    } else
                    {
                        historyBuilder.setCreatedBy(ProjectInfo.TOOL_NAME + ":" + ProjectInfo.VERSION);
                    }
                    imageBuilder
                        .addLayer(applicationLayer)
                        .addHistory(
                            historyBuilder
                                .setCreationTimestamp(layerCreationTime)
                                .setAuthor("Jib")
                                .setComment(applicationLayer.getLayerType())
                                .build());
                }
                if (containerConfiguration != null)
                {
                    imageBuilder
                        .addEnvironment(containerConfiguration.getEnvironmentMap())
                        .setCreated(containerConfiguration.getCreationTime())
                        .setUser(containerConfiguration.getUser())
                        .setEntrypoint(computeEntrypoint(baseImage, containerConfiguration))
                        .setProgramArguments(computeProgramArguments(baseImage, containerConfiguration))
                        .addExposedPorts(containerConfiguration.getExposedPorts())
                        .addVolumes(containerConfiguration.getVolumes())
                        .addLabels(containerConfiguration.getLabels());
                    if (containerConfiguration.getWorkingDirectory() != null)
                    {
                        imageBuilder.setWorkingDirectory(containerConfiguration.getWorkingDirectory().toString());
                    }
                }

                // Gets the container configuration content descriptor.
                return imageBuilder.build();
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
        private ImmutableArray<string>? computeEntrypoint(
            Image baseImage, IContainerConfiguration containerConfiguration)
        {
            bool shouldInherit =
                baseImage.getEntrypoint() != null && containerConfiguration.getEntrypoint() == null;

            ImmutableArray<string>? entrypointToUse =
                shouldInherit ? baseImage.getEntrypoint() : containerConfiguration.getEntrypoint();

            if (entrypointToUse != null)
            {
                string logSuffix = shouldInherit ? " (inherited from base image)" : "";
                string message = "Container entrypoint set to " + entrypointToUse + logSuffix;
                buildConfiguration.getEventHandlers().dispatch(LogEvent.lifecycle(""));
                buildConfiguration.getEventHandlers().dispatch(LogEvent.lifecycle(message));
            }

            return entrypointToUse;
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
        private ImmutableArray<string>? computeProgramArguments(
            Image baseImage, IContainerConfiguration containerConfiguration)
        {
            bool shouldInherit =
                baseImage.getProgramArguments() != null
                    // Inherit CMD only when inheriting ENTRYPOINT.
                    && containerConfiguration.getEntrypoint() == null
                    && containerConfiguration.getProgramArguments() == null;

            ImmutableArray<string>? programArgumentsToUse =
                shouldInherit
                    ? baseImage.getProgramArguments()
                    : containerConfiguration.getProgramArguments();

            if (programArgumentsToUse != null)
            {
                string logSuffix = shouldInherit ? " (inherited from base image)" : "";
                string message = "Container program arguments set to " + programArgumentsToUse + logSuffix;
                buildConfiguration.getEventHandlers().dispatch(LogEvent.lifecycle(message));
            }

            return programArgumentsToUse;
        }
    }
}
