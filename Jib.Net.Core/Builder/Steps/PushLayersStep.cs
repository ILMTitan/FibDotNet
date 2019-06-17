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
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{





    internal class PushLayersStep : AsyncStep<ImmutableArray<AsyncStep<PushBlobStep>>>
    {
        private static readonly string DESCRIPTION = "Setting up to push layers";

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly AuthenticatePushStep authenticatePushStep;

        private readonly AsyncStep<IReadOnlyList<AsyncStep<ICachedLayer>>>
            cachedLayerStep;

        private readonly Task<ImmutableArray<AsyncStep<PushBlobStep>>> listenableFuture;

        public PushLayersStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            AuthenticatePushStep authenticatePushStep,
            AsyncStep<IReadOnlyList<AsyncStep<ICachedLayer>>> cachedLayerStep)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.authenticatePushStep = authenticatePushStep;
            this.cachedLayerStep = cachedLayerStep;

            listenableFuture =
                AsyncDependencies.@using()
                    .addStep(cachedLayerStep)
                    .whenAllSucceedAsync(callAsync);
        }

        public Task<ImmutableArray<AsyncStep<PushBlobStep>>> getFuture()
        {
            return listenableFuture;
        }

        public async Task<ImmutableArray<AsyncStep<PushBlobStep>>> callAsync()
        {
            using (TimerEventDispatcher ignored =
                new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION))
            {
                IReadOnlyList<AsyncStep<ICachedLayer>> cachedLayers =
                    await cachedLayerStep.getFuture();

                using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.create("setting up to push layers", cachedLayers.size()))
                {
                    // Constructs a PushBlobStep for each layer.
                    ImmutableArray<AsyncStep<PushBlobStep>>.Builder pushBlobStepsBuilder =
                        ImmutableArray.CreateBuilder<AsyncStep<PushBlobStep>>();
                    foreach (AsyncStep<CachedLayer> cachedLayerStep in cachedLayers)
                    {
                        ProgressEventDispatcher.Factory childProgressEventDispatcherFactory =
                            progressEventDispatcher.newChildProducer();
                        await cachedLayerStep.getFuture();
                        Task<PushBlobStep> pushBlobStepFuture =
                            makePushBlobStepAsync(cachedLayerStep, childProgressEventDispatcherFactory);
                        pushBlobStepsBuilder.add(AsyncStep.Of(() => pushBlobStepFuture));
                    }

                    return pushBlobStepsBuilder.build();
                }
            }
        }

        private async Task<PushBlobStep> makePushBlobStepAsync(
            AsyncStep<CachedLayer> cachedLayerStep,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory)
        {
            CachedLayer cachedLayer = await cachedLayerStep.getFuture();

            return new PushBlobStep(
                buildConfiguration,
                progressEventDispatcherFactory,
                authenticatePushStep,
                new BlobDescriptor(cachedLayer.getSize(), cachedLayer.getDigest()),
                cachedLayer.getBlob());
        }
    }
}
