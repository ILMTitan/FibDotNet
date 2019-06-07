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

namespace com.google.cloud.tools.jib.configuration {

















/** Immutable configuration options for the container. */
public class ContainerConfiguration {

  /** The default creation time of the container (constant to ensure reproducibility by default). */
  public static readonly Instant DEFAULT_CREATION_TIME = Instant.EPOCH;

  /** Builder for instantiating a {@link ContainerConfiguration}. */
  public static class Builder {

    private Instant creationTime = DEFAULT_CREATION_TIME;
    private ImmutableList<string> entrypoint;
    private ImmutableList<string> programArguments;
    private Map<string, string> environmentMap;
    private Set<Port> exposedPorts;
    private Set<AbsoluteUnixPath> volumes;
    private Map<string, string> labels;
    private string user;
    private AbsoluteUnixPath workingDirectory;

    /**
     * Sets the image creation time.
     *
     * @param creationTime the creation time
     * @return this
     */
    public Builder setCreationTime(Instant creationTime) {
      this.creationTime = creationTime;
      return this;
    }

    /**
     * Sets the commandline arguments for main.
     *
     * @param programArguments the list of arguments
     * @return this
     */
    public Builder setProgramArguments(List<string> programArguments) {
      if (programArguments == null) {
        this.programArguments = null;
      } else {
        Preconditions.checkArgument(
            !programArguments.contains(null), "program arguments list contains null elements");
        this.programArguments = ImmutableList.copyOf(programArguments);
      }
      return this;
    }

    /**
     * Sets the container's environment variables, mapping variable name to value.
     *
     * @param environmentMap the map
     * @return this
     */
    public Builder setEnvironment(Map<string, string> environmentMap) {
      if (environmentMap == null) {
        this.environmentMap = null;
      } else {
        Preconditions.checkArgument(
            !Iterables.any(environmentMap.keySet(), Objects.isNull),
            "environment map contains null keys");
        Preconditions.checkArgument(
            !Iterables.any(environmentMap.values(), Objects.isNull),
            "environment map contains null values");
        this.environmentMap = new HashMap<>(environmentMap);
      }
      return this;
    }

    public void addEnvironment(string name, string value) {
      if (environmentMap == null) {
        environmentMap = new HashMap<>();
      }
      environmentMap.put(name, value);
    }

    /**
     * Sets the container's exposed ports.
     *
     * @param exposedPorts the set of ports
     * @return this
     */
    public Builder setExposedPorts(Set<Port> exposedPorts) {
      if (exposedPorts == null) {
        this.exposedPorts = null;
      } else {
        Preconditions.checkArgument(
            !exposedPorts.contains(null), "ports list contains null elements");
        this.exposedPorts = new HashSet<>(exposedPorts);
      }
      return this;
    }

    public void addExposedPort(Port port) {
      if (exposedPorts == null) {
        exposedPorts = new HashSet<>();
      }
      exposedPorts.add(port);
    }

    /**
     * Sets the container's volumes.
     *
     * @param volumes the set of volumes
     * @return this
     */
    public Builder setVolumes(Set<AbsoluteUnixPath> volumes) {
      if (volumes == null) {
        this.volumes = null;
      } else {
        Preconditions.checkArgument(!volumes.contains(null), "volumes list contains null elements");
        this.volumes = new HashSet<>(volumes);
      }
      return this;
    }

    public void addVolume(AbsoluteUnixPath volume) {
      if (volumes == null) {
        volumes = new HashSet<>();
      }
      volumes.add(volume);
    }

    /**
     * Sets the container's labels.
     *
     * @param labels the map of labels
     * @return this
     */
    public Builder setLabels(Map<string, string> labels) {
      if (labels == null) {
        this.labels = null;
      } else {
        Preconditions.checkArgument(
            !Iterables.any(labels.keySet(), Objects.isNull), "labels map contains null keys");
        Preconditions.checkArgument(
            !Iterables.any(labels.values(), Objects.isNull), "labels map contains null values");
        this.labels = new HashMap<>(labels);
      }
      return this;
    }

    public void addLabel(string key, string value) {
      if (labels == null) {
        labels = new HashMap<>();
      }
      labels.put(key, value);
    }

