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

using Jib.Net.Core.Async;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Configuration;
using Jib.Net.Core.Events.Progress;
using Jib.Net.Core.Events.Time;
using Jib.Net.Core.Hash;
using Jib.Net.Core.Images;
using Jib.Net.Core.Images.Json;
using System.Threading.Tasks;

namespace Jib.Net.Core.BuildSteps
{
    /** Pushes the container configuration. */
    internal class PushContainerConfigurationStep : IAsyncStep<BlobDescriptor>
    {
        private const string DESCRIPTION = "Pushing container configuration";

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly AuthenticatePushStep authenticatePushStep;
        private readonly BuildImageStep buildImageStep;

        private readonly Task<BlobDescriptor> listenableFuture;

        public PushContainerConfigurationStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            AuthenticatePushStep authenticatePushStep,
            BuildImageStep buildImageStep)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.authenticatePushStep = authenticatePushStep;
            this.buildImageStep = buildImageStep;

            listenableFuture = CallAsync();
        }

        public Task<BlobDescriptor> GetFuture()
        {
            return listenableFuture;
        }

        public async Task<BlobDescriptor> CallAsync()
        {
            Image image = await buildImageStep.GetFuture().ConfigureAwait(false);
            using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.Create("pushing container configuration", 1))
            using (TimerEventDispatcher ignored =
                    new TimerEventDispatcher(buildConfiguration.GetEventHandlers(), DESCRIPTION))
            {
                ContainerConfigurationTemplate containerConfiguration =
                    new ImageToJsonTranslator(image).GetContainerConfiguration();
                BlobDescriptor blobDescriptor =
                    await Digests.ComputeJsonDescriptorAsync(containerConfiguration).ConfigureAwait(false);

                return await new PushBlobStep(
                    buildConfiguration,
                    progressEventDispatcher.NewChildProducer(),
                    authenticatePushStep,
                    blobDescriptor,
                    Blobs.FromJson(containerConfiguration)).GetFuture().ConfigureAwait(false);
            }
        }
    }
}
