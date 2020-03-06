// Copyright 2017 Google LLC.
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
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Images.Json;
using NodaTime;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Fib.Net.Core.Images
{
    /** Represents an image. */
    public sealed class Image
    {
        /** Builds the immutable {@link Image}. */
        public class Builder
        {
            private readonly ManifestFormat imageFormat;
            private readonly ImageLayers.Builder imageLayersBuilder = ImageLayers.CreateBuilder();
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
            public Builder SetCreated(Instant created)
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
            public Builder SetArchitecture(string architecture)
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
            public Builder SetOs(string os)
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
            public Builder AddEnvironment(IDictionary<string, string> environment)
            {
                foreach ((string key, string value) in environment ?? Enumerable.Empty<KeyValuePair<string, string>>())
                {
                    environmentBuilder[key] = value;
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
            public Builder AddEnvironmentVariable(string name, string value)
            {
                environmentBuilder[name] = value;
                return this;
            }

            /**
             * Sets the entrypoint of the image.
             *
             * @param entrypoint the list of entrypoint tokens
             * @return this
             */
            public Builder SetEntrypoint(IList<string> entrypoint)
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
            public Builder SetUser(string user)
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
            public Builder SetProgramArguments(IList<string> programArguments)
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
            public Builder SetHealthCheck(DockerHealthCheck healthCheck)
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
            public Builder AddExposedPorts(ISet<Port> exposedPorts)
            {
                if (exposedPorts != null)
                {
                    exposedPortsBuilder.UnionWith(exposedPorts);
                }
                return this;
            }

            /**
             * Adds items to the "Volumes" field in the container configuration.
             *
             * @param volumes the directories to create volumes
             * @return this
             */
            public Builder AddVolumes(ISet<AbsoluteUnixPath> volumes)
            {
                if (volumes != null)
                {
                    volumesBuilder.UnionWith(ImmutableHashSet.CreateRange(volumes));
                }
                return this;
            }

            /**
             * Adds items to the "Labels" field in the container configuration.
             *
             * @param labels the map of labels to add
             * @return this
             */
            public Builder AddLabels(IDictionary<string, string> labels)
            {
                foreach ((string key, string value) in labels ?? Enumerable.Empty<KeyValuePair<string, string>>())
                {
                    labelsBuilder[key] = value;
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
            public Builder AddLabel(string name, string value)
            {
                labelsBuilder[name] = value;
                return this;
            }

            /**
             * Sets the item in the "WorkingDir" field in the container configuration.
             *
             * @param workingDirectory the working directory
             * @return this
             */
            public Builder SetWorkingDirectory(string workingDirectory)
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
            public Builder AddLayer(ILayer layer)
            {
                imageLayersBuilder.Add(layer);
                return this;
            }

            /**
             * Adds a history element to the image.
             *
             * @param history the history object to add
             * @return this
             */
            public Builder AddHistory(HistoryEntry history)
            {
                historyBuilder.Add(history);
                return this;
            }

            public Image Build()
            {
                return new Image(
                    imageFormat,
                    created,
                    architecture,
                    os,
                    imageLayersBuilder.Build(),
                    historyBuilder.ToImmutable(),
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

        public static Builder CreateBuilder(ManifestFormat imageFormat)
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

        public ManifestFormat GetImageFormat()
        {
            return imageFormat;
        }

        public Instant? GetCreated()
        {
            return created;
        }

        public string GetArchitecture()
        {
            return architecture;
        }

        public string GetOs()
        {
            return os;
        }

        public ImmutableDictionary<string, string> GetEnvironment()
        {
            return environment;
        }

        public ImmutableArray<string>? GetEntrypoint()
        {
            return entrypoint;
        }

        public ImmutableArray<string>? GetProgramArguments()
        {
            return programArguments;
        }

        public DockerHealthCheck GetHealthCheck()
        {
            return healthCheck;
        }

        public ImmutableHashSet<Port> GetExposedPorts()
        {
            return exposedPorts;
        }

        public ImmutableHashSet<AbsoluteUnixPath> GetVolumes()
        {
            return volumes;
        }

        public ImmutableDictionary<string, string> GetLabels()
        {
            return labels;
        }

        public string GetWorkingDirectory()
        {
            return workingDirectory;
        }

        public string GetUser()
        {
            return user;
        }

        public ImmutableArray<ILayer> GetLayers()
        {
            return layers.GetLayers();
        }

        public ImmutableArray<HistoryEntry> GetHistory()
        {
            return history;
        }
    }
}
