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

using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.image.json {










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
 *       "author": "Jib",
 *       "created": "1970-01-01T00:00:00Z",
 *       "created_by": "jib"
 *     },
 *     {
 *       "author": "Jib",
 *       "created": "1970-01-01T00:00:00Z",
 *       "created_by": "jib"
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
[JsonIgnoreProperties(ignoreUnknown = true)]
public class ContainerConfigurationTemplate : JsonTemplate {

  /** ISO-8601 formatted combined date and time at which the image was created. */
  private string created;

  /** The CPU architecture to run the binaries in this container. */
  private string architecture = "amd64";

  /** The operating system to run the container on. */
  private string os = "linux";

  /** Execution parameters that should be used as a base when running the container. */
  private readonly ConfigurationObjectTemplate config = new ConfigurationObjectTemplate();

  /** Describes the history of each layer. */
  private readonly IList<HistoryEntry> history = new List<HistoryEntry>();

  /** Layer content digests that are used to build the container filesystem. */
  private readonly RootFilesystemObjectTemplate rootfs = new RootFilesystemObjectTemplate();

  /** Template for inner JSON object representing the configuration for running the container. */
  [JsonIgnoreProperties(ignoreUnknown = true)]
  private class ConfigurationObjectTemplate : JsonTemplate  {
    /** Environment variables in the format {@code VARNAME=VARVALUE}. */
    public IList<string> Env;

    /** Command to run when container starts. */
    public IList<string> Entrypoint;

    /** Arguments to pass into main. */
    public IList<string> Cmd;

    /** Healthcheck. */
    public HealthCheckObjectTemplate Healthcheck;

    /** Network ports the container exposes. */
    public IDictionary<string, IDictionary<object, object>> ExposedPorts;

    /** Labels. */
    public IDictionary<string, string> Labels;

    /** Working directory. */
    public string WorkingDir;

    /** User. */
    public string User;

    /** Volumes */
    public IDictionary<string, IDictionary<object, object>> Volumes;
  }

  /** Template for inner JSON object representing the healthcheck configuration. */
  private class HealthCheckObjectTemplate : JsonTemplate  {
    /** The test to perform to check that the container is healthy. */
    public IList<string> Test;

    /** Number of nanoseconds to wait between probe attempts. */
    public long Interval;

    /** Number of nanoseconds to wait before considering the check to have hung. */
    public long Timeout;

    /**
     * Number of nanoseconds to wait for the container to initialize before starting health-retries.
     */
    public long StartPeriod;

    /** The number of consecutive failures needed to consider the container as unhealthy. */
    public int? Retries;
  }

  /**
   * Template for inner JSON object representing the filesystem changesets used to build the
   * container filesystem.
   */
  private class RootFilesystemObjectTemplate : JsonTemplate  {
    /** The type must always be {@code "layers"}. */
    [JsonProperty]
    public string type => "layers";

    /**
     * The in-order list of layer content digests (hashes of the uncompressed partial filesystem
     * changeset).
     */
    public readonly IList<DescriptorDigest> diff_ids = new List<DescriptorDigest>();
  }

  public void setCreated(string created) {
    this.created = created;
  }

  /**
   * Sets the architecture for which this container was built. See the <a
   * href="https://github.com/opencontainers/image-spec/blob/master/config.md#properties">OCI Image
   * Configuration specification</a> for acceptable values.
   *
   * @param architecture value for the {@code architecture} field
   */
  public void setArchitecture(string architecture) {
    this.architecture = architecture;
  }

  /**
   * Sets the operating system for which this container was built. See the <a
   * href="https://github.com/opencontainers/image-spec/blob/master/config.md#properties">OCI Image
   * Configuration specification</a> for acceptable values.
   *
   * @param os value for the {@code os} field
   */
  public void setOs(string os) {
    this.os = os;
  }

  public void setContainerEnvironment(IList<string> environment) {
    config.Env = environment;
  }

  public void setContainerEntrypoint(IList<string> command) {
    config.Entrypoint = command;
  }

  public void setContainerCmd(IList<string> cmd) {
    config.Cmd = cmd;
  }

  public void setContainerHealthCheckTest(IList<string> test) {
    if (config.Healthcheck == null) {
      config.Healthcheck = new HealthCheckObjectTemplate();
    }
    Preconditions.checkNotNull(config.Healthcheck).Test = test;
  }

  public void setContainerHealthCheckInterval(long interval) {
    if (config.Healthcheck == null) {
      config.Healthcheck = new HealthCheckObjectTemplate();
    }
    Preconditions.checkNotNull(config.Healthcheck).Interval = interval;
  }

  public void setContainerHealthCheckTimeout(long timeout) {
    if (config.Healthcheck == null) {
      config.Healthcheck = new HealthCheckObjectTemplate();
    }
    Preconditions.checkNotNull(config.Healthcheck).Timeout = timeout;
  }

  public void setContainerHealthCheckStartPeriod(long startPeriod) {
    if (config.Healthcheck == null) {
      config.Healthcheck = new HealthCheckObjectTemplate();
    }
    Preconditions.checkNotNull(config.Healthcheck).StartPeriod = startPeriod;
  }

  public void setContainerHealthCheckRetries(int? retries) {
    if (config.Healthcheck == null) {
      config.Healthcheck = new HealthCheckObjectTemplate();
    }
    Preconditions.checkNotNull(config.Healthcheck).Retries = retries;
  }

  public void setContainerExposedPorts(IDictionary<string, IDictionary<object, object>> exposedPorts) {
    config.ExposedPorts = exposedPorts;
  }

  public void setContainerLabels(IDictionary<string, string> labels) {
    config.Labels = labels;
  }

  public void setContainerWorkingDir(string workingDirectory) {
    config.WorkingDir = workingDirectory;
  }

  public void setContainerUser(string user) {
    config.User = user;
  }

  public void setContainerVolumes(IDictionary<string, IDictionary<object, object>> volumes) {
    config.Volumes = volumes;
  }

  public void addLayerDiffId(DescriptorDigest diffId) {
    rootfs.diff_ids.add(diffId);
  }

  public void addHistoryEntry(HistoryEntry historyEntry) {
    history.add(historyEntry);
  }

  public IList<DescriptorDigest> getDiffIds() {
    return rootfs.diff_ids;
  }

  public IList<HistoryEntry> getHistory() {
    return history;
  }

  public string getCreated() {
    return created;
  }

  /**
   * Returns the architecture for which this container was built. See the <a
   * href="https://github.com/opencontainers/image-spec/blob/master/config.md#properties">OCI Image
   * Configuration specification</a> for acceptable values.
   *
   * @return the {@code architecture} field
   */
  public string getArchitecture() {
    return architecture;
  }

  /**
   * Returns the operating system for which this container was built. See the <a
   * href="https://github.com/opencontainers/image-spec/blob/master/config.md#properties">OCI Image
   * Configuration specification</a> for acceptable values.
   *
   * @return the {@code os} field
   */
  public string getOs() {
    return os;
  }

  public IList<string> getContainerEnvironment() {
    return config.Env;
  }

  public IList<string> getContainerEntrypoint() {
    return config.Entrypoint;
  }

  public IList<string> getContainerCmd() {
    return config.Cmd;
  }

  public IList<string> getContainerHealthTest() {
    return config.Healthcheck?.Test;
  }

  public long? getContainerHealthInterval() {
    return config.Healthcheck?.Interval;
  }

  public long? getContainerHealthTimeout() {
    return config.Healthcheck?.Timeout;
  }

  public long? getContainerHealthStartPeriod() {
    return config.Healthcheck?.StartPeriod;
  }

  public int? getContainerHealthRetries() {
    return config.Healthcheck?.Retries;
  }

  public IDictionary<string, IDictionary<object, object>> getContainerExposedPorts() {
    return config.ExposedPorts;
  }

  public IDictionary<string, string> getContainerLabels() {
    return config.Labels;
  }

  public string getContainerWorkingDir() {
    return config.WorkingDir;
  }

  public string getContainerUser() {
    return config.User;
  }

  public IDictionary<string, IDictionary<object, object>> getContainerVolumes() {
    return config.Volumes;
  }

  public DescriptorDigest getLayerDiffId(int index) {
    return rootfs.diff_ids.get(index);
  }
}
}
