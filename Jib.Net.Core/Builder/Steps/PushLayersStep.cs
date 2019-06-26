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

using com.google.cloud.tools.jib.async;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{
    internal class PushLayersStep : IAsyncStep<IReadOnlyList<BlobDescriptor>>
    {
        private const string DESCRIPTION = "Setting up to push layers";

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly AuthenticatePushStep authenticatePushStep;

        private readonly IAsyncStep<IReadOnlyList<ICachedLayer>> cachedLayerStep;

        private readonly Task<IReadOnlyList<BlobDescriptor>> listenableFuture;

        public PushLayersStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            AuthenticatePushStep authenticatePushStep,
            IAsyncStep<IReadOnlyList<ICachedLayer>> cachedLayerStep)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.authenticatePushStep = authenticatePushStep;
            this.cachedLayerStep = cachedLayerStep;

            listenableFuture = callAsync();
        }

        public Task<IReadOnlyList<BlobDescriptor>> getFuture()
        {
            return listenableFuture;
        }

        public async Task<IReadOnlyList<BlobDescriptor>> callAsync()
        {
            IReadOnlyList<ICachedLayer> cachedLayers = await cachedLayerStep.getFuture().ConfigureAwait(false);
            using (TimerEventDispatcher ignored =
                new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION))
            {
                using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.Create("setting up to push layers", cachedLayers.size()))
                {
                    // Constructs a PushBlobStep for each layer.
                    var pushBlobSteps = new List<Task<BlobDescriptor>>();
                    foreach (ICachedLayer cachedLayer in cachedLayers)
                    {
                        ProgressEventDispatcher.Factory childProgressEventDispatcherFactory =
                            progressEventDispatcher.NewChildProducer();
                        Task<BlobDescriptor> pushBlobStepFuture =
                            pushBlobAsync(cachedLayer, childProgressEventDispatcherFactory);
                        pushBlobSteps.add(pushBlobStepFuture);
                    }

                    return await Task.WhenAll(pushBlobSteps).ConfigureAwait(false);
                }
            }
        }

        private async Task<BlobDescriptor> pushBlobAsync(
            ICachedLayer cachedLayer,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory)
        {
            return await new PushBlobStep(
                buildConfiguration,
                progressEventDispatcherFactory,
                authenticatePushStep,
                new BlobDescriptor(cachedLayer.getSize(), cachedLayer.getDigest()),
                cachedLayer.getBlob()).getFuture().ConfigureAwait(false);
        }
    }
}
