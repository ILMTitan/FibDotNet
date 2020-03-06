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
using Fib.Net.Core.Caching;
using Fib.Net.Core.Events;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Registry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http.Headers;

namespace Fib.Net.Core.Configuration
{
    /** Immutable configuration options for the builder process. */
    public sealed class BuildConfiguration : IBuildConfiguration
    {
        /** The default target format of the container manifest. */
        private const ManifestFormat DEFAULT_TARGET_FORMAT = ManifestFormat.V22;

        /** Builds an immutable {@link BuildConfiguration}. Instantiate with {@link #builder}. */
        public class Builder
        {
            // All the parameters below are set to their default values.
            private ImageConfiguration baseImageConfiguration;
            private ImageConfiguration targetImageConfiguration;
            private ImmutableHashSet<string> additionalTargetImageTags = ImmutableHashSet.Create<string>();
            private ContainerConfiguration containerConfiguration;
            private SystemPath applicationLayersCacheDirectory;
            private SystemPath baseImageLayersCacheDirectory;
            private bool allowInsecureRegistries = false;
            private bool offline = false;
            private ImmutableArray<ILayerConfiguration> layerConfigurations = ImmutableArray.Create<ILayerConfiguration>();
            private ManifestFormat targetFormat = DEFAULT_TARGET_FORMAT;
            private string toolName = null;
            private string toolVersion = null;
            private IEventHandlers eventHandlers = EventHandlers.NONE;

            public Builder() { }

            /**
             * Sets the base image configuration.
             *
             * @param imageConfiguration the {@link ImageConfiguration} describing the base image
             * @return this
             */
            public Builder SetBaseImageConfiguration(ImageConfiguration imageConfiguration)
            {
                baseImageConfiguration = imageConfiguration;
                return this;
            }

            /**
             * Sets the target image configuration.
             *
             * @param imageConfiguration the {@link ImageConfiguration} describing the target image
             * @return this
             */
            public Builder SetTargetImageConfiguration(ImageConfiguration imageConfiguration)
            {
                targetImageConfiguration = imageConfiguration;
                return this;
            }

            /**
             * Sets the tags to tag the target image with (in addition to the tag in the target image
             * configuration image reference set via {@link #setTargetImageConfiguration}).
             *
             * @param tags a set of tags
             * @return this
             */
            public Builder SetAdditionalTargetImageTags(ISet<string> tags)
            {
                additionalTargetImageTags = ImmutableHashSet.CreateRange(tags ?? Enumerable.Empty<string>());
                return this;
            }

            /**
             * Sets configuration parameters for the container.
             *
             * @param containerConfiguration the {@link ContainerConfiguration}
             * @return this
             */
            public Builder SetContainerConfiguration(ContainerConfiguration containerConfiguration)
            {
                this.containerConfiguration = containerConfiguration;
                return this;
            }

            /**
             * Sets the location of the cache for storing application layers.
             *
             * @param applicationLayersCacheDirectory the application layers cache directory
             * @return this
             */
            public Builder SetApplicationLayersCacheDirectory(SystemPath applicationLayersCacheDirectory)
            {
                this.applicationLayersCacheDirectory = applicationLayersCacheDirectory;
                return this;
            }

            /**
             * Sets the location of the cache for storing base image layers.
             *
             * @param baseImageLayersCacheDirectory the base image layers cache directory
             * @return this
             */
            public Builder SetBaseImageLayersCacheDirectory(SystemPath baseImageLayersCacheDirectory)
            {
                this.baseImageLayersCacheDirectory = baseImageLayersCacheDirectory;
                return this;
            }

            /**
             * Sets the target format of the container image.
             *
             * @param targetFormat the target format
             * @return this
             */
            public Builder SetTargetFormat(ImageFormat targetFormat)
            {
                this.targetFormat =
                    targetFormat == ImageFormat.Docker
                        ? ManifestFormat.V22
                        : ManifestFormat.OCI;
                return this;
            }

            /**
             * Sets whether or not to allow communication over HTTP (as opposed to HTTPS).
             *
             * @param allowInsecureRegistries if {@code true}, insecure connections will be allowed
             * @return this
             */
            public Builder SetAllowInsecureRegistries(bool allowInsecureRegistries)
            {
                this.allowInsecureRegistries = allowInsecureRegistries;
                return this;
            }

            /**
             * Sets whether or not to perform the build in offline mode.
             *
             * @param offline if {@code true}, the build will run in offline mode
             * @return this
             */
            public Builder SetOffline(bool offline)
            {
                this.offline = offline;
                return this;
            }

