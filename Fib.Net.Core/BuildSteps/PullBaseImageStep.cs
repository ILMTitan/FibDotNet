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

using Fib.Net.Core.Api;
using Fib.Net.Core.Async;
using Fib.Net.Core.Blob;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Events;
using Fib.Net.Core.Events.Progress;
using Fib.Net.Core.Events.Time;
using Fib.Net.Core.Http;
using Fib.Net.Core.Images;
using Fib.Net.Core.Images.Json;
using Fib.Net.Core.Json;
using Fib.Net.Core.Registry;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using static Fib.Net.Core.BuildSteps.PullBaseImageStep;

namespace Fib.Net.Core.BuildSteps
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

            public Image GetBaseImage()
            {
                return baseImage;
            }

            public Authorization GetBaseImageAuthorization()
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
            listenableFuture = Task.Run(CallAsync);
        }

        public Task<BaseImageWithAuthorization> GetFuture()
        {
            return listenableFuture;
        }

        public async Task<BaseImageWithAuthorization> CallAsync()
        {
            IEventHandlers eventHandlers = buildConfiguration.GetEventHandlers();
            // Skip this step if this is a scratch image
            ImageConfiguration baseImageConfiguration = buildConfiguration.GetBaseImageConfiguration();
            string description = string.Format(
                CultureInfo.CurrentCulture,
                Resources.PullBaseImageStepDescriptionFormat,
                buildConfiguration.GetBaseImageConfiguration().GetImage());
            eventHandlers.Dispatch(LogEvent.Progress(description));
            if (baseImageConfiguration.GetImage().IsScratch())
            {
                return new BaseImageWithAuthorization(
                    Image.CreateBuilder(buildConfiguration.GetTargetFormat()).Build(), null);
            }

            if (buildConfiguration.IsOffline())
            {
                return new BaseImageWithAuthorization(PullBaseImageOffline(), null);
            }

            using (ProgressEventDispatcher progressEventDispatcher = progressEventDispatcherFactory.Create(description, 2))
            using (new TimerEventDispatcher(buildConfiguration.GetEventHandlers(), description))

            {
                // First, try with no credentials.
                try
                {
                    return new BaseImageWithAuthorization(await PullBaseImageAsync(null, progressEventDispatcher).ConfigureAwait(false), null);
                }
                catch (RegistryUnauthorizedException)
                {
                    eventHandlers.Dispatch(
                        LogEvent.Lifecycle(
                            "The base image requires auth. Trying again for "
                                + buildConfiguration.GetBaseImageConfiguration().GetImage()
                                + "..."));

                    // If failed, then, retrieve base registry credentials and try with retrieved credentials.
                    // TODO: Refactor the logic in RetrieveRegistryCredentialsStep out to
                    // registry.credentials.RegistryCredentialsRetriever to avoid this direct executor hack.
                    RetrieveRegistryCredentialsStep retrieveBaseRegistryCredentialsStep =
                        RetrieveRegistryCredentialsStep.ForBaseImage(
                            buildConfiguration,
                            progressEventDispatcher.NewChildProducer());

                    Credential registryCredential = await retrieveBaseRegistryCredentialsStep.GetFuture().ConfigureAwait(false);
                    Authorization registryAuthorization =
                        registryCredential?.IsOAuth2RefreshToken() != false
                            ? null
                            : Authorization.FromBasicCredentials(
                                registryCredential.GetUsername(), registryCredential.GetPassword());

                    try
                    {
                        return new BaseImageWithAuthorization(
                            await PullBaseImageAsync(registryAuthorization, progressEventDispatcher).ConfigureAwait(false), registryAuthorization);
                    }
                    catch (RegistryUnauthorizedException)
                    {
                        // The registry requires us to authenticate using the Docker Token Authentication.
                        // See https://docs.docker.com/registry/spec/auth/token
                        try
                        {
                            RegistryAuthenticator registryAuthenticator =
                                await buildConfiguration
                                    .NewBaseImageRegistryClientFactory()
                                    .NewRegistryClient()
                                    .GetRegistryAuthenticatorAsync().ConfigureAwait(false);
                            if (registryAuthenticator != null)
                            {
                                Authorization pullAuthorization =
                                    await registryAuthenticator.AuthenticatePullAsync(registryCredential).ConfigureAwait(false);

                                return new BaseImageWithAuthorization(
                                    await PullBaseImageAsync(pullAuthorization, progressEventDispatcher).ConfigureAwait(false), pullAuthorization);
                            }
                        }
                        catch (InsecureRegistryException)
                        {
                            // Cannot skip certificate validation or use HTTP; fall through.
                        }
                        eventHandlers.Dispatch(LogEvent.Error(Resources.PullBaseImageStepAuthenticationErrorMessage));
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
        private async Task<Image> PullBaseImageAsync(
            Authorization registryAuthorization,
            ProgressEventDispatcher progressEventDispatcher)
        {
            RegistryClient registryClient =
                buildConfiguration
                    .NewBaseImageRegistryClientFactory()
                    .SetAuthorization(registryAuthorization)
                    .NewRegistryClient();

            IManifestTemplate manifestTemplate =
                await registryClient.PullManifestAsync(buildConfiguration.GetBaseImageConfiguration().GetImageTag()).ConfigureAwait(false);

            // TODO: Make schema version be enum.
            switch (manifestTemplate.SchemaVersion)
            {
                case 1:
                    V21ManifestTemplate v21ManifestTemplate = (V21ManifestTemplate)manifestTemplate;
                    await buildConfiguration
                        .GetBaseImageLayersCache()
                        .WriteMetadataAsync(
                            buildConfiguration.GetBaseImageConfiguration().GetImage(), v21ManifestTemplate).ConfigureAwait(false);
                    return JsonToImageTranslator.ToImage(v21ManifestTemplate);

                case 2:
                    IBuildableManifestTemplate buildableManifestTemplate =
                        (IBuildableManifestTemplate)manifestTemplate;
                    if (buildableManifestTemplate.GetContainerConfiguration() == null
                        || buildableManifestTemplate.GetContainerConfiguration().Digest == null)
                    {
                        throw new UnknownManifestFormatException(
                            "Invalid container configuration in Docker V2.2/OCI manifest: \n"
                                + JsonTemplateMapper.ToUtf8String(buildableManifestTemplate));
                    }

                    DescriptorDigest containerConfigurationDigest =
                        buildableManifestTemplate.GetContainerConfiguration().Digest;

                    using (ThrottledProgressEventDispatcherWrapper progressEventDispatcherWrapper =
                        new ThrottledProgressEventDispatcherWrapper(
                            progressEventDispatcher.NewChildProducer(),
                            "pull container configuration " + containerConfigurationDigest))
                    {
                        string containerConfigurationString =
                            await Blobs.WriteToStringAsync(
                                registryClient.PullBlob(
                                    containerConfigurationDigest,
                                    progressEventDispatcherWrapper.SetProgressTarget,
                                    progressEventDispatcherWrapper.DispatchProgress)).ConfigureAwait(false);

                        ContainerConfigurationTemplate containerConfigurationTemplate =
                            JsonTemplateMapper.ReadJson<ContainerConfigurationTemplate>(
                                containerConfigurationString);
                        await buildConfiguration
                            .GetBaseImageLayersCache()
                            .WriteMetadataAsync(
                                buildConfiguration.GetBaseImageConfiguration().GetImage(),
                                buildableManifestTemplate,
                                containerConfigurationTemplate).ConfigureAwait(false);
                        return JsonToImageTranslator.ToImage(
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
        private Image PullBaseImageOffline()
        {
            IImageReference baseImage = buildConfiguration.GetBaseImageConfiguration().GetImage();
            Maybe<ManifestAndConfig> metadata =
                buildConfiguration.GetBaseImageLayersCache().RetrieveMetadata(baseImage);
            if (!metadata.IsPresent())
            {
                throw new IOException(
                    "Cannot run Fib in offline mode; " + baseImage + " not found in local Fib cache");
            }

            IManifestTemplate manifestTemplate = metadata.Get().GetManifest();
            if (manifestTemplate is V21ManifestTemplate v21ManifestTemplate)
            {
                return JsonToImageTranslator.ToImage(v21ManifestTemplate);
            }

            ContainerConfigurationTemplate configurationTemplate =
                metadata.Get().GetConfig().OrElseThrow(() => new InvalidOperationException());
            return JsonToImageTranslator.ToImage(
                (IBuildableManifestTemplate)manifestTemplate, configurationTemplate);
        }
    }
}
