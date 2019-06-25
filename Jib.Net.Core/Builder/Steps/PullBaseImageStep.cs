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
using com.google.cloud.tools.jib.builder;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.image.json;
using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry;
using Jib.Net.Core.Api;
using Jib.Net.Core.Images;
using Jib.Net.Core.Images.Json;
using Jib.Net.Core.Registry;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using static Jib.Net.Core.Builder.Steps.PullBaseImageStep;

namespace Jib.Net.Core.Builder.Steps
{
    /** Pulls the base image manifest. */
    public class PullBaseImageStep : IAsyncStep<BaseImageWithAuthorization>
    {
        /** Structure for the result returned by this step. */
        public class BaseImageWithAuthorization
        {
            private readonly Image baseImage;
            private readonly Authorization baseImageAuthorization;

            public BaseImageWithAuthorization(Image baseImage, Authorization baseImageAuthorization)
            {
                this.baseImage = baseImage;
                this.baseImageAuthorization = baseImageAuthorization;
            }

            public Image getBaseImage()
            {
                return baseImage;
            }

            public Authorization getBaseImageAuthorization()
            {
                return baseImageAuthorization;
            }
        }

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly Task<BaseImageWithAuthorization> listenableFuture;

        public PullBaseImageStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            listenableFuture = Task.Run(callAsync);
        }

        public Task<BaseImageWithAuthorization> getFuture()
        {
            return listenableFuture;
        }

        public async Task<BaseImageWithAuthorization> callAsync()
        {
            IEventHandlers eventHandlers = buildConfiguration.getEventHandlers();
            // Skip this step if this is a scratch image
            ImageConfiguration baseImageConfiguration = buildConfiguration.getBaseImageConfiguration();
            string description = string.Format(
                CultureInfo.CurrentCulture,
                Resources.PullBaseImageStepDescriptionFormat,
                buildConfiguration.getBaseImageConfiguration().getImage());
            eventHandlers.dispatch(LogEvent.progress(description));
            if (baseImageConfiguration.getImage().isScratch())
            {
                return new BaseImageWithAuthorization(
                    Image.builder(buildConfiguration.getTargetFormat()).build(), null);
            }

            if (buildConfiguration.isOffline())
            {
                return new BaseImageWithAuthorization(pullBaseImageOffline(), null);
            }

            using (ProgressEventDispatcher progressEventDispatcher = progressEventDispatcherFactory.create(description, 2))
            using (new TimerEventDispatcher(buildConfiguration.getEventHandlers(), description))

            {
                // First, try with no credentials.
                try
                {
                    return new BaseImageWithAuthorization(await pullBaseImageAsync(null, progressEventDispatcher).ConfigureAwait(false), null);
                }
                catch (RegistryUnauthorizedException)
                {
                    eventHandlers.dispatch(
                        LogEvent.lifecycle(
                            "The base image requires auth. Trying again for "
                                + buildConfiguration.getBaseImageConfiguration().getImage()
                                + "..."));

                    // If failed, then, retrieve base registry credentials and try with retrieved credentials.
                    // TODO: Refactor the logic in RetrieveRegistryCredentialsStep out to
                    // registry.credentials.RegistryCredentialsRetriever to avoid this direct executor hack.
                    RetrieveRegistryCredentialsStep retrieveBaseRegistryCredentialsStep =
                        RetrieveRegistryCredentialsStep.forBaseImage(
                            buildConfiguration,
                            progressEventDispatcher.newChildProducer());

                    Credential registryCredential = await retrieveBaseRegistryCredentialsStep.getFuture().ConfigureAwait(false);
                    Authorization registryAuthorization =
                        registryCredential?.isOAuth2RefreshToken() != false
                            ? null
                            : Authorization.fromBasicCredentials(
                                registryCredential.getUsername(), registryCredential.getPassword());

                    try
                    {
                        return new BaseImageWithAuthorization(
                            await pullBaseImageAsync(registryAuthorization, progressEventDispatcher).ConfigureAwait(false), registryAuthorization);
                    }
                    catch (RegistryUnauthorizedException)
                    {
                        // The registry requires us to authenticate using the Docker Token Authentication.
                        // See https://docs.docker.com/registry/spec/auth/token
                        try
                        {
                            RegistryAuthenticator registryAuthenticator =
                                await buildConfiguration
                                    .newBaseImageRegistryClientFactory()
                                    .newRegistryClient()
                                    .getRegistryAuthenticatorAsync().ConfigureAwait(false);
                            if (registryAuthenticator != null)
                            {
                                Authorization pullAuthorization =
                                    await registryAuthenticator.authenticatePullAsync(registryCredential).ConfigureAwait(false);

                                return new BaseImageWithAuthorization(
                                    await pullBaseImageAsync(pullAuthorization, progressEventDispatcher).ConfigureAwait(false), pullAuthorization);
                            }
                        }
                        catch (InsecureRegistryException)
                        {
                            // Cannot skip certificate validation or use HTTP; fall through.
                        }
                        eventHandlers.dispatch(LogEvent.error(Resources.PullBaseImageStepAuthenticationErrorMessage));
                        throw;
                    }
                }
            }
        }

