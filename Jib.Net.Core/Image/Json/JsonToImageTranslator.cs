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
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using NodaTime;
using NodaTime.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace com.google.cloud.tools.jib.image.json {
























/** Translates {@link V21ManifestTemplate} and {@link V22ManifestTemplate} into {@link Image}. */
public class JsonToImageTranslator {

  /**
   * Pattern used for parsing information out of exposed port configurations. Only accepts single
   * ports with protocol.
   *
   * <p>Example matches: 100, 1000/tcp, 2000/udp
   */
  private static readonly Regex PORT_PATTERN =
      new Regex("(?<portNum>\\d+)(?:/(?<protocol>tcp|udp))?");

  /**
   * Pattern used for parsing environment variables in the format {@code NAME=VALUE}. {@code NAME}
   * should not contain an '='.
   *
   * <p>Example matches: NAME=VALUE, A12345=$$$$$
   */

  public static readonly Regex ENVIRONMENT_PATTERN = new Regex("(?<name>[^=]+)=(?<value>.*)");

  /**
   * Translates {@link V21ManifestTemplate} to {@link Image}.
   *
   * @param manifestTemplate the template containing the image layers.
   * @return the translated {@link Image}.
   * @throws LayerPropertyNotFoundException if adding image layers fails.
   * @throws BadContainerConfigurationFormatException if the container configuration is in a bad
   *     format
   */
  public static Image toImage(V21ManifestTemplate manifestTemplate)
      {
            Image.Builder imageBuilder = Image.builder((Class<V21ManifestTemplate>)typeof(V21ManifestTemplate));

    // V21 layers are in reverse order of V22. (The first layer is the latest one.)
    foreach (DescriptorDigest digest in Lists.reverse(manifestTemplate.getLayerDigests()))
    {
      imageBuilder.addLayer(new DigestOnlyLayer(digest));
    }

    if (manifestTemplate.getContainerConfiguration().isPresent()) {
      configureBuilderWithContainerConfiguration(
          imageBuilder, manifestTemplate.getContainerConfiguration().get());
    }
    return imageBuilder.build();
  }

  /**
   * Translates {@link BuildableManifestTemplate} to {@link Image}. Uses the corresponding {@link
   * ContainerConfigurationTemplate} to get the layer diff IDs.
   *
   * @param manifestTemplate the template containing the image layers.
   * @param containerConfigurationTemplate the template containing the diff IDs and container
   *     configuration properties.
   * @return the translated {@link Image}.
   * @throws LayerCountMismatchException if the manifest and configuration contain conflicting layer
   *     information.
   * @throws LayerPropertyNotFoundException if adding image layers fails.
   * @throws BadContainerConfigurationFormatException if the container configuration is in a bad
   *     format
   */
  public static Image toImage<T>(
      T manifestTemplate,
      ContainerConfigurationTemplate containerConfigurationTemplate) where T:BuildableManifestTemplate
      {
    IList<ReferenceNoDiffIdLayer> layers = new List<ReferenceNoDiffIdLayer>();
    foreach (ContentDescriptorTemplate layerObjectTemplate in
        manifestTemplate.getLayers()) {
      if (layerObjectTemplate.getDigest() == null) {
        throw new ArgumentException(
            "All layers in the manifest template must have digest set");
      }

      layers.add(
          new ReferenceNoDiffIdLayer(
              new BlobDescriptor(layerObjectTemplate.getSize(), layerObjectTemplate.getDigest())));
    }

    IList<DescriptorDigest> diffIds = containerConfigurationTemplate.getDiffIds();
    if (layers.size() != diffIds.size()) {
      throw new LayerCountMismatchException(
          "Mismatch between image manifest and container configuration");
    }

    Image.Builder imageBuilder = Image.builder((IClass<ManifestTemplate>)manifestTemplate.getClass());

    for (int layerIndex = 0; layerIndex < layers.size(); layerIndex++) {
      ReferenceNoDiffIdLayer noDiffIdLayer = layers.get(layerIndex);
      DescriptorDigest diffId = diffIds.get(layerIndex);

      imageBuilder.addLayer(new ReferenceLayer(noDiffIdLayer.getBlobDescriptor(), diffId));
    }

    configureBuilderWithContainerConfiguration(imageBuilder, containerConfigurationTemplate);
    return imageBuilder.build();
  }

  private static void configureBuilderWithContainerConfiguration(
      Image.Builder imageBuilder, ContainerConfigurationTemplate containerConfigurationTemplate)
      {
    containerConfigurationTemplate.getHistory().forEach(imageBuilder.addHistory);

    if (containerConfigurationTemplate.getCreated() != null) {
      try {
                    imageBuilder.setCreated(Instant.FromDateTimeOffset(DateTimeOffset.Parse(containerConfigurationTemplate.getCreated())));
      } catch (FormatException ex) {
        throw new BadContainerConfigurationFormatException(
            "Invalid image creation time: " + containerConfigurationTemplate.getCreated(), ex);
      }
    }

    if (containerConfigurationTemplate.getArchitecture() != null) {
      imageBuilder.setArchitecture(containerConfigurationTemplate.getArchitecture());
    }
    if (containerConfigurationTemplate.getOs() != null) {
      imageBuilder.setOs(containerConfigurationTemplate.getOs());
    }

    imageBuilder.setEntrypoint(containerConfigurationTemplate.getContainerEntrypoint());
    imageBuilder.setProgramArguments(containerConfigurationTemplate.getContainerCmd());

    IList<string> baseHealthCheckCommand = containerConfigurationTemplate.getContainerHealthTest();
    if (baseHealthCheckCommand != null) {
      DockerHealthCheck.Builder builder = DockerHealthCheck.fromCommand(baseHealthCheckCommand);
      if (containerConfigurationTemplate.getContainerHealthInterval() != null) {
        builder.setInterval(
            Duration.FromNanoseconds(containerConfigurationTemplate.getContainerHealthInterval().GetValueOrDefault()));
      }
      if (containerConfigurationTemplate.getContainerHealthTimeout() != null) {
        builder.setTimeout(
            Duration.FromNanoseconds(containerConfigurationTemplate.getContainerHealthTimeout().GetValueOrDefault()));
      }
      if (containerConfigurationTemplate.getContainerHealthStartPeriod() != null) {
        builder.setStartPeriod(
            Duration.FromNanoseconds(containerConfigurationTemplate.getContainerHealthStartPeriod().GetValueOrDefault()));
      }
      if (containerConfigurationTemplate.getContainerHealthRetries() != null) {
        builder.setRetries(containerConfigurationTemplate.getContainerHealthRetries().GetValueOrDefault());
      }
      imageBuilder.setHealthCheck(builder.build());
    }

    if (containerConfigurationTemplate.getContainerExposedPorts() != null) {
      imageBuilder.addExposedPorts(
          portMapToSet(containerConfigurationTemplate.getContainerExposedPorts()));
    }

    if (containerConfigurationTemplate.getContainerVolumes() != null) {
      imageBuilder.addVolumes(volumeMapToSet(containerConfigurationTemplate.getContainerVolumes()));
    }

    if (containerConfigurationTemplate.getContainerEnvironment() != null) {
      foreach (string environmentVariable in containerConfigurationTemplate.getContainerEnvironment())
      {
        Match matcher = ENVIRONMENT_PATTERN.matcher(environmentVariable);
        if (!matcher.matches()) {
          throw new BadContainerConfigurationFormatException(
              "Invalid environment variable definition: " + environmentVariable);
        }
        imageBuilder.addEnvironmentVariable(matcher.group("name"), matcher.group("value"));
      }
    }

    imageBuilder.addLabels(containerConfigurationTemplate.getContainerLabels());
    imageBuilder.setWorkingDirectory(containerConfigurationTemplate.getContainerWorkingDir());
    imageBuilder.setUser(containerConfigurationTemplate.getContainerUser());
  }

  /**
   * Converts a map of exposed ports as strings to a set of {@link Port}s (e.g. {@code
   * {"1000/tcp":{}}} => {@code Port(1000, Protocol.TCP)}).
   *
   * @param portMap the map to convert
   * @return a set of {@link Port}s
   */

  public static ImmutableHashSet<Port> portMapToSet(IDictionary<string, IDictionary<object, object>> portMap)
      {
    if (portMap == null) {
      return ImmutableHashSet.Create<Port>();
    }
    ImmutableHashSet<Port>.Builder ports = ImmutableHashSet.CreateBuilder<Port>();
    foreach (KeyValuePair<string, IDictionary<object, object>> entry in portMap.entrySet()) {
      string port = entry.getKey();
      Match matcher = PORT_PATTERN.matcher(port);
      if (!matcher.matches()) {
        throw new BadContainerConfigurationFormatException(
            "Invalid port configuration: '" + port + "'.");
      }

      int portNumber = int.Parse(matcher.group("portNum"));
      string protocol = matcher.group("protocol");
      ports.add(Port.parseProtocol(portNumber, protocol));
    }
    return ports.build();
  }

  /**
   * Converts a map of volumes strings to a set of {@link AbsoluteUnixPath}s (e.g. {@code {@code
   * {"/var/log/my-app-logs":{}}} => AbsoluteUnixPath().get("/var/log/my-app-logs")}).
   *
   * @param volumeMap the map to convert
   * @return a set of {@link AbsoluteUnixPath}s
   */

  public static ImmutableHashSet<AbsoluteUnixPath> volumeMapToSet(IDictionary<string, IDictionary<object, object>> volumeMap)
      {
    if (volumeMap == null) {
      return ImmutableHashSet.Create<AbsoluteUnixPath>();
    }

    ImmutableHashSet<AbsoluteUnixPath>.Builder volumeList = ImmutableHashSet.CreateBuilder<AbsoluteUnixPath>();
    foreach (string volume in volumeMap.keySet())
    {
      try {
        volumeList.add(AbsoluteUnixPath.get(volume));
      } catch (ArgumentException) {
        throw new BadContainerConfigurationFormatException("Invalid volume path: " + volume);
      }
    }

    return volumeList.build();
  }

  private JsonToImageTranslator() {}
}
}