    /**
     * Sets the container entrypoint.
     *
     * @param entrypoint the tokenized command to run when the container starts
     * @return this
     */
    public Builder setEntrypoint(List<string> entrypoint) {
      if (entrypoint == null) {
        this.entrypoint = null;
      } else {
        Preconditions.checkArgument(
            !entrypoint.contains(null), "entrypoint contains null elements");
        this.entrypoint = ImmutableList.copyOf(entrypoint);
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
    public Builder setUser(string user) {
      this.user = user;
      return this;
    }

    /**
     * Sets the working directory in the container.
     *
     * @param workingDirectory the working directory
     * @return this
     */
    public Builder setWorkingDirectory(AbsoluteUnixPath workingDirectory) {
      this.workingDirectory = workingDirectory;
      return this;
    }

    /**
     * Builds the {@link ContainerConfiguration}.
     *
     * @return the corresponding {@link ContainerConfiguration}
     */
    public ContainerConfiguration build() {
      return new ContainerConfiguration(
          creationTime,
          entrypoint,
          programArguments,
          environmentMap == null ? null : ImmutableMap.copyOf(environmentMap),
          exposedPorts == null ? null : ImmutableSet.copyOf(exposedPorts),
          volumes == null ? null : ImmutableSet.copyOf(volumes),
          labels == null ? null : ImmutableMap.copyOf(labels),
          user,
          workingDirectory);
    }

    private Builder() {}
  }

  /**
   * Constructs a builder for a {@link ContainerConfiguration}.
   *
   * @return the builder
   */
  public static Builder builder() {
    return new Builder();
  }

  private readonly Instant creationTime;
  private final ImmutableList<string> entrypoint;
  private final ImmutableList<string> programArguments;
  private final ImmutableMap<string, string> environmentMap;
  private final ImmutableSet<Port> exposedPorts;
  private final ImmutableSet<AbsoluteUnixPath> volumes;
  private final ImmutableMap<string, string> labels;
  private final string user;
  private final AbsoluteUnixPath workingDirectory;

  private ContainerConfiguration(
      Instant creationTime,
      ImmutableList<string> entrypoint,
      ImmutableList<string> programArguments,
      ImmutableMap<string, string> environmentMap,
      ImmutableSet<Port> exposedPorts,
      ImmutableSet<AbsoluteUnixPath> volumes,
      ImmutableMap<string, string> labels,
      string user,
      AbsoluteUnixPath workingDirectory) {
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

  public Instant getCreationTime() {
    return creationTime;
  }

  public ImmutableList<string> getEntrypoint() {
    return entrypoint;
  }

  public ImmutableList<string> getProgramArguments() {
    return programArguments;
  }

  public ImmutableMap<string, string> getEnvironmentMap() {
    return environmentMap;
  }

  public ImmutableSet<Port> getExposedPorts() {
    return exposedPorts;
  }

  public ImmutableSet<AbsoluteUnixPath> getVolumes() {
    return volumes;
  }

  public string getUser() {
    return user;
  }

  public ImmutableMap<string, string> getLabels() {
    return labels;
  }

  public AbsoluteUnixPath getWorkingDirectory() {
    return workingDirectory;
  }

  public bool equals(object other) {
    if (this == other) {
      return true;
    }
    if (!(other is ContainerConfiguration)) {
      return false;
    }
    ContainerConfiguration otherContainerConfiguration = (ContainerConfiguration) other;
    return creationTime.equals(otherContainerConfiguration.creationTime)
        && Objects.equals(entrypoint, otherContainerConfiguration.entrypoint)
        && Objects.equals(programArguments, otherContainerConfiguration.programArguments)
        && Objects.equals(environmentMap, otherContainerConfiguration.environmentMap)
        && Objects.equals(exposedPorts, otherContainerConfiguration.exposedPorts)
        && Objects.equals(labels, otherContainerConfiguration.labels)
        && Objects.equals(user, otherContainerConfiguration.user)
        && Objects.equals(workingDirectory, otherContainerConfiguration.workingDirectory);
  }

  public int hashCode() {
    return Objects.hash(
        creationTime, entrypoint, programArguments, environmentMap, exposedPorts, labels, user);
  }
}
}
