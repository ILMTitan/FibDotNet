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
using Jib.Net.Core;
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
        public static readonly Instant DefaultCreationTime = Instant.FromUnixTimeMilliseconds(0);

        /** Builder for instantiating a {@link ContainerConfiguration}. */
        public class Builder
        {
            private Instant creationTime = DefaultCreationTime;
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
            public Builder SetCreationTime(Instant creationTime)
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
            public Builder SetProgramArguments(IList<string> programArguments)
            {
                if (programArguments == null)
                {
                    this.programArguments = ImmutableArray<string>.Empty;
                }
                else
                {
                    Preconditions.CheckArgument(
                        !JavaExtensions.Contains(programArguments, null), "program arguments list contains null elements");
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
            public Builder SetEnvironment(IDictionary<string, string> environmentMap)
            {
                if (environmentMap == null)
                {
                    this.environmentMap = null;
                }
                else
                {
                    Preconditions.CheckArgument(
                        !Iterables.Any(environmentMap.KeySet(), Objects.IsNull),
                        "environment map contains null keys");
                    Preconditions.CheckArgument(
                        !Iterables.Any(environmentMap.Values(), Objects.IsNull),
                        "environment map contains null values");
                    this.environmentMap = new Dictionary<string, string>(environmentMap);
                }
                return this;
            }

            public void AddEnvironment(string name, string value)
            {
                (environmentMap ?? (environmentMap = new Dictionary<string, string>())).Put(name, value);
            }

            /**
             * Sets the container's exposed ports.
             *
             * @param exposedPorts the set of ports
             * @return this
             */
            public Builder SetExposedPorts(ISet<Port> exposedPorts)
            {
                if (exposedPorts == null)
                {
                    this.exposedPorts = null;
                }
                else
                {
                    Preconditions.CheckArgument(
                        !JavaExtensions.Contains(exposedPorts, null), "ports list contains null elements");
                    this.exposedPorts = new HashSet<Port>(exposedPorts);
                }
                return this;
            }

            public void AddExposedPort(Port port)
            {
                (exposedPorts ?? (exposedPorts = new HashSet<Port>())).Add(port);
            }

            /**
             * Sets the container's volumes.
             *
             * @param volumes the set of volumes
             * @return this
             */
            public Builder SetVolumes(ISet<AbsoluteUnixPath> volumes)
            {
                if (volumes == null)
                {
                    this.volumes = null;
                }
                else
                {
                    Preconditions.CheckArgument(!JavaExtensions.Contains(volumes, null), "volumes list contains null elements");
                    this.volumes = new HashSet<AbsoluteUnixPath>(volumes);
                }
                return this;
            }

            public void AddVolume(AbsoluteUnixPath volume)
            {
                (volumes ?? (volumes = new HashSet<AbsoluteUnixPath>())).Add(volume);
            }

            /**
             * Sets the container's labels.
             *
             * @param labels the map of labels
             * @return this
             */
            public Builder SetLabels(IDictionary<string, string> labels)
            {
                if (labels == null)
                {
                    this.labels = null;
                }
                else
                {
                    Preconditions.CheckArgument(
                        !Iterables.Any(labels.KeySet(), Objects.IsNull), "labels map contains null keys");
                    Preconditions.CheckArgument(
                        !Iterables.Any(labels.Values(), Objects.IsNull), "labels map contains null values");
                    this.labels = new Dictionary<string, string>(labels);
                }
                return this;
            }

            public void AddLabel(string key, string value)
            {
                (labels ?? (labels = new Dictionary<string, string>())).Put(key, value);
            }

            /**
             * Sets the container entrypoint.
             *
             * @param entrypoint the tokenized command to run when the container starts
             * @return this
             */
            public Builder SetEntrypoint(IList<string> entrypoint)
            {
                if (entrypoint == null)
                {
                    this.entrypoint = null;
                }
                else
                {
                    Preconditions.CheckArgument(
                        !JavaExtensions.Contains(entrypoint, null), "entrypoint contains null elements");
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
            public Builder SetUser(string user)
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
            public Builder SetWorkingDirectory(AbsoluteUnixPath workingDirectory)
            {
                this.workingDirectory = workingDirectory;
                return this;
            }

            /**
             * Builds the {@link ContainerConfiguration}.
             *
             * @return the corresponding {@link ContainerConfiguration}
             */
            public ContainerConfiguration Build()
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
        public static Builder CreateBuilder()
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

        public Instant GetCreationTime()
        {
            return creationTime;
        }

        public ImmutableArray<string>? GetEntrypoint()
        {
            return entrypoint;
        }

        public ImmutableArray<string>? GetProgramArguments()
        {
            return programArguments;
        }

        public ImmutableDictionary<string, string> GetEnvironmentMap()
        {
            return environmentMap;
        }

        public ImmutableHashSet<Port> GetExposedPorts()
        {
            return exposedPorts;
        }

        public ImmutableHashSet<AbsoluteUnixPath> GetVolumes()
        {
            return volumes;
        }

        public string GetUser()
        {
            return user;
        }

        public ImmutableDictionary<string, string> GetLabels()
        {
            return labels;
        }

        public AbsoluteUnixPath GetWorkingDirectory()
        {
            return workingDirectory;
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (!(other is ContainerConfiguration otherContainerConfiguration))
            {
                return false;
            }
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
            return Objects.Hash(
                creationTime, entrypoint, programArguments, environmentMap, exposedPorts, labels, user);
        }
    }
}
