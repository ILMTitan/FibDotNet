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

namespace com.google.cloud.tools.jib.image {
















/** Represents an image. */
public class Image {

  /** Builds the immutable {@link Image}. */
  public static class Builder {

    private readonly Class<? extends ManifestTemplate> imageFormat;
    private readonly ImageLayers.Builder imageLayersBuilder = ImageLayers.builder();
    private readonly ImmutableList.Builder<HistoryEntry> historyBuilder = ImmutableList.builder();

    // Don't use ImmutableMap.Builder because it does not allow for replacing existing keys with new
    // values.
    private readonly Map<string, string> environmentBuilder = new HashMap<>();
    private readonly Map<string, string> labelsBuilder = new HashMap<>();
    private readonly Set<Port> exposedPortsBuilder = new HashSet<>();
    private readonly Set<AbsoluteUnixPath> volumesBuilder = new HashSet<>();

    private Instant created;
    private string architecture = "amd64";
    private string os = "linux";
    private ImmutableList<string> entrypoint;
    private ImmutableList<string> programArguments;
    private DockerHealthCheck healthCheck;
    private string workingDirectory;
    private string user;

    private Builder(Class<? extends ManifestTemplate> imageFormat) {
      this.imageFormat = imageFormat;
    }

    /**
     * Sets the image creation time.
     *
     * @param created the creation time
     * @return this
     */
    public Builder setCreated(Instant created) {
      this.created = created;
      return this;
    }

    /**
     * Sets the image architecture.
     *
     * @param architecture the architecture
     * @return this
     */
    public Builder setArchitecture(string architecture) {
      this.architecture = architecture;
      return this;
    }

    /**
     * Sets the image operating system.
     *
     * @param os the operating system
     * @return this
     */
    public Builder setOs(string os) {
      this.os = os;
      return this;
    }

