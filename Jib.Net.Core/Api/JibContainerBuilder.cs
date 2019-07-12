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
using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.BuildSteps;
using Jib.Net.Core.Events;
using Jib.Net.Core.Events.Time;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NodaTime.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jib.Net.Core.Api
{
    // TODO: Move to com.google.cloud.tools.jib once that package is cleaned up.

    /**
     * Builds a container with Jib.
     *
     * <p>Example usage:
     *
     * <pre>{@code
     * Jib.from(baseImage)
     *    .addLayer(sourceFiles, extractionPath)
     *    .setEntrypoint("myprogram", "--flag", "subcommand")
     *    .setProgramArguments("hello", "world")
     *    .addEnvironmentVariable("HOME", "/app")
     *    .addExposedPort(Port.tcp(8080))
     *    .addLabel("containerizer", "jib")
     *    .containerize(...);
     * }</pre>
     */
    // TODO: Add tests once containerize() is added.
    public class JibContainerBuilder
    {
        /**
         * Starts building the container from a base image. The base image should be publicly-available.
         * For a base image that requires credentials, use {@link #from(RegistryImage)}.
         *
         * @param baseImageReference the base image reference
         * @return a new {@link JibContainerBuilder} to continue building the container
         * @throws InvalidImageReferenceException if the {@code baseImageReference} is not a valid image
         *     reference
         */
        public static JibContainerBuilder From(string baseImageReference)
        {
            return From(RegistryImage.Named(baseImageReference));
        }

        /**
         * Starts building the container from a base image. The base image should be publicly-available.
         * For a base image that requires credentials, use {@link #from(RegistryImage)}.
         *
         * @param baseImageReference the base image reference
         * @return a new {@link JibContainerBuilder} to continue building the container
         */
        public static JibContainerBuilder From(ImageReference baseImageReference)
        {
            return From(RegistryImage.Named(baseImageReference));
        }

        /**
         * Starts building the container from a base image.
         *
         * @param registryImage the {@link RegistryImage} that defines base container registry and
         *     credentials
         * @return a new {@link JibContainerBuilder} to continue building the container
         */
        public static JibContainerBuilder From(RegistryImage registryImage)
        {
            return new JibContainerBuilder(registryImage);
        }

        /**
         * Starts building the container from an empty base image.
         *
         * @return a new {@link JibContainerBuilder} to continue building the container
         */
        public static JibContainerBuilder FromScratch()
        {
            return From(ImageReference.Scratch());
        }

        private readonly ContainerConfiguration.Builder containerConfigurationBuilder =
            ContainerConfiguration.CreateBuilder();

        private readonly BuildConfiguration.Builder buildConfigurationBuilder;

        private IList<ILayerConfiguration> layerConfigurations = new List<ILayerConfiguration>();

        /** Instantiate with {@link Jib#from}. */
        public JibContainerBuilder(RegistryImage baseImage) :
          this(baseImage, BuildConfiguration.CreateBuilder())
        {
        }

        public JibContainerBuilder(RegistryImage baseImage, BuildConfiguration.Builder buildConfigurationBuilder)
        {
            baseImage = baseImage ?? throw new ArgumentNullException(nameof(baseImage));
            this.buildConfigurationBuilder = buildConfigurationBuilder ??
                throw new ArgumentNullException(nameof(buildConfigurationBuilder));

            ImageConfiguration imageConfiguration =
                ImageConfiguration.CreateBuilder(baseImage.GetImageReference())
                    .SetCredentialRetrievers(baseImage.GetCredentialRetrievers())
                    .Build();
            buildConfigurationBuilder.SetBaseImageConfiguration(imageConfiguration);
        }

        /**
         * Adds a new layer to the container with {@code files} as the source files and {@code
         * pathInContainer} as the path to copy the source files to in the container file system.
         *
         * <p>Source files that are directories will be recursively copied. For example, if the source
         * files are:
         *
         * <ul>
         *   <li>{@code fileA}
         *   <li>{@code fileB}
         *   <li>{@code directory/}
         * </ul>
         *
         * and the destination to copy to is {@code /path/in/container}, then the new layer will have the
         * following entries for the container file system:
         *
         * <ul>
         *   <li>{@code /path/in/container/fileA}
         *   <li>{@code /path/in/container/fileB}
         *   <li>{@code /path/in/container/directory/}
         *   <li>{@code /path/in/container/directory/...} (all contents of {@code directory/})
         * </ul>
         *
         * @param files the source files to copy to a new layer in the container
         * @param pathInContainer the path in the container file system corresponding to the {@code
         *     sourceFile}
         * @return this
         * @throws IOException if an exception occurred when recursively listing any directories
         */
        public JibContainerBuilder AddLayer(IList<SystemPath> files, AbsoluteUnixPath pathInContainer)
        {
            pathInContainer = pathInContainer ?? throw new ArgumentNullException(nameof(pathInContainer));
            files = files ?? throw new ArgumentNullException(nameof(files));

            LayerConfiguration.Builder layerConfigurationBuilder = LayerConfiguration.CreateBuilder();

            foreach (SystemPath file in files)

            {
                layerConfigurationBuilder.AddEntryRecursive(
                    file, pathInContainer.Resolve(file.GetFileName()));
            }

            return AddLayer(layerConfigurationBuilder.Build());
        }

        /**
         * Adds a new layer to the container with {@code files} as the source files and {@code
         * pathInContainer} as the path to copy the source files to in the container file system.
         *
         * @param files the source files to copy to a new layer in the container
         * @param pathInContainer the path in the container file system corresponding to the {@code
         *     sourceFile}
         * @return this
         * @throws IOException if an exception occurred when recursively listing any directories
         * @throws IllegalArgumentException if {@code pathInContainer} is not an absolute Unix-style path
         * @see #addLayer(List, AbsoluteUnixPath)
         */
        public JibContainerBuilder AddLayer(IList<SystemPath> files, string pathInContainer)
        {
            return AddLayer(files, AbsoluteUnixPath.Get(pathInContainer));
        }

        /**
         * Adds a layer (defined by a {@link LayerConfiguration}).
         *
         * @param layerConfiguration the {@link LayerConfiguration}
         * @return this
         */
        public JibContainerBuilder AddLayer(ILayerConfiguration layerConfiguration)
        {
            JavaExtensions.Add(layerConfigurations, layerConfiguration);
            return this;
        }

        /**
         * Sets the layers (defined by a list of {@link LayerConfiguration}s). This replaces any
         * previously-added layers.
         *
         * @param layerConfigurations the list of {@link LayerConfiguration}s
         * @return this
         */
        public JibContainerBuilder SetLayers(IEnumerable<ILayerConfiguration> layerConfigurations)
        {
            this.layerConfigurations = new List<ILayerConfiguration>(layerConfigurations);
            return this;
        }

        /**
         * Sets the layers. This replaces any previously-added layers.
         *
         * @param layerConfigurations the {@link LayerConfiguration}s
         * @return this
         */
        public JibContainerBuilder SetLayers(params ILayerConfiguration[] layerConfigurations)
        {
            return SetLayers(layerConfigurations.ToList());
        }

        /**
         * Sets the container entrypoint. This is the beginning of the command that is run when the
         * container starts. {@link #setProgramArguments} sets additional tokens.
         *
         * <p>This is similar to <a
         * href="https://docs.docker.com/engine/reference/builder/#exec-form-entrypoint-example">{@code
         * ENTRYPOINT} in Dockerfiles</a> or {@code command} in the <a
         * href="https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.11/#container-v1-core">Kubernetes
         * Container spec</a>.
         *
         * @param entrypoint a list of the entrypoint command
         * @return this
         */
        public JibContainerBuilder SetEntrypoint(IEnumerable<string> entrypoint)
        {
            containerConfigurationBuilder.SetEntrypoint(entrypoint);
            return this;
        }

        /**
         * Sets the container entrypoint.
         *
         * @param entrypoint the entrypoint command
         * @return this
         * @see #setEntrypoint(List)
         */
        public JibContainerBuilder SetEntrypoint(params string[] entrypoint)
        {
            return SetEntrypoint(entrypoint.ToList());
        }

        /**
         * Sets the container entrypoint program arguments. These are additional tokens added to the end
         * of the entrypoint command.
         *
         * <p>This is similar to <a href="https://docs.docker.com/engine/reference/builder/#cmd">{@code
         * CMD} in Dockerfiles</a> or {@code args} in the <a
         * href="https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.11/#container-v1-core">Kubernetes
         * Container spec</a>.
         *
         * <p>For example, if the entrypoint was {@code myprogram --flag subcommand} and program arguments
         * were {@code hello world}, then the command that run when the container starts is {@code
         * myprogram --flag subcommand hello world}.
         *
         * @param programArguments a list of program argument tokens
         * @return this
         */
        public JibContainerBuilder SetProgramArguments(IEnumerable<string> programArguments)
        {
            containerConfigurationBuilder.SetProgramArguments(programArguments);
            return this;
        }

        /**
         * Sets the container entrypoint program arguments.
         *
         * @param programArguments program arguments tokens
         * @return this
         * @see #setProgramArguments(List)
         */
        public JibContainerBuilder SetProgramArguments(params string[] programArguments)
        {
            return SetProgramArguments(programArguments.ToList());
        }

        /**
         * Sets the container environment. These environment variables are available to the program
         * launched by the container entrypoint command. This replaces any previously-set environment
         * variables.
         *
         * <p>This is similar to <a href="https://docs.docker.com/engine/reference/builder/#env">{@code
         * ENV} in Dockerfiles</a> or {@code env} in the <a
         * href="https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.11/#container-v1-core">Kubernetes
         * Container spec</a>.
         *
         * @param environmentMap a map of environment variable names to values
         * @return this
         */
        public JibContainerBuilder SetEnvironment(IEnumerable<KeyValuePair<string, string>> environmentMap)
        {
            containerConfigurationBuilder.SetEnvironment(environmentMap);
            return this;
        }

        /**
         * Sets a variable in the container environment.
         *
         * @param name the environment variable name
         * @param value the environment variable value
         * @return this
         * @see #setEnvironment
         */
        public JibContainerBuilder AddEnvironmentVariable(string name, string value)
        {
            containerConfigurationBuilder.AddEnvironment(name, value);
            return this;
        }

        /**
         * Sets the directories that may hold externally mounted volumes.
         *
         * <p>This is similar to <a href="https://docs.docker.com/engine/reference/builder/#volume">{@code
         * VOLUME} in Dockerfiles</a>.
         *
         * @param volumes the directory paths on the container filesystem to set as volumes
         * @return this
         */
        public JibContainerBuilder SetVolumes(IEnumerable<AbsoluteUnixPath> volumes)
        {
            containerConfigurationBuilder.SetVolumes(volumes);
            return this;
        }

        /**
         * Sets the directories that may hold externally mounted volumes.
         *
         * @param volumes the directory paths on the container filesystem to set as volumes
         * @return this
         * @see #setVolumes(ISet)
         */
        public JibContainerBuilder SetVolumes(params AbsoluteUnixPath[] volumes)
        {
            return SetVolumes(new HashSet<AbsoluteUnixPath>(volumes));
        }

        /**
         * Adds a directory that may hold an externally mounted volume.
         *
         * @param volume a directory path on the container filesystem to represent a volume
         * @return this
         * @see #setVolumes(ISet)
         */
        public JibContainerBuilder AddVolume(AbsoluteUnixPath volume)
        {
            containerConfigurationBuilder.AddVolume(volume);
            return this;
        }

        /**
         * Sets the ports to expose from the container. Ports exposed will allow ingress traffic. This
         * replaces any previously-set exposed ports.
         *
         * <p>Use {@link Port#tcp} to expose a port for TCP traffic and {@link Port#udp} to expose a port
         * for UDP traffic.
         *
         * <p>This is similar to <a href="https://docs.docker.com/engine/reference/builder/#expose">{@code
         * EXPOSE} in Dockerfiles</a> or {@code ports} in the <a
         * href="https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.11/#container-v1-core">Kubernetes
         * Container spec</a>.
         *
         * @param ports the ports to expose
         * @return this
         */
        public JibContainerBuilder SetExposedPorts(IEnumerable<Port> ports)
        {
            containerConfigurationBuilder.SetExposedPorts(ports);
            return this;
        }

        /**
         * Sets the ports to expose from the container. This replaces any previously-set exposed ports.
         *
         * @param ports the ports to expose
         * @return this
         * @see #setExposedPorts(ISet)
         */
        public JibContainerBuilder SetExposedPorts(params Port[] ports)
        {
            return SetExposedPorts(new HashSet<Port>(ports));
        }

        /**
         * Adds a port to expose from the container.
         *
         * @param port the port to expose
         * @return this
         * @see #setExposedPorts(ISet)
         */
        public JibContainerBuilder AddExposedPort(Port port)
        {
            containerConfigurationBuilder.AddExposedPort(port);
            return this;
        }

        /**
         * Sets the labels for the container. This replaces any previously-set labels.
         *
         * <p>This is similar to <a href="https://docs.docker.com/engine/reference/builder/#label">{@code
         * LABEL} in Dockerfiles</a>.
         *
         * @param labelMap a map of label keys to values
         * @return this
         */
        public JibContainerBuilder SetLabels(IEnumerable<KeyValuePair<string, string>> labelMap)
        {
            containerConfigurationBuilder.SetLabels(labelMap);
            return this;
        }

        /**
         * Sets a label for the container.
         *
         * @param key the label key
         * @param value the label value
         * @return this
         */
        public JibContainerBuilder AddLabel(string key, string value)
        {
            containerConfigurationBuilder.AddLabel(key, value);
            return this;
        }

        /**
         * Sets the format to build the container image as. Use {@link ImageFormat#Docker} for Docker V2.2
         * or {@link ImageFormat#OCI} for OCI.
         *
         * @param imageFormat the {@link ImageFormat}
         * @return this
         */
        public JibContainerBuilder SetFormat(ImageFormat imageFormat)
        {
            buildConfigurationBuilder.SetTargetFormat(imageFormat);
            return this;
        }

        /**
         * Sets the container image creation time. The default is {@link Instant#EPOCH}.
         *
         * @param creationTime the container image creation time
         * @return this
         */
        public JibContainerBuilder SetCreationTime(DateTimeOffset creationTime)
        {
            containerConfigurationBuilder.SetCreationTime(creationTime.ToInstant());
            return this;
        }

        /**
         * Sets the user and group to run the container as. {@code user} can be a username or UID along
         * with an optional groupname or GID.
         *
         * <p>The following are valid formats for {@code user}
         *
         * <ul>
         *   <li>{@code user}
         *   <li>{@code uid}
         *   <li>{@code user:group}
         *   <li>{@code uid:gid}
         *   <li>{@code uid:group}
         *   <li>{@code user:gid}
         * </ul>
         *
         * @param user the user to run the container as
         * @return this
         */
        public JibContainerBuilder SetUser(string user)
        {
            containerConfigurationBuilder.SetUser(user);
            return this;
        }

        /**
         * Sets the working directory in the container.
         *
         * @param workingDirectory the working directory
         * @return this
         */
        public JibContainerBuilder SetWorkingDirectory(AbsoluteUnixPath workingDirectory)
        {
            containerConfigurationBuilder.SetWorkingDirectory(workingDirectory);
            return this;
        }

        /**
         * Builds the container.
         *
         * @param containerizer the {@link Containerizer} that configures how to containerize
         * @return the built container
         * @throws IOException if an I/O exception occurs
         * @throws CacheDirectoryCreationException if a directory to be used for the cache could not be
         *     created
         * @throws HttpHostConnectException if jib failed to connect to a registry
         * @throws RegistryUnauthorizedException if a registry request is unauthorized and needs
         *     authentication
         * @throws RegistryAuthenticationFailedException if registry authentication failed
         * @throws UnknownHostException if the registry does not exist
         * @throws InsecureRegistryException if a server could not be verified due to an insecure
         *     connection
         * @throws RegistryException if some other error occurred while interacting with a registry
         * @throws ExecutionException if some other exception occurred during execution
         * @throws InterruptedException if the execution was interrupted
         */
        public async Task<JibContainer> ContainerizeAsync(IContainerizer containerizer)
        {
            containerizer = containerizer ?? throw new ArgumentNullException(nameof(containerizer));
            BuildConfiguration buildConfiguration = ToBuildConfiguration(containerizer);

            IEventHandlers eventHandlers = buildConfiguration.GetEventHandlers();
            LogSources(eventHandlers);

            using (new TimerEventDispatcher(eventHandlers, containerizer.GetDescription()))
            {
                try
                {
                    IBuildResult result = await containerizer.CreateStepsRunner(buildConfiguration).RunAsync().ConfigureAwait(false);
                    return new JibContainer(result.GetImageDigest(), result.GetImageId());
                }
                catch (Exception ex)
                {
                    // If an ExecutionException occurs, re-throw the cause to be more easily handled by the user
                    if (ex.InnerException is RegistryException)
                    {
                        throw (RegistryException)ex.InnerException;
                    }
                    throw;
                }
            }
        }

        /**
         * Builds a {@link BuildConfiguration} using this and a {@link Containerizer}.
         *
         * @param containerizer the {@link Containerizer}
         * @param executorService the {@link ExecutorService} to use, overriding the executor in the
         *     {@link Containerizer}
         * @return the {@link BuildConfiguration}
         * @throws CacheDirectoryCreationException if a cache directory could not be created
         * @throws IOException if an I/O exception occurs
         */

        public BuildConfiguration ToBuildConfiguration(
            IContainerizer containerizer)
        {
            containerizer = containerizer ?? throw new ArgumentNullException(nameof(containerizer));
            return buildConfigurationBuilder
                .SetTargetImageConfiguration(containerizer.GetImageConfiguration())
                .SetAdditionalTargetImageTags(containerizer.GetAdditionalTags())
                .SetBaseImageLayersCacheDirectory(containerizer.GetBaseImageLayersCacheDirectory())
                .SetApplicationLayersCacheDirectory(containerizer.GetApplicationLayersCacheDirectory())
                .SetContainerConfiguration(containerConfigurationBuilder.Build())
                .SetLayerConfigurations(layerConfigurations)
                .SetAllowInsecureRegistries(containerizer.GetAllowInsecureRegistries())
                .SetOffline(containerizer.IsOfflineMode())
                .SetToolName(containerizer.GetToolName())
                .SetEventHandlers(containerizer.BuildEventHandlers())
                .Build();
        }

        private void LogSources(IEventHandlers eventHandlers)
        {
            // Logs the different source files used.
            var message = new StringBuilder(Resources.ContainerBuilderLogSourcesHeader);
            message.Append(":");

            foreach (LayerConfiguration layerConfiguration in layerConfigurations)

            {
                if (layerConfiguration.LayerEntries.Length == 0)
                {
                    continue;
                }

                message.Append('\t').Append(layerConfiguration.Name).Append(':');

                foreach (LayerEntry layerEntry in layerConfiguration.LayerEntries)
                {
                    message.Append("\t\t").Append(layerEntry.SourceFile);
                }
            }
            eventHandlers.Dispatch(LogEvent.Info(message.ToString()));
        }
    }
}