            /**
             * Sets the layers to build.
             *
             * @param layerConfigurations the configurations for the layers
             * @return this
             */
            public Builder SetLayerConfigurations(IList<ILayerConfiguration> layerConfigurations)
            {
                this.layerConfigurations = ImmutableArray.CreateRange(layerConfigurations);
                return this;
            }

            /**
             * Sets the name of the tool that is executing the build.
             *
             * @param toolName the tool name
             * @return this
             */
            public Builder SetToolName(string toolName)
            {
                this.toolName = toolName;
                return this;
            }

            /**
             * Sets the version of the tool that is executing the build.
             *
             * @param toolName the tool name
             * @return this
             */
            public Builder SetToolVersion(string toolVersion)
            {
                this.toolVersion = toolVersion;
                return this;
            }

            /**
             * Sets the {@link EventHandlers} to dispatch events with.
             *
             * @param eventHandlers the {@link EventHandlers}
             * @return this
             */
            public Builder SetEventHandlers(IEventHandlers eventHandlers)
            {
                this.eventHandlers = eventHandlers;
                return this;
            }

            /**
             * Builds a new {@link BuildConfiguration} using the parameters passed into the builder.
             *
             * @return the corresponding build configuration
             * @throws IOException if an I/O exception occurs
             */
            public BuildConfiguration Build()
            {
                // Validates the parameters.
                IList<string> missingFields = new List<string>();
                if (baseImageConfiguration == null)
                {
                    missingFields.Add("base image configuration");
                }
                if (targetImageConfiguration == null)
                {
                    missingFields.Add("target image configuration");
                }
                if (baseImageLayersCacheDirectory == null)
                {
                    missingFields.Add("base image layers cache directory");
                }
                if (applicationLayersCacheDirectory == null)
                {
                    missingFields.Add("application layers cache directory");
                }

                switch (missingFields.Count)
                {
                    case 0: // No errors
                        if (Preconditions.CheckNotNull(baseImageConfiguration).GetImage().UsesDefaultTag())
                        {
                            eventHandlers.Dispatch(
                                LogEvent.Warn(
                                    "Base image '"
                                        + baseImageConfiguration.GetImage()
                                        + "' does not use a specific image digest - build may not be reproducible"));
                        }

                        return new BuildConfiguration(
                            baseImageConfiguration,
                            Preconditions.CheckNotNull(targetImageConfiguration),
                            additionalTargetImageTags,
                            containerConfiguration,
                            LayersCache.WithDirectory(Preconditions.CheckNotNull(baseImageLayersCacheDirectory)),
                            LayersCache.WithDirectory(Preconditions.CheckNotNull(applicationLayersCacheDirectory)),
                            targetFormat,
                            allowInsecureRegistries,
                            offline,
                            layerConfigurations,
                            toolName,
                            toolVersion,
                            eventHandlers);

                    case 1:
                        throw new InvalidOperationException("Required field is not set: " + missingFields[0]);
                    default:
                        throw new InvalidOperationException("Required fields are not set: " + string.Join(", ", missingFields));
                }
            }

            public SystemPath GetBaseImageLayersCacheDirectory()
            {
                return baseImageLayersCacheDirectory;
            }

            public SystemPath GetApplicationLayersCacheDirectory()
            {
                return applicationLayersCacheDirectory;
            }
        }

        /**
         * Creates a new {@link Builder} to build a {@link BuildConfiguration}.
         *
         * @return a new {@link Builder}
         */
        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        private readonly ImageConfiguration baseImageConfiguration;
        private readonly ImageConfiguration targetImageConfiguration;
        private readonly ImmutableHashSet<string> additionalTargetImageTags;
        private readonly ContainerConfiguration containerConfiguration;
        private readonly LayersCache baseImageLayersCache;
        private readonly LayersCache applicationLayersCache;
        private readonly ManifestFormat targetFormat;
        private readonly bool allowInsecureRegistries;
        private readonly bool offline;
        private readonly ImmutableArray<ILayerConfiguration> layerConfigurations;
        private readonly string toolName;
        private readonly string toolVersion;
        private readonly IEventHandlers eventHandlers;

