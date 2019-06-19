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
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.@event.progress;
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.registry;
using Jib.Net.Core;
using Jib.Net.Core.Blob;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{








    /** Pushes a BLOB to the target registry. */
    internal class PushBlobStep : AsyncStep<BlobDescriptor>
    {
        private static readonly string DESCRIPTION = "Pushing BLOB ";

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDipatcherFactory;

        private readonly AuthenticatePushStep authenticatePushStep;
        private readonly BlobDescriptor blobDescriptor;
        private readonly Blob blob;

        private readonly Task<BlobDescriptor> listenableFuture;

        public PushBlobStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDipatcherFactory,
            AuthenticatePushStep authenticatePushStep,
            BlobDescriptor blobDescriptor,
            Blob blob)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDipatcherFactory = progressEventDipatcherFactory;
            this.authenticatePushStep = authenticatePushStep;
            this.blobDescriptor = blobDescriptor;
            this.blob = blob;

            listenableFuture = callAsync();
        }

        public Task<BlobDescriptor> getFuture()
        {
            return listenableFuture;
        }

        public async Task<BlobDescriptor> callAsync()
        {
            Authorization authorization = await authenticatePushStep.getFuture();
            using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDipatcherFactory.create(
                        "pushing blob " + blobDescriptor.getDigest(), blobDescriptor.getSize()))
            using (TimerEventDispatcher ignored =
                    new TimerEventDispatcher(
                        buildConfiguration.getEventHandlers(), DESCRIPTION + blobDescriptor))
            using (
    ThrottledAccumulatingConsumer throttledProgressReporter =
        new ThrottledAccumulatingConsumer(progressEventDispatcher.dispatchProgress))
            {
                RegistryClient registryClient =
                    buildConfiguration
                        .newTargetImageRegistryClientFactory()
                        .setAuthorization(authorization)
                        .newRegistryClient();

                // check if the BLOB is available
                if (await registryClient.checkBlobAsync(blobDescriptor))
                {
                    buildConfiguration
                        .getEventHandlers()
                        .dispatch(LogEvent.info("BLOB : " + blobDescriptor + " already exists on registry"));
                    return blobDescriptor;
                }

                // todo: leverage cross-repository mounts
                await registryClient.pushBlobAsync(blobDescriptor.getDigest(), blob, null, throttledProgressReporter.accept);

                return blobDescriptor;
            }
        }
    }
}
