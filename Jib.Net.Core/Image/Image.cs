/*
 * Copyright 2017 Google LLC.
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
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.image
{
    /** Represents an image. */
    public sealed class Image
    {
        /** Builds the immutable {@link Image}. */
        public class Builder
        {
            private readonly ManifestFormat imageFormat;
            private readonly ImageLayers.Builder imageLayersBuilder = ImageLayers.builder();
            private readonly ImmutableArray<HistoryEntry>.Builder historyBuilder = ImmutableArray.CreateBuilder<HistoryEntry>();

            // Don't use ImmutableDictionary.Builder because it does not allow for replacing existing keys with new
            // values.
            private readonly IDictionary<string, string> environmentBuilder = new Dictionary<string, string>();
            private readonly IDictionary<string, string> labelsBuilder = new Dictionary<string, string>();
            private readonly ISet<Port> exposedPortsBuilder = new HashSet<Port>();
            private readonly ISet<AbsoluteUnixPath> volumesBuilder = new HashSet<AbsoluteUnixPath>();

            private Instant created;
            private string architecture = "amd64";
            private string os = "linux";
            private ImmutableArray<string>? entrypoint;
            private ImmutableArray<string>? programArguments;
            private DockerHealthCheck healthCheck;
            private string workingDirectory;
            private string user;

            public Builder(ManifestFormat imageFormat)
            {
                this.imageFormat = imageFormat;
            }

            /**
             * Sets the image creation time.
             *
             * @param created the creation time
             * @return this
             */
            public Builder setCreated(Instant created)
            {
                this.created = created;
                return this;
            }

            /**
             * Sets the image architecture.
             *
             * @param architecture the architecture
             * @return this
             */
            public Builder setArchitecture(string architecture)
            {
                this.architecture = architecture;
                return this;
            }

            /**
             * Sets the image operating system.
             *
             * @param os the operating system
             * @return this
             */
            public Builder setOs(string os)
            {
                this.os = os;
                return this;
            }

            /**
             * Adds a map of environment variables to the current map.
             *
             * @param environment the map of environment variables
             * @return this
             */
            public Builder addEnvironment(IDictionary<string, string> environment)
            {
                if (environment != null)
                {
                    this.environmentBuilder.putAll(environment);
                }
                return this;
            }

            /**
             * Adds an environment variable with a given name and value.
             *
             * @param name the name of the variable
             * @param value the value to set it to
             * @return this
             */
            public Builder addEnvironmentVariable(string name, string value)
            {
                environmentBuilder.put(name, value);
                return this;
            }

            /**
             * Sets the entrypoint of the image.
             *
             * @param entrypoint the list of entrypoint tokens
             * @return this
             */
            public Builder setEntrypoint(IList<string> entrypoint)
            {
                this.entrypoint = entrypoint?.ToImmutableArray();
                return this;
            }

            /**
             * Sets the user/group to run the container as.
             *
             * @param user the username/UID and optionally the groupname/GID
             * @return this
             */
            public Builder setUser(string user)
            {
                this.user = user;
                return this;
            }

            /**
             * Sets the items in the "Cmd" field in the container configuration.
             *
             * @param programArguments the list of arguments to append to the image entrypoint
             * @return this
             */
            public Builder setProgramArguments(IList<string> programArguments)
            {
                this.programArguments = programArguments?.ToImmutableArray();
                return this;
            }

            /**
             * Sets the container's healthcheck configuration.
             *
             * @param healthCheck the healthcheck configuration
             * @return this
             */
            public Builder setHealthCheck(DockerHealthCheck healthCheck)
            {
                this.healthCheck = healthCheck;
                return this;
            }

            /**
             * Adds items to the "ExposedPorts" field in the container configuration.
             *
             * @param exposedPorts the exposed ports to add
             * @return this
             */
            public Builder addExposedPorts(ISet<Port> exposedPorts)
            {
                if (exposedPorts != null)
                {
                    exposedPortsBuilder.addAll(exposedPorts);
                }
                return this;
            }

            /**
             * Adds items to the "Volumes" field in the container configuration.
             *
             * @param volumes the directories to create volumes
             * @return this
             */
            public Builder addVolumes(ISet<AbsoluteUnixPath> volumes)
            {
                if (volumes != null)
                {
                    volumesBuilder.addAll(ImmutableHashSet.CreateRange(volumes));
                }
                return this;
            }

            /**
             * Adds items to the "Labels" field in the container configuration.
             *
             * @param labels the map of labels to add
             * @return this
             */
            public Builder addLabels(IDictionary<string, string> labels)
            {
                if (labels != null)
                {
                    labelsBuilder.putAll(labels);
                }
                return this;
            }

            /**
             * Adds an item to the "Labels" field in the container configuration.
             *
             * @param name the name of the label
             * @param value the value of the label
             * @return this
             */
            public Builder addLabel(string name, string value)
            {
                labelsBuilder.put(name, value);
                return this;
            }

            /**
             * Sets the item in the "WorkingDir" field in the container configuration.
             *
             * @param workingDirectory the working directory
             * @return this
             */
            public Builder setWorkingDirectory(string workingDirectory)
            {
                this.workingDirectory = workingDirectory;
                return this;
            }

            /**
             * Adds a layer to the image.
             *
             * @param layer the layer to add
             * @return this
             * @throws LayerPropertyNotFoundException if adding the layer fails
             */
            public Builder addLayer(ILayer layer)
            {
                imageLayersBuilder.add(layer);
                return this;
            }

            /**
             * Adds a history element to the image.
             *
             * @param history the history object to add
             * @return this
             */
            public Builder addHistory(HistoryEntry history)
            {
                historyBuilder.add(history);
                return this;
            }

            public Image build()
            {
                return new Image(
                    imageFormat,
                    created,
                    architecture,
                    os,
                    imageLayersBuilder.build(),
                    historyBuilder.build(),
                    ImmutableDictionary.CreateRange(environmentBuilder),
                    entrypoint,
                    programArguments,
                    healthCheck,
                    ImmutableHashSet.CreateRange(exposedPortsBuilder),
                    ImmutableHashSet.CreateRange(volumesBuilder),
                    ImmutableDictionary.CreateRange(labelsBuilder),
                    workingDirectory,
                    user);
            }
        }

        public static Builder builder(ManifestFormat imageFormat)
        {
            return new Builder(imageFormat);
        }

        /** The image format. */
        private readonly ManifestFormat imageFormat;

        /** The image creation time. */
        private readonly Instant? created;

        /** The image architecture. */
        private readonly string architecture;

        /** The image operating system. */
        private readonly string os;

        /** The layers of the image, in the order in which they are applied. */
        private readonly ImageLayers layers;

        /** The commands used to build each layer of the image */
        private readonly ImmutableArray<HistoryEntry> history;

        /** Environment variable definitions for running the image, in the format {@code NAME=VALUE}. */
        private readonly ImmutableDictionary<string, string> environment;

        /** Initial command to run when running the image. */
        private readonly ImmutableArray<string>? entrypoint;

        /** Arguments to append to the image entrypoint when running the image. */
        private readonly ImmutableArray<string>? programArguments;

        /** Healthcheck configuration. */
        private readonly DockerHealthCheck healthCheck;

        /** Ports that the container listens on. */
        private readonly ImmutableHashSet<Port> exposedPorts;

        /** Directories to mount as volumes. */
        private readonly ImmutableHashSet<AbsoluteUnixPath> volumes;

        /** Labels on the container configuration */
        private readonly ImmutableDictionary<string, string> labels;

        /** Working directory on the container configuration */
        private readonly string workingDirectory;

        /** User on the container configuration */
        private readonly string user;

        private Image(
            ManifestFormat imageFormat,
            Instant created,
            string architecture,
            string os,
            ImageLayers layers,
            ImmutableArray<HistoryEntry> history,
            ImmutableDictionary<string, string> environment,
            ImmutableArray<string>? entrypoint,
            ImmutableArray<string>? programArguments,
            DockerHealthCheck healthCheck,
            ImmutableHashSet<Port> exposedPorts,
            ImmutableHashSet<AbsoluteUnixPath> volumes,
            ImmutableDictionary<string, string> labels,
            string workingDirectory,
            string user)
        {
            this.imageFormat = imageFormat;
            this.created = created;
            this.architecture = architecture;
            this.os = os;
            this.layers = layers;
            this.history = history;
            this.environment = environment;
            this.entrypoint = entrypoint;
            this.programArguments = programArguments;
            this.healthCheck = healthCheck;
            this.exposedPorts = exposedPorts;
            this.volumes = volumes;
            this.labels = labels;
            this.workingDirectory = workingDirectory;
            this.user = user;
        }

        public ManifestFormat getImageFormat()
        {
            return this.imageFormat;
        }

        public Instant? getCreated()
        {
            return created;
        }

        public string getArchitecture()
        {
            return architecture;
        }

        public string getOs()
        {
            return os;
        }

        public ImmutableDictionary<string, string> getEnvironment()
        {
            return environment;
        }

        public ImmutableArray<string>? getEntrypoint()
        {
            return entrypoint;
        }

        public ImmutableArray<string>? getProgramArguments()
        {
            return programArguments;
        }

        public DockerHealthCheck getHealthCheck()
        {
            return healthCheck;
        }

        public ImmutableHashSet<Port> getExposedPorts()
        {
            return exposedPorts;
        }

        public ImmutableHashSet<AbsoluteUnixPath> getVolumes()
        {
            return volumes;
        }

        public ImmutableDictionary<string, string> getLabels()
        {
            return labels;
        }

        public string getWorkingDirectory()
        {
            return workingDirectory;
        }

        public string getUser()
        {
            return user;
        }

        public ImmutableArray<ILayer> getLayers()
        {
            return layers.getLayers();
        }

        public ImmutableArray<HistoryEntry> getHistory()
        {
            return history;
        }
    }
}