        /** Instantiate with {@link #builder}. */
        private BuildConfiguration(
            ImageConfiguration baseImageConfiguration,
            ImageConfiguration targetImageConfiguration,
            ImmutableHashSet<string> additionalTargetImageTags,
            ContainerConfiguration containerConfiguration,
            LayersCache baseImageLayersCache,
            LayersCache applicationLayersCache,
            ManifestFormat targetFormat,
            bool allowInsecureRegistries,
            bool offline,
            ImmutableArray<ILayerConfiguration> layerConfigurations,
            string toolName,
            string toolVersion,
            IEventHandlers eventHandlers)
        {
            this.baseImageConfiguration = baseImageConfiguration;
            this.targetImageConfiguration = targetImageConfiguration;
            this.additionalTargetImageTags = additionalTargetImageTags;
            this.containerConfiguration = containerConfiguration;
            this.baseImageLayersCache = baseImageLayersCache;
            this.applicationLayersCache = applicationLayersCache;
            this.targetFormat = targetFormat;
            this.allowInsecureRegistries = allowInsecureRegistries;
            this.offline = offline;
            this.layerConfigurations = layerConfigurations;
            this.toolName = toolName;
            this.toolVersion = toolVersion;
            this.eventHandlers = eventHandlers;
        }

        public ImageConfiguration GetBaseImageConfiguration()
        {
            return baseImageConfiguration;
        }

        public ImageConfiguration GetTargetImageConfiguration()
        {
            return targetImageConfiguration;
        }

        public ImmutableHashSet<string> GetAllTargetImageTags()
        {
            ImmutableHashSet<string>.Builder allTargetImageTags = ImmutableHashSet.CreateBuilder<string>();
            allTargetImageTags.Add(targetImageConfiguration.GetImageTag());
            allTargetImageTags.UnionWith(additionalTargetImageTags);
            return allTargetImageTags.ToImmutable();
        }

        public IContainerConfiguration GetContainerConfiguration()
        {
            return containerConfiguration;
        }

        public ManifestFormat GetTargetFormat()
        {
            return targetFormat;
        }

        public string GetToolName()
        {
            return toolName;
        }

        public string GetToolVersion()
        {
            return toolVersion;
        }

        public IEventHandlers GetEventHandlers()
        {
            return eventHandlers;
        }

        /**
         * Gets the {@link Cache} for base image layers.
         *
         * @return the {@link Cache} for base image layers
         */
        public LayersCache GetBaseImageLayersCache()
        {
            return baseImageLayersCache;
        }

        /**
         * Gets the {@link Cache} for application layers.
         *
         * @return the {@link Cache} for application layers
         */
        public LayersCache GetApplicationLayersCache()
        {
            return applicationLayersCache;
        }

        /**
         * Gets whether or not to allow insecure registries (ignoring certificate validation failure or
         * communicating over HTTP if all else fail).
         *
         * @return {@code true} if insecure connections will be allowed; {@code false} otherwise
         */
        public bool GetAllowInsecureRegistries()
        {
            return allowInsecureRegistries;
        }

        /**
         * Gets whether or not to run the build in offline mode.
         *
         * @return {@code true} if the build will run in offline mode; {@code false} otherwise
         */
        public bool IsOffline()
        {
            return offline;
        }

        /**
         * Gets the configurations for building the layers.
         *
         * @return the list of layer configurations
         */
        public ImmutableArray<ILayerConfiguration> GetLayerConfigurations()
        {
            return layerConfigurations;
        }

        /**
         * Creates a new {@link RegistryClient.Factory} for the base image with fields from the build
         * configuration.
         *
         * @return a new {@link RegistryClient.Factory}
         */
        public RegistryClient.Factory NewBaseImageRegistryClientFactory()
        {
            return NewRegistryClientFactory(baseImageConfiguration);
        }

        /**
         * Creates a new {@link RegistryClient.Factory} for the target image with fields from the build
         * configuration.
         *
         * @return a new {@link RegistryClient.Factory}
         */
        public RegistryClient.Factory NewTargetImageRegistryClientFactory()
        {
            return NewRegistryClientFactory(targetImageConfiguration);
        }

        private RegistryClient.Factory NewRegistryClientFactory(ImageConfiguration imageConfiguration)
        {
            RegistryClient.Factory factory = RegistryClient.CreateFactory(
                    GetEventHandlers(),
                    imageConfiguration.GetImageRegistry(),
                    imageConfiguration.GetImageRepository())
                .SetAllowInsecureRegistries(GetAllowInsecureRegistries());
            if (GetToolName() != null)
            {
                factory.AddUserAgentValue(new ProductInfoHeaderValue(GetToolName(), GetToolVersion()));
            }
            return factory;
        }
    }
}
