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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.image;
using com.google.cloud.tools.jib.image.json;
using com.google.cloud.tools.jib.json;
using Jib.Net.Core;
using Jib.Net.Core.Blob;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
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

            listenableFuture = callAsync();
        }

        public Task<BlobDescriptor> getFuture()
        {
            return listenableFuture;
        }

        public async Task<BlobDescriptor> callAsync()
        {
            Image image = await buildImageStep.getFuture().ConfigureAwait(false);
            using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.create("pushing container configuration", 1))
            using (TimerEventDispatcher ignored =
                    new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION))
            {
                ContainerConfigurationTemplate containerConfiguration =
                    new ImageToJsonTranslator(image).getContainerConfiguration();
                BlobDescriptor blobDescriptor = 
                    await Digests.computeJsonDescriptorAsync(containerConfiguration).ConfigureAwait(false);

                return await new PushBlobStep(
                    buildConfiguration,
                    progressEventDispatcher.newChildProducer(),
                    authenticatePushStep,
                    blobDescriptor,
                    Blobs.fromJson(containerConfiguration)).getFuture().ConfigureAwait(false);
            }
        }
    }
}