    /**
     * Adds a map of environment variables to the current map.
     *
     * @param environment the map of environment variables
     * @return this
     */
    public Builder addEnvironment(Map<string, string> environment) {
      if (environment != null) {
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
    public Builder addEnvironmentVariable(string name, string value) {
      environmentBuilder.put(name, value);
      return this;
    }

    /**
     * Sets the entrypoint of the image.
     *
     * @param entrypoint the list of entrypoint tokens
     * @return this
     */
    public Builder setEntrypoint(List<string> entrypoint) {
      this.entrypoint = (entrypoint == null) ? null : ImmutableList.copyOf(entrypoint);
      return this;
    }

    /**
     * Sets the user/group to run the container as.
     *
     * @param user the username/UID and optionally the groupname/GID
     * @return this
     */
    public Builder setUser(string user) {
      this.user = user;
      return this;
    }

    /**
     * Sets the items in the "Cmd" field in the container configuration.
     *
     * @param programArguments the list of arguments to append to the image entrypoint
     * @return this
     */
    public Builder setProgramArguments(List<string> programArguments) {
      this.programArguments =
          (programArguments == null) ? null : ImmutableList.copyOf(programArguments);
      return this;
    }

    /**
     * Sets the container's healthcheck configuration.
     *
     * @param healthCheck the healthcheck configuration
     * @return this
     */
    public Builder setHealthCheck(DockerHealthCheck healthCheck) {
      this.healthCheck = healthCheck;
      return this;
    }

    /**
     * Adds items to the "ExposedPorts" field in the container configuration.
     *
     * @param exposedPorts the exposed ports to add
     * @return this
     */
    public Builder addExposedPorts(Set<Port> exposedPorts) {
      if (exposedPorts != null) {
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
    public Builder addVolumes(Set<AbsoluteUnixPath> volumes) {
      if (volumes != null) {
        volumesBuilder.addAll(ImmutableSet.copyOf(volumes));
      }
      return this;
    }

    /**
     * Adds items to the "Labels" field in the container configuration.
     *
     * @param labels the map of labels to add
     * @return this
     */
    public Builder addLabels(Map<string, string> labels) {
      if (labels != null) {
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
    public Builder addLabel(string name, string value) {
      labelsBuilder.put(name, value);
      return this;
    }

    /**
     * Sets the item in the "WorkingDir" field in the container configuration.
     *
     * @param workingDirectory the working directory
     * @return this
     */
    public Builder setWorkingDirectory(string workingDirectory) {
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
    public Builder addLayer(Layer layer) {
      imageLayersBuilder.add(layer);
      return this;
    }

    /**
     * Adds a history element to the image.
     *
     * @param history the history object to add
     * @return this
     */
    public Builder addHistory(HistoryEntry history) {
      historyBuilder.add(history);
      return this;
    }

    public Image build() {
      return new Image(
          imageFormat,
          created,
          architecture,
          os,
          imageLayersBuilder.build(),
          historyBuilder.build(),
          ImmutableMap.copyOf(environmentBuilder),
          entrypoint,
          programArguments,
          healthCheck,
          ImmutableSet.copyOf(exposedPortsBuilder),
          ImmutableSet.copyOf(volumesBuilder),
          ImmutableMap.copyOf(labelsBuilder),
          workingDirectory,
          user);
    }
  }

  public static Builder builder(Class<? extends ManifestTemplate> imageFormat) {
    return new Builder(imageFormat);
  }

  /** The image format. */
  private readonly Class<? extends ManifestTemplate> imageFormat;

  /** The image creation time. */
  private final Instant created;

  /** The image architecture. */
  private readonly string architecture;

  /** The image operating system. */
  private readonly string os;

  /** The layers of the image, in the order in which they are applied. */
  private readonly ImageLayers layers;

  /** The commands used to build each layer of the image */
  private readonly ImmutableList<HistoryEntry> history;

  /** Environment variable definitions for running the image, in the format {@code NAME=VALUE}. */
  private final ImmutableMap<string, string> environment;

  /** Initial command to run when running the image. */
  private final ImmutableList<string> entrypoint;

  /** Arguments to append to the image entrypoint when running the image. */
  private final ImmutableList<string> programArguments;

  /** Healthcheck configuration. */
  private final DockerHealthCheck healthCheck;

  /** Ports that the container listens on. */
  private final ImmutableSet<Port> exposedPorts;

  /** Directories to mount as volumes. */
  private final ImmutableSet<AbsoluteUnixPath> volumes;

  /** Labels on the container configuration */
  private final ImmutableMap<string, string> labels;

  /** Working directory on the container configuration */
  private final string workingDirectory;

  /** User on the container configuration */
  private final string user;

  private Image(
      Class<? extends ManifestTemplate> imageFormat,
      Instant created,
      string architecture,
      string os,
      ImageLayers layers,
      ImmutableList<HistoryEntry> history,
      ImmutableMap<string, string> environment,
      ImmutableList<string> entrypoint,
      ImmutableList<string> programArguments,
      DockerHealthCheck healthCheck,
      ImmutableSet<Port> exposedPorts,
      ImmutableSet<AbsoluteUnixPath> volumes,
      ImmutableMap<string, string> labels,
      string workingDirectory,
      string user) {
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

  public Class<? extends ManifestTemplate> getImageFormat() {
    return this.imageFormat;
  }

  public Instant getCreated() {
    return created;
  }

  public string getArchitecture() {
    return architecture;
  }

  public string getOs() {
    return os;
  }

  public ImmutableMap<string, string> getEnvironment() {
    return environment;
  }

  public ImmutableList<string> getEntrypoint() {
    return entrypoint;
  }

  public ImmutableList<string> getProgramArguments() {
    return programArguments;
  }

  public DockerHealthCheck getHealthCheck() {
    return healthCheck;
  }

  public ImmutableSet<Port> getExposedPorts() {
    return exposedPorts;
  }

  public ImmutableSet<AbsoluteUnixPath> getVolumes() {
    return volumes;
  }

  public ImmutableMap<string, string> getLabels() {
    return labels;
  }

  public string getWorkingDirectory() {
    return workingDirectory;
  }

  public string getUser() {
    return user;
  }

  public ImmutableList<Layer> getLayers() {
    return layers.getLayers();
  }

  public ImmutableList<HistoryEntry> getHistory() {
    return history;
  }
}
}
