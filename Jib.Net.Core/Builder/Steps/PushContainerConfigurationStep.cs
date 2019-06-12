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
    internal class PushContainerConfigurationStep : AsyncStep<AsyncStep<PushBlobStep>>
    {
        private static readonly string DESCRIPTION = "Pushing container configuration";

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly AuthenticatePushStep authenticatePushStep;
        private readonly BuildImageStep buildImageStep;

        private readonly Task<AsyncStep<PushBlobStep>> listenableFuture;

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

            listenableFuture =
                AsyncDependencies.@using()
                    .addStep(buildImageStep)
                    .whenAllSucceed(this);
        }

        public Task<AsyncStep<PushBlobStep>> getFuture()
        {
            return listenableFuture;
        }

        public AsyncStep<PushBlobStep> call()
        {
            Task<PushBlobStep> pushBlobStepFuture =
                AsyncDependencies.@using()
                    .addStep(authenticatePushStep)
                    .addStep(NonBlockingSteps.get(buildImageStep))
                    .whenAllSucceed(this.afterBuildConfigurationFutureFuture);
            return AsyncStep.Of(() => pushBlobStepFuture);
        }

        private PushBlobStep afterBuildConfigurationFutureFuture()
        {
            using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDispatcherFactory.create("pushing container configuration", 1))
            using (TimerEventDispatcher ignored =
                    new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION))
            {
                Image image = NonBlockingSteps.get(NonBlockingSteps.get(buildImageStep));
                JsonTemplate containerConfiguration =
                    new ImageToJsonTranslator(image).getContainerConfiguration();
                BlobDescriptor blobDescriptor = Digests.computeDigest(containerConfiguration);

                return new PushBlobStep(
                    buildConfiguration,
                    progressEventDispatcher.newChildProducer(),
                    authenticatePushStep,
                    blobDescriptor,
                    Blobs.from(containerConfiguration));
            }
        }
    }
}