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
using Fib.Net.Core.Blob;
using Fib.Net.Core.Configuration;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Fib.Net.Core.Images.Json
{
    /** Translates {@link V21ManifestTemplate} and {@link V22ManifestTemplate} into {@link Image}. */
    public sealed class JsonToImageTranslator
    {
        /**
         * Pattern used for parsing information out of exposed port configurations. Only accepts single
         * ports with protocol.
         *
         * <p>Example matches: 100, 1000/tcp, 2000/udp
         */
        private static readonly Regex PORT_PATTERN =
            new Regex("^(?<portNum>\\d+)(?:/(?<protocol>tcp|udp))?$");

        /**
         * Pattern used for parsing environment variables in the format {@code NAME=VALUE}. {@code NAME}
         * should not contain an '='.
         *
         * <p>Example matches: NAME=VALUE, A12345=$$$$$
         */

        public static readonly Regex EnvironmentPattern = new Regex("^(?<name>[^=]+)=(?<value>.*)$");

        /**
         * Translates {@link V21ManifestTemplate} to {@link Image}.
         *
         * @param manifestTemplate the template containing the image layers.
         * @return the translated {@link Image}.
         * @throws LayerPropertyNotFoundException if adding image layers fails.
         * @throws BadContainerConfigurationFormatException if the container configuration is in a bad
         *     format
         */
        public static Image ToImage(V21ManifestTemplate manifestTemplate)
        {
            manifestTemplate = manifestTemplate ?? throw new ArgumentNullException(nameof(manifestTemplate));
            Image.Builder imageBuilder = Image.CreateBuilder(ManifestFormat.V21);

            // V21 layers are in reverse order of V22. (The first layer is the latest one.)
            foreach (DescriptorDigest digest in manifestTemplate.GetLayerDigests().ToImmutableList().Reverse())
            {
                imageBuilder.AddLayer(new DigestOnlyLayer(digest));
            }

            if (manifestTemplate.GetContainerConfiguration().IsPresent())
            {
                ConfigureBuilderWithContainerConfiguration(
                    imageBuilder, manifestTemplate.GetContainerConfiguration().Get());
            }
            return imageBuilder.Build();
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
        public static Image ToImage<T>(
            T manifestTemplate,
            ContainerConfigurationTemplate containerConfigurationTemplate) where T : IBuildableManifestTemplate
        {
            containerConfigurationTemplate =
                containerConfigurationTemplate
                ?? throw new ArgumentNullException(nameof(containerConfigurationTemplate));
            IList<ReferenceNoDiffIdLayer> layers = new List<ReferenceNoDiffIdLayer>();
            foreach (ContentDescriptorTemplate layerObjectTemplate in
                manifestTemplate.Layers)
            {
                if (layerObjectTemplate.Digest== null)
                {
                    throw new ArgumentException(Resources.JsonToImageTranslatorMissingDigestExceptionMessage);
                }

                layers.Add(new ReferenceNoDiffIdLayer(
                        new BlobDescriptor(layerObjectTemplate.Size, layerObjectTemplate.Digest)));
            }

            IList<DescriptorDigest> diffIds = containerConfigurationTemplate.GetDiffIds();
            if (layers.Count != diffIds.Count)
            {
                throw new LayerCountMismatchException(Resources.JsonToImageTranslatorDiffIdMismatchExceptionMessage);
            }

            Image.Builder imageBuilder = Image.CreateBuilder(manifestTemplate.GetFormat());

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                ReferenceNoDiffIdLayer noDiffIdLayer = layers[layerIndex];
                DescriptorDigest diffId = diffIds[layerIndex];

                imageBuilder.AddLayer(new ReferenceLayer(noDiffIdLayer.GetBlobDescriptor(), diffId));
            }

            ConfigureBuilderWithContainerConfiguration(imageBuilder, containerConfigurationTemplate);
            return imageBuilder.Build();
        }

        private static void ConfigureBuilderWithContainerConfiguration(
            Image.Builder imageBuilder, ContainerConfigurationTemplate containerConfigurationTemplate)
        {
            foreach (HistoryEntry i in containerConfigurationTemplate.History)
            {
                imageBuilder.AddHistory(i);
            }
            if (containerConfigurationTemplate.Created!= null)
            {
                try
                {
                    imageBuilder.SetCreated(
                        Instant.FromDateTimeOffset(
                            DateTimeOffset.Parse(
                                containerConfigurationTemplate.Created,
                                CultureInfo.InvariantCulture)));
                }
                catch (FormatException ex)
                {
                    throw new BadContainerConfigurationFormatException(
                        "Invalid image creation time: " + containerConfigurationTemplate.Created, ex);
                }
            }

            if (containerConfigurationTemplate.Architecture!= null)
            {
                imageBuilder.SetArchitecture(containerConfigurationTemplate.Architecture);
            }
            if (containerConfigurationTemplate.Os!= null)
            {
                imageBuilder.SetOs(containerConfigurationTemplate.Os);
            }

            imageBuilder.SetEntrypoint(containerConfigurationTemplate.GetContainerEntrypoint());
            imageBuilder.SetProgramArguments(containerConfigurationTemplate.GetContainerCmd());

            IList<string> baseHealthCheckCommand = containerConfigurationTemplate.GetContainerHealthTest();
            if (baseHealthCheckCommand != null)
            {
                DockerHealthCheck.Builder builder = DockerHealthCheck.FromCommand(baseHealthCheckCommand);
                if (containerConfigurationTemplate.GetContainerHealthInterval() != null)
                {
                    builder.SetInterval(
                        Duration.FromNanoseconds(containerConfigurationTemplate.GetContainerHealthInterval().GetValueOrDefault()));
                }
                if (containerConfigurationTemplate.GetContainerHealthTimeout() != null)
                {
                    builder.SetTimeout(
                        Duration.FromNanoseconds(containerConfigurationTemplate.GetContainerHealthTimeout().GetValueOrDefault()));
                }
                if (containerConfigurationTemplate.GetContainerHealthStartPeriod() != null)
                {
                    builder.SetStartPeriod(
                        Duration.FromNanoseconds(containerConfigurationTemplate.GetContainerHealthStartPeriod().GetValueOrDefault()));
                }
                if (containerConfigurationTemplate.GetContainerHealthRetries() != null)
                {
                    builder.SetRetries(containerConfigurationTemplate.GetContainerHealthRetries().GetValueOrDefault());
                }
                imageBuilder.SetHealthCheck(builder.Build());
            }

            if (containerConfigurationTemplate.GetContainerExposedPorts() != null)
            {
                imageBuilder.AddExposedPorts(
                    PortMapToSet(containerConfigurationTemplate.GetContainerExposedPorts()));
            }

            if (containerConfigurationTemplate.GetContainerVolumes() != null)
            {
                imageBuilder.AddVolumes(VolumeMapToSet(containerConfigurationTemplate.GetContainerVolumes()));
            }

            if (containerConfigurationTemplate.GetContainerEnvironment() != null)
            {
                foreach (string environmentVariable in containerConfigurationTemplate.GetContainerEnvironment())
                {
                    Match matcher = EnvironmentPattern.Match(environmentVariable);
                    if (!matcher.Success)
                    {
                        throw new BadContainerConfigurationFormatException(
                            "Invalid environment variable definition: " + environmentVariable);
                    }
                    imageBuilder.AddEnvironmentVariable(matcher.Groups["name"].Value, matcher.Groups["value"].Value);
                }
            }

            imageBuilder.AddLabels(containerConfigurationTemplate.GetContainerLabels());
            imageBuilder.SetWorkingDirectory(containerConfigurationTemplate.GetContainerWorkingDir());
            imageBuilder.SetUser(containerConfigurationTemplate.GetContainerUser());
        }

        /**
         * Converts a map of exposed ports as strings to a set of {@link Port}s (e.g. {@code
         * {"1000/tcp":{}}} => {@code Port(1000, Protocol.TCP)}).
         *
         * @param portMap the map to convert
         * @return a set of {@link Port}s
         */

        public static ImmutableHashSet<Port> PortMapToSet(IDictionary<string, IDictionary<object, object>> portMap)
        {
            if (portMap == null)
            {
                return ImmutableHashSet.Create<Port>();
            }
            ImmutableHashSet<Port>.Builder ports = ImmutableHashSet.CreateBuilder<Port>();
            foreach (KeyValuePair<string, IDictionary<object, object>> entry in portMap)
            {
                string port = entry.Key;
                Match matcher = PORT_PATTERN.Match(port);
                if (!matcher.Success)
                {
                    throw new BadContainerConfigurationFormatException(
                        "Invalid port configuration: '" + port + "'.");
                }

                int portNumber = int.Parse(matcher.Groups["portNum"].Value, CultureInfo.InvariantCulture);
                string protocol = matcher.Groups["protocol"].Value;
                ports.Add(Port.ParseProtocol(portNumber, protocol));
            }
            return ports.ToImmutable();
        }

        /**
         * Converts a map of volumes strings to a set of {@link AbsoluteUnixPath}s (e.g. {@code {@code
         * {"/var/log/my-app-logs":{}}} => AbsoluteUnixPath().get("/var/log/my-app-logs")}).
         *
         * @param volumeMap the map to convert
         * @return a set of {@link AbsoluteUnixPath}s
         */

        public static ImmutableHashSet<AbsoluteUnixPath> VolumeMapToSet(IDictionary<string, IDictionary<object, object>> volumeMap)
        {
            if (volumeMap == null)
            {
                return ImmutableHashSet.Create<AbsoluteUnixPath>();
            }

            ImmutableHashSet<AbsoluteUnixPath>.Builder volumeList = ImmutableHashSet.CreateBuilder<AbsoluteUnixPath>();
            foreach (string volume in volumeMap.Keys)
            {
                try
                {
                    volumeList.Add(AbsoluteUnixPath.Get(volume));
                }
                catch (ArgumentException)
                {
                    throw new BadContainerConfigurationFormatException("Invalid volume path: " + volume);
                }
            }

            return volumeList.ToImmutable();
        }

        private JsonToImageTranslator() { }
    }
}
