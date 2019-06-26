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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{
    /** Builds and caches application layers. */
    public sealed class BuildAndCacheApplicationLayerStep : IAsyncStep<ICachedLayer>
    {
        private const string DESCRIPTION = "Building application layers";

        /**
         * Makes a list of {@link BuildAndCacheApplicationLayerStep} for dependencies, resources, and
         * classes layers. Optionally adds an extra layer if configured to do so.
         */
        public static IAsyncStep<IReadOnlyList<ICachedLayer>> MakeList(
            IBuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory)
        {
            buildConfiguration = buildConfiguration ?? throw new ArgumentNullException(nameof(buildConfiguration));
            int layerCount = buildConfiguration.GetLayerConfigurations().Size();

            using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.Create(
                        "setting up to build application layers", layerCount))
            using (TimerEventDispatcher ignored =
                    new TimerEventDispatcher(buildConfiguration.GetEventHandlers(), DESCRIPTION))

            {
                List<Task<ICachedLayer>> buildAndCacheApplicationLayerSteps = new List<Task<ICachedLayer>>();
                foreach (LayerConfiguration layerConfiguration in buildConfiguration.GetLayerConfigurations())
                {
                    // Skips the layer if empty.
                    if (layerConfiguration.GetLayerEntries().IsEmpty())
                    {
                        continue;
                    }

                    JavaExtensions.Add(
buildAndCacheApplicationLayerSteps, new BuildAndCacheApplicationLayerStep(
                            buildConfiguration,
                            progressEventDispatcher.NewChildProducer(),
                            layerConfiguration.GetName(),
                            layerConfiguration).GetFuture());
                }
                return AsyncSteps.FromTasks(buildAndCacheApplicationLayerSteps);
            }
        }

        private readonly IBuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly string layerType;
        private readonly LayerConfiguration layerConfiguration;

        private readonly Task<ICachedLayer> listenableFuture;

        private BuildAndCacheApplicationLayerStep(
            IBuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            string layerType,
            LayerConfiguration layerConfiguration)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.layerType = layerType;
            this.layerConfiguration = layerConfiguration;

            listenableFuture = Task.Run(CallAsync);
        }

        public Task<ICachedLayer> GetFuture()
        {
            return listenableFuture;
        }

        public async Task<ICachedLayer> CallAsync()
        {
            string description = "Building " + layerType + " layer";

            buildConfiguration.GetEventHandlers().Dispatch(LogEvent.Progress(description + "..."));

            using (ProgressEventDispatcher ignored =
                    progressEventDispatcherFactory.Create("building " + layerType + " layer", 1))
            using (TimerEventDispatcher ignored2 =
                    new TimerEventDispatcher(buildConfiguration.GetEventHandlers(), description))

            {
                Cache cache = buildConfiguration.GetApplicationLayersCache();

                // Don't build the layer if it exists already.
                Option<CachedLayer> optionalCachedLayer =
                    await cache.RetrieveAsync(layerConfiguration.GetLayerEntries()).ConfigureAwait(false);
                if (optionalCachedLayer.IsPresent())
                {
                    return new CachedLayerWithType(optionalCachedLayer.Get(), GetLayerType());
                }

                IBlob layerBlob = new ReproducibleLayerBuilder(layerConfiguration.GetLayerEntries()).Build();
                CachedLayer cachedLayer =
                    await cache.WriteUncompressedLayerAsync(layerBlob, layerConfiguration.GetLayerEntries()).ConfigureAwait(false);

                buildConfiguration
                    .GetEventHandlers()
                    .Dispatch(LogEvent.Debug(description + " built " + cachedLayer.GetDigest()));

                return new CachedLayerWithType(cachedLayer, GetLayerType());
            }
        }

        public string GetLayerType()
        {
            return layerType;
        }
    }
}
