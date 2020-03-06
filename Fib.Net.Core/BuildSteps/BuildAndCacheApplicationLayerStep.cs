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
using Fib.Net.Core.Blob;
using Fib.Net.Core.Caching;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Events;
using Fib.Net.Core.Events.Progress;
using Fib.Net.Core.Events.Time;
using Fib.Net.Core.Images;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fib.Net.Core.BuildSteps
{
    /** Builds and caches application layers. */
    public sealed class BuildAndCacheApplicationLayerStep : IAsyncStep<ICachedLayer>
    {
        private const string Description = "Building application layers";

        /**
         * Makes a list of {@link BuildAndCacheApplicationLayerStep} for dependencies, resources, and
         * classes layers. Optionally adds an extra layer if configured to do so.
         */
        public static IAsyncStep<IReadOnlyList<ICachedLayer>> MakeList(
            IBuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory)
        {
            buildConfiguration = buildConfiguration ?? throw new ArgumentNullException(nameof(buildConfiguration));
            int layerCount = buildConfiguration.GetLayerConfigurations().Length;

            using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.Create(
                        "setting up to build application layers", layerCount))
            using (TimerEventDispatcher ignored =
                    new TimerEventDispatcher(buildConfiguration.GetEventHandlers(), Description))

            {
                List<Task<ICachedLayer>> buildAndCacheApplicationLayerSteps = new List<Task<ICachedLayer>>();
                foreach (LayerConfiguration layerConfiguration in buildConfiguration.GetLayerConfigurations())
                {
                    // Skips the layer if empty.
                    if (layerConfiguration.LayerEntries.Length == 0)
                    {
                        continue;
                    }

                    buildAndCacheApplicationLayerSteps.Add(
                        new BuildAndCacheApplicationLayerStep(
                            buildConfiguration,
                            progressEventDispatcher.NewChildProducer(),
                            layerConfiguration.Name,
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
                LayersCache cache = buildConfiguration.GetApplicationLayersCache();

                // Don't build the layer if it exists already.
                Maybe<CachedLayer> optionalCachedLayer =
                    await cache.RetrieveAsync(layerConfiguration.LayerEntries).ConfigureAwait(false);
                if (optionalCachedLayer.IsPresent())
                {
                    return new CachedLayerWithType(optionalCachedLayer.Get(), GetLayerType());
                }

                IBlob layerBlob = new ReproducibleLayerBuilder(layerConfiguration.LayerEntries).Build();
                CachedLayer cachedLayer =
                    await cache.WriteUncompressedLayerAsync(layerBlob, layerConfiguration.LayerEntries).ConfigureAwait(false);

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
