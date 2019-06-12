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
using com.google.cloud.tools.jib.image;
using Jib.Net.Core;
using Jib.Net.Core.Global;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static com.google.cloud.tools.jib.builder.steps.PullBaseImageStep;

namespace com.google.cloud.tools.jib.builder.steps
{





    /** Pulls and caches the base image layers. */
    public class PullAndCacheBaseImageLayersStep : AsyncStep<IReadOnlyList<AsyncStep<CachedLayer>>>
    {
        private static readonly string DESCRIPTION = "Setting up base image caching";

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly PullBaseImageStep pullBaseImageStep;

        private readonly Task<IReadOnlyList<AsyncStep<CachedLayer>>> listenableFuture;

        public PullAndCacheBaseImageLayersStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            PullBaseImageStep pullBaseImageStep)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.pullBaseImageStep = pullBaseImageStep;

            listenableFuture =
                AsyncDependencies.@using()
                    .addStep(pullBaseImageStep)
                    .whenAllSucceed(this);
        }

        public Task<IReadOnlyList<AsyncStep<CachedLayer>>> getFuture()
        {
            return listenableFuture;
        }

        public ImmutableArray<PullAndCacheBaseImageLayerStep> call()
        {
            BaseImageWithAuthorization pullBaseImageStepResult = NonBlockingSteps.get(pullBaseImageStep);
            ImmutableArray<Layer> baseImageLayers = pullBaseImageStepResult.getBaseImage().getLayers();

            using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.create(
                        "checking base image layers", baseImageLayers.size()))
            using (TimerEventDispatcher ignored =
                    new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION))

            {
                ImmutableArray<PullAndCacheBaseImageLayerStep>.Builder pullAndCacheBaseImageLayerStepsBuilder =
                    ImmutableArray.CreateBuilder<PullAndCacheBaseImageLayerStep>();
                foreach (Layer layer in baseImageLayers)
                {
                    pullAndCacheBaseImageLayerStepsBuilder.add(
                        new PullAndCacheBaseImageLayerStep(
                            buildConfiguration,
                            progressEventDispatcher.newChildProducer(),
                            layer.getBlobDescriptor().getDigest(),
                            pullBaseImageStepResult.getBaseImageAuthorization()));
                }

                return pullAndCacheBaseImageLayerStepsBuilder.build();
            }
        }
    }
}