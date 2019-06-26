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
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images.Json;
using Jib.Net.Core.Registry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{
    /** Pushes the final image. Outputs the pushed image digest. */
    internal class PushImageStep : IAsyncStep<BuildResult>
    {
        private const string DESCRIPTION = "Pushing new image";

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

            listenableFuture = CallAsync();
        }

        public Task<BuildResult> GetFuture()
        {
            return listenableFuture;
        }

        public async Task<BuildResult> CallAsync()
        {
            IReadOnlyList<BlobDescriptor> baseImageDescriptors = await pushBaseImageLayersStep.GetFuture().ConfigureAwait(false);
            IReadOnlyList<BlobDescriptor> appLayerDescriptors = await pushApplicationLayersStep.GetFuture().ConfigureAwait(false);
            BlobDescriptor containerConfigurationBlobDescriptor = await pushContainerConfigurationStep.GetFuture().ConfigureAwait(false);
            ImmutableHashSet<string> targetImageTags = buildConfiguration.GetAllTargetImageTags();

            using (ProgressEventDispatcher progressEventDispatcher =
                progressEventDispatcherFactory.Create("pushing image manifest", targetImageTags.Count))
            using (TimerEventDispatcher ignored =
                new TimerEventDispatcher(buildConfiguration.GetEventHandlers(), DESCRIPTION))
            {
                RegistryClient registryClient =
                    buildConfiguration
                        .NewTargetImageRegistryClientFactory()
                        .SetAuthorization(await authenticatePushStep.GetFuture().ConfigureAwait(false))
                        .NewRegistryClient();

                // Constructs the image.
                ImageToJsonTranslator imageToJsonTranslator =
                    new ImageToJsonTranslator(await buildImageStep.GetFuture().ConfigureAwait(false));

                // Gets the image manifest to push.
                IBuildableManifestTemplate manifestTemplate =
                    imageToJsonTranslator.GetManifestTemplate(
                        buildConfiguration.GetTargetFormat(), containerConfigurationBlobDescriptor);

                // Pushes to all target image tags.
                IList<Task<DescriptorDigest>> pushAllTagsFutures = new List<Task<DescriptorDigest>>();
                foreach (string tag in targetImageTags)
                {
                    ProgressEventDispatcher.Factory progressEventDispatcherFactory =
                        progressEventDispatcher.NewChildProducer();
                    using (progressEventDispatcherFactory.Create("tagging with " + tag, 1))
                    {
                        buildConfiguration.GetEventHandlers().Dispatch(LogEvent.Info("Tagging with " + tag + "..."));
                        JavaExtensions.Add(pushAllTagsFutures, registryClient.PushManifestAsync(manifestTemplate, tag));
                    }
                }

                DescriptorDigest imageDigest =
                    await Digests.ComputeJsonDigestAsync(manifestTemplate).ConfigureAwait(false);
                DescriptorDigest imageId = containerConfigurationBlobDescriptor.GetDigest();
                BuildResult result = new BuildResult(imageDigest, imageId);

                await Task.WhenAll(pushAllTagsFutures).ConfigureAwait(false);
                return result;
            }
        }
    }
}
