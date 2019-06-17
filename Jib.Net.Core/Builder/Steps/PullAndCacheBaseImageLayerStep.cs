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
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.registry;
using Jib.Net.Core;
using Jib.Net.Core.Api;
using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{







    /** Pulls and caches a single base image layer. */
    public class PullAndCacheBaseImageLayerStep : AsyncStep<ICachedLayer>
    {
        private static readonly string DESCRIPTION = "Pulling base image layer {0}";

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly DescriptorDigest layerDigest;
        private readonly Authorization pullAuthorization;

        private readonly Task<ICachedLayer> listenableFuture;

        public PullAndCacheBaseImageLayerStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            DescriptorDigest layerDigest,
            Authorization pullAuthorization)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.layerDigest = layerDigest;
            this.pullAuthorization = pullAuthorization;

            listenableFuture = Task.Run(callAsync);
        }

        public Task<ICachedLayer> getFuture()
        {
            return listenableFuture;
        }

        public async Task<ICachedLayer> callAsync()
        {
            using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.create("checking base image layer " + layerDigest, 1))
            using (TimerEventDispatcher ignored =
                    new TimerEventDispatcher(
                        buildConfiguration.getEventHandlers(), string.Format(DESCRIPTION, layerDigest)))
            {
                Cache cache = buildConfiguration.getBaseImageLayersCache();

                // Checks if the layer already exists in the cache.
                Optional<CachedLayer> optionalCachedLayer = cache.retrieve(layerDigest);
                if (optionalCachedLayer.isPresent())
                {
                    return optionalCachedLayer.get();
                }
                else if (buildConfiguration.isOffline())
                {
                    throw new IOException(
                        "Cannot run Jib in offline mode; local Jib cache for base image is missing image layer "
                            + layerDigest
                            + ". You may need to rerun Jib in online mode to re-download the base image layers.");
                }

                RegistryClient registryClient =
                    buildConfiguration
                        .newBaseImageRegistryClientFactory()
                        .setAuthorization(pullAuthorization)
                        .newRegistryClient();

                using (ThrottledProgressEventDispatcherWrapper progressEventDispatcherWrapper =
                    new ThrottledProgressEventDispatcherWrapper(
                        progressEventDispatcher.newChildProducer(),
                        "pulling base image layer " + layerDigest))
                {
                    return await cache.writeCompressedLayerAsync(
                        registryClient.pullBlob(
                            layerDigest,
                            progressEventDispatcherWrapper.setProgressTarget,
                            progressEventDispatcherWrapper.dispatchProgress));
                }
            }
        }
    }
}
