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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fib.Net.Core.Images.Json
{
    /**
     * JSON Template for Docker Container Configuration referenced in Docker Manifest Schema V2.2
     *
     * <p>Example container config JSON:
     *
     * <pre>{@code
     * {
     *   "created": "1970-01-01T00:00:00Z",
     *   "architecture": "amd64",
     *   "os": "linux",
     *   "config": {
     *     "Env": ["/usr/bin/java"],
     *     "Entrypoint": ["PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"],
     *     "Cmd": ["arg1", "arg2"],
     *     "Healthcheck": {
     *       "Test": ["CMD-SHELL", "/usr/bin/check-health localhost"],
     *       "Interval": 30000000000,
     *       "Timeout": 10000000000,
     *       "StartPeriod": 0,
     *       "Retries": 3
     *     }
     *     "ExposedPorts": { "6000/tcp":{}, "8000/tcp":{}, "9000/tcp":{} },
     *     "Volumes":{"/var/job-result-data":{},"/var/log/my-app-logs":{}}},
     *     "Labels": { "com.example.label": "value" },
     *     "WorkingDir": "/home/user/workspace",
     *     "User": "me"
     *   },
     *   "history": [
     *     {
     *       "author": "Fib",
     *       "created": "1970-01-01T00:00:00Z",
     *       "created_by": "fib"
     *     },
     *     {
     *       "author": "Fib",
     *       "created": "1970-01-01T00:00:00Z",
     *       "created_by": "fib"
     *     }
     *   ]
     *   "rootfs": {
     *     "diff_ids": [
     *       "sha256:2aebd096e0e237b447781353379722157e6c2d434b9ec5a0d63f2a6f07cf90c2",
     *       "sha256:5f70bf18a086007016e948b04aed3b82103a36bea41755b6cddfaf10ace3c6ef",
     *     ],
     *     "type": "layers"
     *   }
     * }
     * }</pre>
     *
     * @see <a href="https://docs.docker.com/registry/spec/manifest-v2-2/">Image Manifest Version 2,
     *     Schema 2</a>
     */
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ContainerConfigurationTemplate
    {
        /** ISO-8601 formatted combined date and time at which the image was created. */
        public string Created { get; set; }

        /** The CPU architecture to run the binaries in this container. See the <a
         * href="https://github.com/opencontainers/image-spec/blob/master/config.md#properties">OCI Image
         * Configuration specification</a> for acceptable values.*/
        public string Architecture { get; set; } = "amd64";

        /** The operating system to run the container on.
         *  See the <a href="https://github.com/opencontainers/image-spec/blob/master/config.md#properties">OCI Image
         *  Configuration specification</a> for acceptable values.
         */
        public string Os { get; set; } = "linux";

        /** Execution parameters that should be used as a base when running the container. */
        public ConfigurationObjectTemplate Config { get; } = new ConfigurationObjectTemplate();

        /** Describes the history of each layer. */
        public IList<HistoryEntry> History { get; } = new List<HistoryEntry>();

        /** Layer content digests that are used to build the container filesystem. */
        public RootFilesystemObjectTemplate Rootfs { get; } = new RootFilesystemObjectTemplate();

        /** Template for inner JSON object representing the configuration for running the container. */
        [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        public class ConfigurationObjectTemplate
        {
            /** Environment variables in the format {@code VARNAME=VARVALUE}. */
            public IList<string> Env { get; set; }

            /** Command to run when container starts. */
            public IList<string> Entrypoint { get; set; }

            /** Arguments to pass into main. */
            public IList<string> Cmd { get; set; }

            /** Healthcheck. */
            public HealthCheckObjectTemplate Healthcheck { get; set; }

            /** Network ports the container exposes. */
            public IDictionary<string, IDictionary<object, object>> ExposedPorts { get; set; }

            /** Labels. */
            public ImmutableSortedDictionary<string, string> Labels { get; set; }

            /** Working directory. */
            public string WorkingDir { get; set; }

            /** User. */
            public string User { get; set; }

            /** Volumes */
            public ImmutableSortedDictionary<string, IDictionary<object, object>> Volumes { get; set; }

            public ConfigurationObjectTemplate() { }

            [JsonConstructor]
            public ConfigurationObjectTemplate(
                IList<string> env,
                IList<string> entrypoint,
                IList<string> cmd,
                HealthCheckObjectTemplate healthcheck,
                IDictionary<string, IDictionary<object, object>> exposedPorts,
                ImmutableSortedDictionary<string, string> labels,
                string workingDir,
                string user,
                ImmutableSortedDictionary<string,
                    IDictionary<object, object>> volumes)
            {
                Env = env;
                Entrypoint = entrypoint;
                Cmd = cmd;
                Healthcheck = healthcheck;
                ExposedPorts = exposedPorts;
                Labels = labels;
                WorkingDir = workingDir;
                User = user;
                Volumes = volumes;
            }
        }

        /** Template for inner JSON object representing the healthcheck configuration. */
        [JsonObject]
        public class HealthCheckObjectTemplate
        {
            /** The test to perform to check that the container is healthy. */
            public IList<string> Test { get; set; }

            /** Number of nanoseconds to wait between probe attempts. */
            public long Interval { get; set; }

            /** Number of nanoseconds to wait before considering the check to have hung. */
            public long Timeout { get; set; }

            /**
             * Number of nanoseconds to wait for the container to initialize before starting health-retries.
             */
            public long StartPeriod { get; set; }

            /** The number of consecutive failures needed to consider the container as unhealthy. */
            public int? Retries { get; set; }
        }

        /**
         * Template for inner JSON object representing the filesystem changesets used to build the
         * container filesystem.
         */
        [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
        public class RootFilesystemObjectTemplate
        {
            /** The type must always be {@code "layers"}. */
            public string Type { get; } = "layers";

            /**
             * The in-order list of layer content digests (hashes of the uncompressed partial filesystem
             * changeset).
             */
            public IList<DescriptorDigest> DiffIds { get; } = new List<DescriptorDigest>();
        }

        public void SetContainerEnvironment(IList<string> environment)
        {
            Config.Env = environment;
        }

        public void SetContainerEntrypoint(IList<string> command)
        {
            Config.Entrypoint = command;
        }

        public void SetContainerCmd(IList<string> cmd)
        {
            Config.Cmd = cmd;
        }

        public void SetContainerHealthCheckTest(IList<string> test)
        {
            if (Config.Healthcheck == null)
            {
                Config.Healthcheck = new HealthCheckObjectTemplate();
            }
            Preconditions.CheckNotNull(Config.Healthcheck).Test = test;
        }

        public void SetContainerHealthCheckInterval(long interval)
        {
            if (Config.Healthcheck == null)
            {
                Config.Healthcheck = new HealthCheckObjectTemplate();
            }
            Preconditions.CheckNotNull(Config.Healthcheck).Interval = interval;
        }

        public void SetContainerHealthCheckTimeout(long timeout)
        {
            if (Config.Healthcheck == null)
            {
                Config.Healthcheck = new HealthCheckObjectTemplate();
            }
            Preconditions.CheckNotNull(Config.Healthcheck).Timeout = timeout;
        }

        public void SetContainerHealthCheckStartPeriod(long startPeriod)
        {
            if (Config.Healthcheck == null)
            {
                Config.Healthcheck = new HealthCheckObjectTemplate();
            }
            Preconditions.CheckNotNull(Config.Healthcheck).StartPeriod = startPeriod;
        }

        public void SetContainerHealthCheckRetries(int? retries)
        {
            if (Config.Healthcheck == null)
            {
                Config.Healthcheck = new HealthCheckObjectTemplate();
            }
            Preconditions.CheckNotNull(Config.Healthcheck).Retries = retries;
        }

        public void SetContainerExposedPorts(IDictionary<string, IDictionary<object, object>> exposedPorts)
        {
            Config.ExposedPorts = exposedPorts;
        }

        public void SetContainerLabels(IDictionary<string, string> labels)
        {
            Config.Labels = labels.ToImmutableSortedDictionary();
        }

        public void SetContainerWorkingDir(string workingDirectory)
        {
            Config.WorkingDir = workingDirectory;
        }

        public void SetContainerUser(string user)
        {
            Config.User = user;
        }

        public void SetContainerVolumes(IDictionary<string, IDictionary<object, object>> volumes)
        {
            Config.Volumes = volumes.ToImmutableSortedDictionary();
        }

        public void AddLayerDiffId(DescriptorDigest diffId)
        {
            Rootfs.DiffIds.Add(diffId);
        }

        public void AddHistoryEntry(HistoryEntry historyEntry)
        {
            History.Add(historyEntry);
        }

        public IList<DescriptorDigest> GetDiffIds()
        {
            return Rootfs.DiffIds;
        }

        public IList<string> GetContainerEnvironment()
        {
            return Config.Env;
        }

        public IList<string> GetContainerEntrypoint()
        {
            return Config.Entrypoint;
        }

        public IList<string> GetContainerCmd()
        {
            return Config.Cmd;
        }

        public IList<string> GetContainerHealthTest()
        {
            return Config.Healthcheck?.Test;
        }

        public long? GetContainerHealthInterval()
        {
            return Config.Healthcheck?.Interval;
        }

        public long? GetContainerHealthTimeout()
        {
            return Config.Healthcheck?.Timeout;
        }

        public long? GetContainerHealthStartPeriod()
        {
            return Config.Healthcheck?.StartPeriod;
        }

        public int? GetContainerHealthRetries()
        {
            return Config.Healthcheck?.Retries;
        }

        public IDictionary<string, IDictionary<object, object>> GetContainerExposedPorts()
        {
            return Config.ExposedPorts;
        }

        public IDictionary<string, string> GetContainerLabels()
        {
            return Config.Labels;
        }

        public string GetContainerWorkingDir()
        {
            return Config.WorkingDir;
        }

        public string GetContainerUser()
        {
            return Config.User;
        }

        public IDictionary<string, IDictionary<object, object>> GetContainerVolumes()
        {
            return Config.Volumes;
        }

        public DescriptorDigest GetLayerDiffId(int index)
        {
            return Rootfs.DiffIds[index];
        }
    }
}