        /**
         * Pulls the base image.
         *
         * @param registryAuthorization authentication credentials to possibly use
         * @param progressEventDispatcher the {@link ProgressEventDispatcher} for emitting {@link
         *     ProgressEvent}s
         * @return the pulled image
         * @throws IOException when an I/O exception occurs during the pulling
         * @throws RegistryException if communicating with the registry caused a known error
         * @throws LayerCountMismatchException if the manifest and configuration contain conflicting layer
         *     information
         * @throws LayerPropertyNotFoundException if adding image layers fails
         * @throws BadContainerConfigurationFormatException if the container configuration is in a bad
         *     format
         */
        private async Task<Image> pullBaseImageAsync(
            Authorization registryAuthorization,
            ProgressEventDispatcher progressEventDispatcher)
        {
            RegistryClient registryClient =
                buildConfiguration
                    .newBaseImageRegistryClientFactory()
                    .setAuthorization(registryAuthorization)
                    .newRegistryClient();

            IManifestTemplate manifestTemplate =
                await registryClient.pullManifestAsync(buildConfiguration.getBaseImageConfiguration().getImageTag()).ConfigureAwait(false);

            // TODO: Make schema version be enum.
            switch (manifestTemplate.getSchemaVersion())
            {
                case 1:
                    V21ManifestTemplate v21ManifestTemplate = (V21ManifestTemplate)manifestTemplate;
                    await buildConfiguration
                        .getBaseImageLayersCache()
                        .writeMetadataAsync(
                            buildConfiguration.getBaseImageConfiguration().getImage(), v21ManifestTemplate).ConfigureAwait(false);
                    return JsonToImageTranslator.toImage(v21ManifestTemplate);

                case 2:
                    IBuildableManifestTemplate buildableManifestTemplate =
                        (IBuildableManifestTemplate)manifestTemplate;
                    if (buildableManifestTemplate.getContainerConfiguration() == null
                        || buildableManifestTemplate.getContainerConfiguration().getDigest() == null)
                    {
                        throw new UnknownManifestFormatException(
                            "Invalid container configuration in Docker V2.2/OCI manifest: \n"
                                + JsonTemplateMapper.toUtf8String(buildableManifestTemplate));
                    }

                    DescriptorDigest containerConfigurationDigest =
                        buildableManifestTemplate.getContainerConfiguration().getDigest();

                    using (ThrottledProgressEventDispatcherWrapper progressEventDispatcherWrapper =
                        new ThrottledProgressEventDispatcherWrapper(
                            progressEventDispatcher.newChildProducer(),
                            "pull container configuration " + containerConfigurationDigest))
                    {
                        string containerConfigurationString =
                            await Blobs.writeToStringAsync(
                                registryClient.pullBlob(
                                    containerConfigurationDigest,
                                    progressEventDispatcherWrapper.setProgressTarget,
                                    progressEventDispatcherWrapper.dispatchProgress)).ConfigureAwait(false);

                        ContainerConfigurationTemplate containerConfigurationTemplate =
                            JsonTemplateMapper.readJson<ContainerConfigurationTemplate>(
                                containerConfigurationString);
                        await buildConfiguration
                            .getBaseImageLayersCache()
                            .writeMetadataAsync(
                                buildConfiguration.getBaseImageConfiguration().getImage(),
                                buildableManifestTemplate,
                                containerConfigurationTemplate).ConfigureAwait(false);
                        return JsonToImageTranslator.toImage(
                            buildableManifestTemplate, containerConfigurationTemplate);
                    }
            }

            throw new InvalidOperationException(Resources.PullBaseImageStepUnknownManifestErrorMessage);
        }

        /**
         * Retrieves the cached base image.
         *
         * @return the cached image
         * @throws IOException when an I/O exception occurs
         * @throws CacheCorruptedException if the cache is corrupted
         * @throws LayerPropertyNotFoundException if adding image layers fails
         * @throws BadContainerConfigurationFormatException if the container configuration is in a bad
         *     format
         */
        private Image pullBaseImageOffline()
        {
            IImageReference baseImage = buildConfiguration.getBaseImageConfiguration().getImage();
            Optional<ManifestAndConfig> metadata =
                buildConfiguration.getBaseImageLayersCache().retrieveMetadata(baseImage);
            if (!metadata.isPresent())
            {
                throw new IOException(
                    "Cannot run Jib in offline mode; " + baseImage + " not found in local Jib cache");
            }

            IManifestTemplate manifestTemplate = metadata.get().getManifest();
            if (manifestTemplate is V21ManifestTemplate v21ManifestTemplate)
            {
                return JsonToImageTranslator.toImage(v21ManifestTemplate);
            }

            ContainerConfigurationTemplate configurationTemplate =
                metadata.get().getConfig().orElseThrow(() => new InvalidOperationException());
            return JsonToImageTranslator.toImage(
                (IBuildableManifestTemplate)manifestTemplate, configurationTemplate);
        }
    }
}
