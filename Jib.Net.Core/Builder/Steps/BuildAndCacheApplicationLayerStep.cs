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
using com.google.cloud.tools.jib.image;
using Jib.Net.Core.Global;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{








    /** Builds and caches application layers. */
    public sealed class BuildAndCacheApplicationLayerStep : AsyncStep<ICachedLayer>
    {
        private static readonly string DESCRIPTION = "Building application layers";

        /**
         * Makes a list of {@link BuildAndCacheApplicationLayerStep} for dependencies, resources, and
         * classes layers. Optionally adds an extra layer if configured to do so.
         */
        public static AsyncStep<IReadOnlyList<ICachedLayer>> makeList(
            IBuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory)
        {
            int layerCount = buildConfiguration.getLayerConfigurations().size();

            using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.create(
                        "setting up to build application layers", layerCount))
            using (TimerEventDispatcher ignored =
                    new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION))

            {
                List<Task<ICachedLayer>> buildAndCacheApplicationLayerSteps = new List<Task<ICachedLayer>>();
                foreach (LayerConfiguration layerConfiguration in buildConfiguration.getLayerConfigurations())
                {
                    // Skips the layer if empty.
                    if (layerConfiguration.getLayerEntries().isEmpty())
                    {
                        continue;
                    }

                    buildAndCacheApplicationLayerSteps.add(
                        new BuildAndCacheApplicationLayerStep(
                            buildConfiguration,
                            progressEventDispatcher.newChildProducer(),
                            layerConfiguration.getName(),
                            layerConfiguration).getFuture());
                }
                return AsyncSteps.fromTasks(buildAndCacheApplicationLayerSteps);
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

            listenableFuture = Task.Run(callAsync);
        }

        public Task<ICachedLayer> getFuture()
        {
            return listenableFuture;
        }

        public async Task<ICachedLayer> callAsync()
        {
            string description = "Building " + layerType + " layer";

            buildConfiguration.getEventHandlers().dispatch(LogEvent.progress(description + "..."));

            using (ProgressEventDispatcher ignored =
                    progressEventDispatcherFactory.create("building " + layerType + " layer", 1))
            using (TimerEventDispatcher ignored2 =
                    new TimerEventDispatcher(buildConfiguration.getEventHandlers(), description))

            {
                Cache cache = buildConfiguration.getApplicationLayersCache();

                // Don't build the layer if it exists already.
                Optional<CachedLayer> optionalCachedLayer =
                    cache.retrieve(layerConfiguration.getLayerEntries());
                if (optionalCachedLayer.isPresent())
                {
                    return new CachedLayerWithType(optionalCachedLayer.get(), getLayerType());
                }

                Blob layerBlob = new ReproducibleLayerBuilder(layerConfiguration.getLayerEntries()).build();
                CachedLayer cachedLayer =
                    await cache.writeUncompressedLayerAsync(layerBlob, layerConfiguration.getLayerEntries());

                buildConfiguration
                    .getEventHandlers()
                    .dispatch(LogEvent.debug(description + " built " + cachedLayer.getDigest()));

                return new CachedLayerWithType(cachedLayer, getLayerType());
            }
        }

        public string getLayerType()
        {
            return layerType;
        }
    }
}
