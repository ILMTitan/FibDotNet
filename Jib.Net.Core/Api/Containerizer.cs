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
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.filesystem;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Jib.Net.Core.Api
{
    /** Configures how to containerize. */
    public sealed class Containerizer : IDisposable, IContainerizer
    {
        /**
         * The default directory for caching the base image layers, in {@code [user cache
         * home]/google-cloud-tools-java/jib}.
         */
        public static readonly SystemPath DefaultBaseCacheDirectory =
            UserCacheHome.GetCacheHome(SystemEnvironment.Instance).Resolve("google-cloud-tools-java").Resolve("jib");

        private const string DEFAULT_TOOL_NAME = "jib-core";

        private const string DESCRIPTION_FOR_DOCKER_REGISTRY = "Building and pushing image";
        private const string DESCRIPTION_FOR_DOCKER_DAEMON = "Building image to Docker daemon";
        private const string DESCRIPTION_FOR_TARBALL = "Building image tarball";

        /**
         * Gets a new {@link Containerizer} that containerizes to a container registry.
         *
         * @param registryImage the {@link RegistryImage} that defines target container registry and
         *     credentials
         * @return a new {@link Containerizer}
         */
        public static Containerizer To(RegistryImage registryImage)
        {
            registryImage = registryImage ?? throw new ArgumentNullException(nameof(registryImage));
            ImageConfiguration imageConfiguration =
                ImageConfiguration.CreateBuilder(registryImage.GetImageReference())
                    .SetCredentialRetrievers(registryImage.GetCredentialRetrievers())
                    .Build();

            return new Containerizer(
                DESCRIPTION_FOR_DOCKER_REGISTRY, imageConfiguration, stepsRunnerFactory, true);

            StepsRunner stepsRunnerFactory(BuildConfiguration buildConfiguration) =>
                    StepsRunner.Begin(buildConfiguration)
                        .RetrieveTargetRegistryCredentials()
                        .AuthenticatePush()
                        .PullBaseImage()
                        .PullAndCacheBaseImageLayers()
                        .PushBaseImageLayers()
                        .BuildAndCacheApplicationLayers()
                        .BuildImage()
                        .PushContainerConfiguration()
                        .PushApplicationLayers()
                        .PushImage();
        }

        /**
         * Gets a new {@link Containerizer} that containerizes to a Docker daemon.
         *
         * @param dockerDaemonImage the {@link DockerDaemonImage} that defines target Docker daemon
         * @return a new {@link Containerizer}
         */
        public static Containerizer To(DockerDaemonImage dockerDaemonImage)
        {
            dockerDaemonImage = dockerDaemonImage ?? throw new ArgumentNullException(nameof(dockerDaemonImage));
            ImageConfiguration imageConfiguration =
                ImageConfiguration.CreateBuilder(dockerDaemonImage.GetImageReference()).Build();

            DockerClient.Builder dockerClientBuilder = DockerClient.CreateBuilder();
            dockerDaemonImage.GetDockerExecutable().IfPresent(dockerClientBuilder.SetDockerExecutable);
            dockerClientBuilder.SetDockerEnvironment(ImmutableDictionary.CreateRange(dockerDaemonImage.GetDockerEnvironment()));

            return new Containerizer(
                DESCRIPTION_FOR_DOCKER_DAEMON, imageConfiguration, stepsRunnerFactory, false);

            StepsRunner stepsRunnerFactory(BuildConfiguration buildConfiguration) =>
                    StepsRunner.Begin(buildConfiguration)
                        .PullBaseImage()
                        .PullAndCacheBaseImageLayers()
                        .BuildAndCacheApplicationLayers()
                        .BuildImage()
                        .LoadDocker(dockerClientBuilder.Build());
        }

        /**
         * Gets a new {@link Containerizer} that containerizes to a tarball archive.
         *
         * @param tarImage the {@link TarImage} that defines target output file
         * @return a new {@link Containerizer}
         */
        public static Containerizer To(TarImage tarImage)
        {
            tarImage = tarImage ?? throw new ArgumentNullException(nameof(tarImage));
            ImageConfiguration imageConfiguration =
                ImageConfiguration.CreateBuilder(tarImage.GetImageReference()).Build();

            return new Containerizer(
                DESCRIPTION_FOR_TARBALL, imageConfiguration, stepsRunnerFactory, false);

            StepsRunner stepsRunnerFactory(BuildConfiguration buildConfiguration) =>
                    StepsRunner.Begin(buildConfiguration)
                        .PullBaseImage()
                        .PullAndCacheBaseImageLayers()
                        .BuildAndCacheApplicationLayers()
                        .BuildImage()
                        .WriteTarFile(tarImage.GetOutputFile());
        }

        private readonly string description;
        private readonly ImageConfiguration imageConfiguration;
        private readonly Func<BuildConfiguration, StepsRunner> stepsRunnerFactory;
        private readonly bool mustBeOnline;
        private readonly ISet<string> additionalTags = new HashSet<string>();
        public event Action<IJibEvent> JibEvents = _ => { };

        private SystemPath baseImageLayersCacheDirectory = DefaultBaseCacheDirectory;
        private TemporaryDirectory tempAppLayersCacheDir;
        private SystemPath applicationLayersCacheDirectory;
        private bool allowInsecureRegistries = false;
        private bool offline = false;
        private string toolName = DEFAULT_TOOL_NAME;

        /** Instantiate with {@link #to}. */
        private Containerizer(
            string description,
            ImageConfiguration imageConfiguration,
            Func<BuildConfiguration, StepsRunner> stepsRunnerFactory,
            bool mustBeOnline)
        {
            this.description = description;
            this.imageConfiguration = imageConfiguration;
            this.stepsRunnerFactory = stepsRunnerFactory;
            this.mustBeOnline = mustBeOnline;
        }

        /**
         * Adds an additional tag to tag the target image with. For example, the following would
         * containerize to both {@code gcr.io/my-project/my-image:tag} and {@code
         * gcr.io/my-project/my-image:tag2}:
         *
         * <pre>{@code
         * Containerizer.to(RegistryImage.named("gcr.io/my-project/my-image:tag")).withAdditionalTag("tag2");
         * }</pre>
         *
         * @param tag the additional tag to push to
         * @return this
         */
        public Containerizer WithAdditionalTag(string tag)
        {
            Preconditions.CheckArgument(ImageReference.IsValidTag(tag), "invalid tag '{0}'", tag);
            JavaExtensions.Add(additionalTags, tag);
            return this;
        }

        /**
         * Sets the directory to use for caching base image layers. This cache can (and should) be shared
         * between multiple images. The default base image layers cache directory is {@code [user cache
         * home]/google-cloud-tools-java/jib} ({@link #DEFAULT_BASE_CACHE_DIRECTORY}. This directory can
         * be the same directory used for {@link #setApplicationLayersCache}.
         *
         * @param cacheDirectory the cache directory
         * @return this
         */
        public Containerizer SetBaseImageLayersCache(SystemPath cacheDirectory)
        {
            baseImageLayersCacheDirectory = cacheDirectory;
            return this;
        }

        /**
         * Sets the directory to use for caching application layers. This cache can be shared between
         * multiple images. If not set, a temporary directory will be used as the application layers
         * cache. This directory can be the same directory used for {@link #setBaseImageLayersCache}.
         *
         * @param cacheDirectory the cache directory
         * @return this
         */
        public Containerizer SetApplicationLayersCache(SystemPath cacheDirectory)
        {
            applicationLayersCacheDirectory = cacheDirectory;
            return this;
        }

        public Containerizer AddEventHandler<T>(Action<T> eventConsumer) where T : IJibEvent
        {
            return AddEventHandler(je =>
            {
                if (je is T te)
                {
                    eventConsumer(te);
                }
            });
        }

        /**
         * Adds the {@code eventConsumer} to handle all {@link JibEvent} types. See {@link
         * #addEventHandler(Class, Consumer)} for more details.
         *
         * @param eventConsumer the event handler
         * @return this
         */
        private Containerizer AddEventHandler(Action<IJibEvent> eventConsumer)
        {
            JibEvents += eventConsumer;
            return this;
        }

        /**
         * Sets whether or not to allow communication over HTTP/insecure HTTPS.
         *
         * @param allowInsecureRegistries if {@code true}, insecure connections will be allowed
         * @return this
         */
        public Containerizer SetAllowInsecureRegistries(bool allowInsecureRegistries)
        {
            this.allowInsecureRegistries = allowInsecureRegistries;
            return this;
        }

        /**
         * Sets whether or not to run the build in offline mode. In offline mode, the base image is
         * retrieved from the cache instead of pulled from a registry, and the build will fail if the base
         * image is not in the cache or if the target is an image registry.
         *
         * @param offline if {@code true}, the build will run in offline mode
         * @return this
         */
        public Containerizer SetOfflineMode(bool offline)
        {
            if (mustBeOnline && offline)
            {
                throw new InvalidOperationException(Resources.ContainerizerOfflineExceptionMessage);
            }
            this.offline = offline;
            return this;
        }

        /**
         * Sets the name of the tool that is using Jib Core. The tool name is sent as part of the {@code
         * User-Agent} in registry requests and set as the {@code created_by} in the container layer
         * history. Defaults to {@code jib-core}.
         *
         * @param toolName the name of the tool using this library
         * @return this
         */
        public Containerizer SetToolName(string toolName)
        {
            this.toolName = toolName;
            return this;
        }

        public ISet<string> GetAdditionalTags()
        {
            return additionalTags;
        }

        public SystemPath GetBaseImageLayersCacheDirectory()
        {
            return baseImageLayersCacheDirectory;
        }

        public SystemPath GetApplicationLayersCacheDirectory()
        {
            if (applicationLayersCacheDirectory == null)
            {
                // Uses a temporary directory if application layers cache directory is not set.
                try
                {
                    tempAppLayersCacheDir = new TemporaryDirectory(Path.GetTempPath());
                    applicationLayersCacheDirectory = tempAppLayersCacheDir.GetDirectory();
                }
                catch (IOException ex)
                {
                    throw new CacheDirectoryCreationException(ex);
                }
            }
            return applicationLayersCacheDirectory;
        }

        public bool GetAllowInsecureRegistries()
        {
            return allowInsecureRegistries;
        }

        public bool IsOfflineMode()
        {
            return offline;
        }

        public string GetToolName()
        {
            return toolName;
        }

        public string GetDescription()
        {
            return description;
        }

        public ImageConfiguration GetImageConfiguration()
        {
            return imageConfiguration;
        }

        public IStepsRunner CreateStepsRunner(BuildConfiguration buildConfiguration)
        {
            return stepsRunnerFactory(buildConfiguration);
        }

        public EventHandlers BuildEventHandlers()
        {
            return new EventHandlers.Builder().Add<IJibEvent>(e => JibEvents(e)).Build();
        }

        public void Dispose()
        {
            tempAppLayersCacheDir?.Dispose();
        }
    }
}
