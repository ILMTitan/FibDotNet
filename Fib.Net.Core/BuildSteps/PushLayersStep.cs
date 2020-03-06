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

using Fib.Net.Core.Async;
using Fib.Net.Core.Blob;
using Fib.Net.Core.Caching;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Events.Progress;
using Fib.Net.Core.Events.Time;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fib.Net.Core.BuildSteps
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

            listenableFuture = CallAsync();
        }

        public Task<IReadOnlyList<BlobDescriptor>> GetFuture()
        {
            return listenableFuture;
        }

        public async Task<IReadOnlyList<BlobDescriptor>> CallAsync()
        {
            IReadOnlyList<ICachedLayer> cachedLayers = await cachedLayerStep.GetFuture().ConfigureAwait(false);
            using (TimerEventDispatcher ignored =
                new TimerEventDispatcher(buildConfiguration.GetEventHandlers(), DESCRIPTION))
            {
                using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.Create("setting up to push layers", cachedLayers.Count))
                {
                    // Constructs a PushBlobStep for each layer.
                    var pushBlobSteps = new List<Task<BlobDescriptor>>();
                    foreach (ICachedLayer cachedLayer in cachedLayers)
                    {
                        pushBlobSteps.Add(PushBlobAsync(cachedLayer, progressEventDispatcher.NewChildProducer()));
                    }

                    return await Task.WhenAll(pushBlobSteps).ConfigureAwait(false);
                }
            }
        }

        private async Task<BlobDescriptor> PushBlobAsync(
            ICachedLayer cachedLayer,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory)
        {
            return await new PushBlobStep(
                buildConfiguration,
                progressEventDispatcherFactory,
                authenticatePushStep,
                new BlobDescriptor(cachedLayer.GetSize(), cachedLayer.GetDigest()),
                cachedLayer.GetBlob()).GetFuture().ConfigureAwait(false);
        }
    }
}
