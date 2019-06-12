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
    public sealed class Containerizer
    {
        /**
         * The default directory for caching the base image layers, in {@code [user cache
         * home]/google-cloud-tools-java/jib}.
         */
        public static readonly SystemPath DEFAULT_BASE_CACHE_DIRECTORY =
            UserCacheHome.getCacheHome().resolve("google-cloud-tools-java").resolve("jib");

        private static readonly string DEFAULT_TOOL_NAME = "jib-core";

        private static readonly string DESCRIPTION_FOR_DOCKER_REGISTRY = "Building and pushing image";
        private static readonly string DESCRIPTION_FOR_DOCKER_DAEMON = "Building image to Docker daemon";
        private static readonly string DESCRIPTION_FOR_TARBALL = "Building image tarball";

        /**
         * Gets a new {@link Containerizer} that containerizes to a container registry.
         *
         * @param registryImage the {@link RegistryImage} that defines target container registry and
         *     credentials
         * @return a new {@link Containerizer}
         */
        public static Containerizer to(RegistryImage registryImage)
        {
            ImageConfiguration imageConfiguration =
                ImageConfiguration.builder(registryImage.getImageReference())
                    .setCredentialRetrievers(registryImage.getCredentialRetrievers())
                    .build();

            Func<BuildConfiguration, StepsRunner> stepsRunnerFactory =
                buildConfiguration =>
                    StepsRunner.begin(buildConfiguration)
                        .retrieveTargetRegistryCredentials()
                        .authenticatePush()
                        .pullBaseImage()
                        .pullAndCacheBaseImageLayers()
                        .pushBaseImageLayers()
                        .buildAndCacheApplicationLayers()
                        .buildImage()
                        .pushContainerConfiguration()
                        .pushApplicationLayers()
                        .pushImage();

            return new Containerizer(
                DESCRIPTION_FOR_DOCKER_REGISTRY, imageConfiguration, stepsRunnerFactory, true);
        }

        /**
         * Gets a new {@link Containerizer} that containerizes to a Docker daemon.
         *
         * @param dockerDaemonImage the {@link DockerDaemonImage} that defines target Docker daemon
         * @return a new {@link Containerizer}
         */
        public static Containerizer to(DockerDaemonImage dockerDaemonImage)
        {
            ImageConfiguration imageConfiguration =
                ImageConfiguration.builder(dockerDaemonImage.getImageReference()).build();

            DockerClient.Builder dockerClientBuilder = DockerClient.builder();
            dockerDaemonImage.getDockerExecutable().ifPresent(dockerClientBuilder.setDockerExecutable);
            dockerClientBuilder.setDockerEnvironment(ImmutableDictionary.CreateRange(dockerDaemonImage.getDockerEnvironment()));

            Func<BuildConfiguration, StepsRunner> stepsRunnerFactory =
                buildConfiguration =>
                    StepsRunner.begin(buildConfiguration)
                        .pullBaseImage()
                        .pullAndCacheBaseImageLayers()
                        .buildAndCacheApplicationLayers()
                        .buildImage()
                        .loadDocker(dockerClientBuilder.build());

            return new Containerizer(
                DESCRIPTION_FOR_DOCKER_DAEMON, imageConfiguration, stepsRunnerFactory, false);
        }

        /**
         * Gets a new {@link Containerizer} that containerizes to a tarball archive.
         *
         * @param tarImage the {@link TarImage} that defines target output file
         * @return a new {@link Containerizer}
         */
        public static Containerizer to(TarImage tarImage)
        {
            ImageConfiguration imageConfiguration =
                ImageConfiguration.builder(tarImage.getImageReference()).build();

            Func<BuildConfiguration, StepsRunner> stepsRunnerFactory =
                buildConfiguration =>
                    StepsRunner.begin(buildConfiguration)
                        .pullBaseImage()
                        .pullAndCacheBaseImageLayers()
                        .buildAndCacheApplicationLayers()
                        .buildImage()
                        .writeTarFile(tarImage.getOutputFile());

            return new Containerizer(
                DESCRIPTION_FOR_TARBALL, imageConfiguration, stepsRunnerFactory, false);
        }

        private readonly string description;
        private readonly ImageConfiguration imageConfiguration;
        private readonly Func<BuildConfiguration, StepsRunner> stepsRunnerFactory;
        private readonly bool mustBeOnline;
        private readonly ISet<string> additionalTags = new HashSet<string>();
        public event Action<JibEvent> JibEvents = _ => { };

        private SystemPath baseImageLayersCacheDirectory = DEFAULT_BASE_CACHE_DIRECTORY;
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
        public Containerizer withAdditionalTag(string tag)
        {
            Preconditions.checkArgument(ImageReference.isValidTag(tag), "invalid tag '%s'", tag);
            additionalTags.add(tag);
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
        public Containerizer setBaseImageLayersCache(SystemPath cacheDirectory)
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
        public Containerizer setApplicationLayersCache(SystemPath cacheDirectory)
        {
            applicationLayersCacheDirectory = cacheDirectory;
            return this;
        }

        public Containerizer addEventHandler<T>(Action<T> eventConsumer) where T : JibEvent
        {
            return addEventHandler(je =>
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
        private Containerizer addEventHandler(Action<JibEvent> eventConsumer)
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
        public Containerizer setAllowInsecureRegistries(bool allowInsecureRegistries)
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
        public Containerizer setOfflineMode(bool offline)
        {
            if (mustBeOnline && offline)
            {
                throw new InvalidOperationException("Cannot build to a container registry in offline mode");
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
        public Containerizer setToolName(string toolName)
        {
            this.toolName = toolName;
            return this;
        }

        public ISet<string> getAdditionalTags()
        {
            return additionalTags;
        }

        public SystemPath getBaseImageLayersCacheDirectory()
        {
            return baseImageLayersCacheDirectory;
        }

        public SystemPath getApplicationLayersCacheDirectory()
        {
            if (applicationLayersCacheDirectory == null)
            {
                // Uses a temporary directory if application layers cache directory is not set.
                try
                {
                    SystemPath temporaryDirectory = Files.createTempDirectory(null);
                    temporaryDirectory.toFile().deleteOnExit();
                    applicationLayersCacheDirectory = temporaryDirectory;
                }
                catch (IOException ex)
                {
                    throw new CacheDirectoryCreationException(ex);
                }
            }
            return applicationLayersCacheDirectory;
        }

        public bool getAllowInsecureRegistries()
        {
            return allowInsecureRegistries;
        }

        public bool isOfflineMode()
        {
            return offline;
        }

        public string getToolName()
        {
            return toolName;
        }

        public string getDescription()
        {
            return description;
        }

        public ImageConfiguration getImageConfiguration()
        {
            return imageConfiguration;
        }

        public StepsRunner createStepsRunner(BuildConfiguration buildConfiguration)
        {
            return stepsRunnerFactory.apply(buildConfiguration);
        }

        public EventHandlers buildEventHandlers()
        {
            throw new NotImplementedException();
        }
    }
}