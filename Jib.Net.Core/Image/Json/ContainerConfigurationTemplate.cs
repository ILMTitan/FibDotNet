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
  private readonly List<HistoryEntry> history = new ArrayList<>();

  /** Layer content digests that are used to build the container filesystem. */
  private readonly RootFilesystemObjectTemplate rootfs = new RootFilesystemObjectTemplate();

  /** Template for inner JSON object representing the configuration for running the container. */
  [JsonIgnoreProperties(ignoreUnknown = true)]
  private class ConfigurationObjectTemplate : JsonTemplate  {
    /** Environment variables in the format {@code VARNAME=VARVALUE}. */
    private List<string> Env;

    /** Command to run when container starts. */
    private List<string> Entrypoint;

    /** Arguments to pass into main. */
    private List<string> Cmd;

    /** Healthcheck. */
    private HealthCheckObjectTemplate Healthcheck;

    /** Network ports the container exposes. */
    private Map<string, Map<object, object>> ExposedPorts;

    /** Labels. */
    private Map<string, string> Labels;

    /** Working directory. */
    private string WorkingDir;

    /** User. */
    private string User;

    /** Volumes */
    private Map<string, Map<object, object>> Volumes;
  }

  /** Template for inner JSON object representing the healthcheck configuration. */
  private class HealthCheckObjectTemplate : JsonTemplate  {
    /** The test to perform to check that the container is healthy. */
    private List<string> Test;

    /** Number of nanoseconds to wait between probe attempts. */
    private Long Interval;

    /** Number of nanoseconds to wait before considering the check to have hung. */
    private Long Timeout;

    /**
     * Number of nanoseconds to wait for the container to initialize before starting health-retries.
     */
    private Long StartPeriod;

    /** The number of consecutive failures needed to consider the container as unhealthy. */
    private Integer Retries;
  }

  /**
   * Template for inner JSON object representing the filesystem changesets used to build the
   * container filesystem.
   */
  private class RootFilesystemObjectTemplate : JsonTemplate  {
    /** The type must always be {@code "layers"}. */
    private readonly string type = "layers";

    /**
     * The in-order list of layer content digests (hashes of the uncompressed partial filesystem
     * changeset).
     */
    private readonly List<DescriptorDigest> diff_ids = new ArrayList<>();
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

  public void setContainerEnvironment(List<string> environment) {
    config.Env = environment;
  }

  public void setContainerEntrypoint(List<string> command) {
    config.Entrypoint = command;
  }

  public void setContainerCmd(List<string> cmd) {
    config.Cmd = cmd;
  }

  public void setContainerHealthCheckTest(List<string> test) {
    if (config.Healthcheck == null) {
      config.Healthcheck = new HealthCheckObjectTemplate();
    }
    Preconditions.checkNotNull(config.Healthcheck).Test = test;
  }

  public void setContainerHealthCheckInterval(Long interval) {
    if (config.Healthcheck == null) {
      config.Healthcheck = new HealthCheckObjectTemplate();
    }
    Preconditions.checkNotNull(config.Healthcheck).Interval = interval;
  }

  public void setContainerHealthCheckTimeout(Long timeout) {
    if (config.Healthcheck == null) {
      config.Healthcheck = new HealthCheckObjectTemplate();
    }
    Preconditions.checkNotNull(config.Healthcheck).Timeout = timeout;
  }

  public void setContainerHealthCheckStartPeriod(Long startPeriod) {
    if (config.Healthcheck == null) {
      config.Healthcheck = new HealthCheckObjectTemplate();
    }
    Preconditions.checkNotNull(config.Healthcheck).StartPeriod = startPeriod;
  }

  public void setContainerHealthCheckRetries(Integer retries) {
    if (config.Healthcheck == null) {
      config.Healthcheck = new HealthCheckObjectTemplate();
    }
    Preconditions.checkNotNull(config.Healthcheck).Retries = retries;
  }

  public void setContainerExposedPorts(Map<string, Map<object, object>> exposedPorts) {
    config.ExposedPorts = exposedPorts;
  }

  public void setContainerLabels(Map<string, string> labels) {
    config.Labels = labels;
  }

  public void setContainerWorkingDir(string workingDirectory) {
    config.WorkingDir = workingDirectory;
  }

  public void setContainerUser(string user) {
    config.User = user;
  }

  public void setContainerVolumes(Map<string, Map<object, object>> volumes) {
    config.Volumes = volumes;
  }

  public void addLayerDiffId(DescriptorDigest diffId) {
    rootfs.diff_ids.add(diffId);
  }

  public void addHistoryEntry(HistoryEntry historyEntry) {
    history.add(historyEntry);
  }

  List<DescriptorDigest> getDiffIds() {
    return rootfs.diff_ids;
  }

  List<HistoryEntry> getHistory() {
    return history;
  }

  string getCreated() {
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

  List<string> getContainerEnvironment() {
    return config.Env;
  }

  List<string> getContainerEntrypoint() {
    return config.Entrypoint;
  }

  List<string> getContainerCmd() {
    return config.Cmd;
  }

  List<string> getContainerHealthTest() {
    return config.Healthcheck == null ? null : config.Healthcheck.Test;
  }

  Long getContainerHealthInterval() {
    return config.Healthcheck == null ? null : config.Healthcheck.Interval;
  }

  Long getContainerHealthTimeout() {
    return config.Healthcheck == null ? null : config.Healthcheck.Timeout;
  }

  Long getContainerHealthStartPeriod() {
    return config.Healthcheck == null ? null : config.Healthcheck.StartPeriod;
  }

  Integer getContainerHealthRetries() {
    return config.Healthcheck == null ? null : config.Healthcheck.Retries;
  }

  Map<string, Map<object, object>> getContainerExposedPorts() {
    return config.ExposedPorts;
  }

  Map<string, string> getContainerLabels() {
    return config.Labels;
  }

  string getContainerWorkingDir() {
    return config.WorkingDir;
  }

  string getContainerUser() {
    return config.User;
  }

  Map<string, Map<object, object>> getContainerVolumes() {
    return config.Volumes;
  }

  DescriptorDigest getLayerDiffId(int index) {
    return rootfs.diff_ids.get(index);
  }
}
}
