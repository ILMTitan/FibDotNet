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
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.image.json;
using com.google.cloud.tools.jib.registry;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{













    /** Pushes the final image. Outputs the pushed image digest. */
    internal class PushImageStep : AsyncStep<BuildResult>
    {
        private static readonly string DESCRIPTION = "Pushing new image";

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly AuthenticatePushStep authenticatePushStep;

        private readonly PushLayersStep pushBaseImageLayersStep;
        private readonly PushLayersStep pushApplicationLayersStep;
        private readonly PushContainerConfigurationStep pushContainerConfigurationStep;
        private readonly BuildImageStep buildImageStep;

        private readonly Task<BuildResult> listenableFuture;

        public PushImageStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            AuthenticatePushStep authenticatePushStep,
            PushLayersStep pushBaseImageLayersStep,
            PushLayersStep pushApplicationLayersStep,
            PushContainerConfigurationStep pushContainerConfigurationStep,
            BuildImageStep buildImageStep)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.authenticatePushStep = authenticatePushStep;
            this.pushBaseImageLayersStep = pushBaseImageLayersStep;
            this.pushApplicationLayersStep = pushApplicationLayersStep;
            this.pushContainerConfigurationStep = pushContainerConfigurationStep;
            this.buildImageStep = buildImageStep;

            listenableFuture =
                AsyncDependencies.@using()
                    .addStep(pushBaseImageLayersStep)
                    .addStep(pushApplicationLayersStep)
                    .addStep(pushContainerConfigurationStep)
                    .whenAllSucceedAsync(callAsync);
        }

        public Task<BuildResult> getFuture()
        {
            return listenableFuture;
        }

        public async Task<BuildResult> callAsync()
        {
            return await AsyncDependencies.@using()
                .addStep(authenticatePushStep)
                .addSteps(await pushBaseImageLayersStep.getFuture())
                .addSteps(await pushApplicationLayersStep.getFuture())
                .addStep(await pushContainerConfigurationStep.getFuture())
                .addStep(await buildImageStep.getFuture())
                .whenAllSucceedAsync(this.afterPushStepsAsync);
        }

        private async Task<BuildResult> afterPushStepsAsync()
        {
            AsyncDependencies dependencies = AsyncDependencies.@using();
            foreach (AsyncStep<PushBlobStep> pushBaseImageLayerStep in await pushBaseImageLayersStep.getFuture())
            {
                dependencies.addStep(await pushBaseImageLayerStep.getFuture());
            }
            foreach (AsyncStep<PushBlobStep> pushApplicationLayerStep in await pushApplicationLayersStep.getFuture())
            {
                dependencies.addStep(await pushApplicationLayerStep.getFuture());
            }
            return await dependencies
                .addStep(await (await pushContainerConfigurationStep.getFuture()).getFuture())
                .whenAllSucceedAsync(this.afterAllPushedAsync);
        }

        private async Task<BuildResult> afterAllPushedAsync()
        {
            ImmutableHashSet<string> targetImageTags = buildConfiguration.getAllTargetImageTags();
            ProgressEventDispatcher progressEventDispatcher =
                progressEventDispatcherFactory.create("pushing image manifest", targetImageTags.size());

            using (TimerEventDispatcher ignored =
                new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION))
            {
                RegistryClient registryClient =
                    buildConfiguration
                        .newTargetImageRegistryClientFactory()
                        .setAuthorization(await authenticatePushStep.getFuture())
                        .newRegistryClient();

                // Constructs the image.
                ImageToJsonTranslator imageToJsonTranslator =
                    new ImageToJsonTranslator(await (await buildImageStep.getFuture()).getFuture());

                // Gets the image manifest to push.
                BlobDescriptor containerConfigurationBlobDescriptor =
                    await (await (await pushContainerConfigurationStep.getFuture()).getFuture()).getFuture();
                BuildableManifestTemplate manifestTemplate =
                    imageToJsonTranslator.getManifestTemplate(
                        buildConfiguration.getTargetFormat(), containerConfigurationBlobDescriptor);

                // Pushes to all target image tags.
                IList<Task<DescriptorDigest>> pushAllTagsFutures = new List<Task<DescriptorDigest>>();
                foreach (string tag in targetImageTags)
                {
                    ProgressEventDispatcher.Factory progressEventDispatcherFactory =
                        progressEventDispatcher.newChildProducer();
                    using (progressEventDispatcherFactory.create("tagging with " + tag, 1))
                    {
                        buildConfiguration.getEventHandlers().dispatch(LogEvent.info("Tagging with " + tag + "..."));
                        pushAllTagsFutures.add(registryClient.pushManifestAsync(manifestTemplate, tag));
                    }
                }

                DescriptorDigest imageDigest = Digests.computeJsonDigest(manifestTemplate);
                DescriptorDigest imageId = containerConfigurationBlobDescriptor.getDigest();
                BuildResult result = new BuildResult(imageDigest, imageId);

                return await Futures.whenAllSucceed(pushAllTagsFutures)
                    .callAsync(
                        () =>
                        {
                            progressEventDispatcher.close();
                            return result;
                        });
            }
        }
    }
}
