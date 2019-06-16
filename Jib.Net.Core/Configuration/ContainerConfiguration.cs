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
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using NodaTime;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.configuration
{







    /** Immutable configuration options for the container. */
    public sealed class ContainerConfiguration : IContainerConfiguration
    {
        /** The default creation time of the container (constant to ensure reproducibility by default). */
        public static readonly Instant DEFAULT_CREATION_TIME = Instant.FromUnixTimeMilliseconds(0);

        /** Builder for instantiating a {@link ContainerConfiguration}. */
        public class Builder
        {
            private Instant creationTime = DEFAULT_CREATION_TIME;
            private ImmutableArray<string>? entrypoint;
            private ImmutableArray<string>? programArguments;
            private IDictionary<string, string> environmentMap;
            private ISet<Port> exposedPorts;
            private ISet<AbsoluteUnixPath> volumes;
            private IDictionary<string, string> labels;
            private string user;
            private AbsoluteUnixPath workingDirectory;

            /**
             * Sets the image creation time.
             *
             * @param creationTime the creation time
             * @return this
             */
            public Builder setCreationTime(Instant creationTime)
            {
                this.creationTime = creationTime;
                return this;
            }

            /**
             * Sets the commandline arguments for main.
             *
             * @param programArguments the list of arguments
             * @return this
             */
            public Builder setProgramArguments(IList<string> programArguments)
            {
                if (programArguments == null)
                {
                    this.programArguments = ImmutableArray<string>.Empty;
                }
                else
                {
                    Preconditions.checkArgument(
                        !programArguments.contains(null), "program arguments list contains null elements");
                    this.programArguments = ImmutableArray.CreateRange(programArguments);
                }
                return this;
            }

            /**
             * Sets the container's environment variables, mapping variable name to value.
             *
             * @param environmentMap the map
             * @return this
             */
            public Builder setEnvironment(IDictionary<string, string> environmentMap)
            {
                if (environmentMap == null)
                {
                    this.environmentMap = null;
                }
                else
                {
                    Preconditions.checkArgument(
                        !Iterables.any(environmentMap.keySet(), Objects.isNull),
                        "environment map contains null keys");
                    Preconditions.checkArgument(
                        !Iterables.any(environmentMap.values(), Objects.isNull),
                        "environment map contains null values");
                    this.environmentMap = new Dictionary<string, string>(environmentMap);
                }
                return this;
            }

            public void addEnvironment(string name, string value)
            {
                if (environmentMap == null)
                {
                    environmentMap = new Dictionary<string, string>();
                }
                environmentMap.put(name, value);
            }

            /**
             * Sets the container's exposed ports.
             *
             * @param exposedPorts the set of ports
             * @return this
             */
            public Builder setExposedPorts(ISet<Port> exposedPorts)
            {
                if (exposedPorts == null)
                {
                    this.exposedPorts = null;
                }
                else
                {
                    Preconditions.checkArgument(
                        !exposedPorts.contains(null), "ports list contains null elements");
                    this.exposedPorts = new HashSet<Port>(exposedPorts);
                }
                return this;
            }

            public void addExposedPort(Port port)
            {
                if (exposedPorts == null)
                {
                    exposedPorts = new HashSet<Port>();
                }
                exposedPorts.add(port);
            }

            /**
             * Sets the container's volumes.
             *
             * @param volumes the set of volumes
             * @return this
             */
            public Builder setVolumes(ISet<AbsoluteUnixPath> volumes)
            {
                if (volumes == null)
                {
                    this.volumes = null;
                }
                else
                {
                    Preconditions.checkArgument(!volumes.contains(null), "volumes list contains null elements");
                    this.volumes = new HashSet<AbsoluteUnixPath>(volumes);
                }
                return this;
            }

            public void addVolume(AbsoluteUnixPath volume)
            {
                if (volumes == null)
                {
                    volumes = new HashSet<AbsoluteUnixPath>();
                }
                volumes.add(volume);
            }

            /**
             * Sets the container's labels.
             *
             * @param labels the map of labels
             * @return this
             */
            public Builder setLabels(IDictionary<string, string> labels)
            {
                if (labels == null)
                {
                    this.labels = null;
                }
                else
                {
                    Preconditions.checkArgument(
                        !Iterables.any(labels.keySet(), Objects.isNull), "labels map contains null keys");
                    Preconditions.checkArgument(
                        !Iterables.any(labels.values(), Objects.isNull), "labels map contains null values");
                    this.labels = new Dictionary<string, string>(labels);
                }
                return this;
            }

            public void addLabel(string key, string value)
            {
                if (labels == null)
                {
                    labels = new Dictionary<string, string>();
                }
                labels.put(key, value);
            }

            /**
             * Sets the container entrypoint.
             *
             * @param entrypoint the tokenized command to run when the container starts
             * @return this
             */
            public Builder setEntrypoint(IList<string> entrypoint)
            {
                if (entrypoint == null)
                {
                    this.entrypoint = null;
                }
                else
                {
                    Preconditions.checkArgument(
                        !entrypoint.contains(null), "entrypoint contains null elements");
                    this.entrypoint = ImmutableArray.CreateRange(entrypoint);
                }
                return this;
            }

            /**
             * Sets the user and group to run the container as. {@code user} can be a username or UID along
             * with an optional groupname or GID. The following are all valid: {@code user}, {@code uid},
             * {@code user:group}, {@code uid:gid}, {@code uid:group}, {@code user:gid}.
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
             * Sets the working directory in the container.
             *
             * @param workingDirectory the working directory
             * @return this
             */
            public Builder setWorkingDirectory(AbsoluteUnixPath workingDirectory)
            {
                this.workingDirectory = workingDirectory;
                return this;
            }

            /**
             * Builds the {@link ContainerConfiguration}.
             *
             * @return the corresponding {@link ContainerConfiguration}
             */
            public ContainerConfiguration build()
            {
                return new ContainerConfiguration(
                    creationTime,
                    entrypoint,
                    programArguments,
                    environmentMap == null ? null : ImmutableDictionary.CreateRange(environmentMap),
                    exposedPorts == null ? null : ImmutableHashSet.CreateRange(exposedPorts),
                    volumes == null ? null : ImmutableHashSet.CreateRange(volumes),
                    labels == null ? null : ImmutableDictionary.CreateRange(labels),
                    user,
                    workingDirectory);
            }

            public Builder() { }
        }

        /**
         * Constructs a builder for a {@link ContainerConfiguration}.
         *
         * @return the builder
         */
        public static Builder builder()
        {
            return new Builder();
        }

        private readonly Instant creationTime;
        private readonly ImmutableArray<string>? entrypoint;
        private readonly ImmutableArray<string>? programArguments;
        private readonly ImmutableDictionary<string, string> environmentMap;
        private readonly ImmutableHashSet<Port> exposedPorts;
        private readonly ImmutableHashSet<AbsoluteUnixPath> volumes;
        private readonly ImmutableDictionary<string, string> labels;
        private readonly string user;
        private readonly AbsoluteUnixPath workingDirectory;

        private ContainerConfiguration(
            Instant creationTime,
            ImmutableArray<string>? entrypoint,
            ImmutableArray<string>? programArguments,
            ImmutableDictionary<string, string> environmentMap,
            ImmutableHashSet<Port> exposedPorts,
            ImmutableHashSet<AbsoluteUnixPath> volumes,
            ImmutableDictionary<string, string> labels,
            string user,
            AbsoluteUnixPath workingDirectory)
        {
            this.creationTime = creationTime;
            this.entrypoint = entrypoint;
            this.programArguments = programArguments;
            this.environmentMap = environmentMap;
            this.exposedPorts = exposedPorts;
            this.volumes = volumes;
            this.labels = labels;
            this.user = user;
            this.workingDirectory = workingDirectory;
        }

        public Instant getCreationTime()
        {
            return creationTime;
        }

        public ImmutableArray<string>? getEntrypoint()
        {
            return entrypoint;
        }

        public ImmutableArray<string>? getProgramArguments()
        {
            return programArguments;
        }

        public ImmutableDictionary<string, string> getEnvironmentMap()
        {
            return environmentMap;
        }

        public ImmutableHashSet<Port> getExposedPorts()
        {
            return exposedPorts;
        }

        public ImmutableHashSet<AbsoluteUnixPath> getVolumes()
        {
            return volumes;
        }

        public string getUser()
        {
            return user;
        }

        public ImmutableDictionary<string, string> getLabels()
        {
            return labels;
        }

        public AbsoluteUnixPath getWorkingDirectory()
        {
            return workingDirectory;
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (!(other is ContainerConfiguration))
            {
                return false;
            }
            ContainerConfiguration otherContainerConfiguration = (ContainerConfiguration)other;
            return creationTime.Equals(otherContainerConfiguration.creationTime)
                && Objects.Equals(entrypoint, otherContainerConfiguration.entrypoint)
                && Objects.Equals(programArguments, otherContainerConfiguration.programArguments)
                && Objects.Equals(environmentMap, otherContainerConfiguration.environmentMap)
                && Objects.Equals(exposedPorts, otherContainerConfiguration.exposedPorts)
                && Objects.Equals(labels, otherContainerConfiguration.labels)
                && Objects.Equals(user, otherContainerConfiguration.user)
                && Objects.Equals(workingDirectory, otherContainerConfiguration.workingDirectory);
        }

        public override int GetHashCode()
        {
            return Objects.hash(
                creationTime, entrypoint, programArguments, environmentMap, exposedPorts, labels, user);
        }
    }
}
