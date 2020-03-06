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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Fib.Net.Core.Images.Json
{
    /**
     * Translates an {@link Image} into a manifest or container configuration JSON BLOB.
     *
     * <p>Example usage:
     *
     * <pre>{@code
     * ImageToJsonTranslator translator = new ImageToJsonTranslator(image);
     * Blob containerConfigurationBlob = translator.getContainerConfigurationBlob();
     * BlobDescriptor containerConfigurationBlobDescriptor = blob.writeTo(innerStream);
     * Blob manifestBlob = translator.getManifestBlob(containerConfigurationBlobDescriptor);
     * }</pre>
     */
    public class ImageToJsonTranslator
    {
        /**
         * Converts a set of {@link Port}s to the corresponding container config format for exposed ports
         * (e.g. {@code Port(1000, Protocol.TCP)} => {@code {"1000/tcp":{}}}).
         *
         * @param exposedPorts the set of {@link Port}s to translate, or {@code null}
         * @return a sorted map with the string representation of the ports as keys and empty maps as
         *     values, or {@code null} if {@code exposedPorts} is {@code null}
         */

        public static IDictionary<string, IDictionary<object, object>> PortSetToMap(ISet<Port> exposedPorts)
        {
            return SetToMap(exposedPorts, port => port.GetPort() + "/" + port.GetProtocol());
        }

        /**
         * Converts a set of {@link AbsoluteUnixPath}s to the corresponding container config format for
         * volumes (e.g. {@code AbsoluteUnixPath().get("/var/log/my-app-logs")} => {@code
         * {"/var/log/my-app-logs":{}}}).
         *
         * @param volumes the set of {@link AbsoluteUnixPath}s to translate, or {@code null}
         * @return a sorted map with the string representation of the ports as keys and empty maps as
         *     values, or {@code null} if {@code exposedPorts} is {@code null}
         */

        public static ImmutableSortedDictionary<string, IDictionary<object, object>> VolumesSetToMap(ISet<AbsoluteUnixPath> volumes)
        {
            return SetToMap(volumes, p => p.ToString());
        }

        /**
         * Converts the map of environment variables to a list with items in the format "NAME=VALUE".
         *
         * @return the list
         */

        public static ImmutableArray<string> EnvironmentMapToList(IDictionary<string, string> environment)
        {
            if (environment == null)
            {
                return ImmutableArray<string>.Empty;
            }
            Preconditions.CheckArgument(
                !environment.Keys.Any(key => key.Contains("=")),
                "Illegal environment variable: name cannot contain '='");
            return environment
                .Select(entry => entry.Key + "=" + entry.Value)
                .OrderBy(i => i)
                .ToImmutableArray();
        }

        /**
         * Turns a set into a sorted map where each element of the set is mapped to an entry composed by
         * the key generated with {@code Func<E, string> elementMapper} and an empty map as value.
         *
         * <p>This method is needed because the volume object is a direct JSON serialization of the Go
         * type map[string]struct{} and is represented in JSON as an object mapping its keys to an empty
         * object.
         *
         * <p>Further read at the <a
         * href="https://github.com/opencontainers/image-spec/blob/master/config.md">image specs.</a>
         *
         * @param set the set of elements to be transformed
         * @param keyMapper the mapper function to generate keys to the map
         * @param <E> the type of the elements from the set
         * @return an map
         */
        private static ImmutableSortedDictionary<string, IDictionary<object, object>> SetToMap<E>(
            ISet<E> set, Func<E, string> keyMapper)
        {
            if (set == null)
            {
                return null;
            }

            return set.ToImmutableSortedDictionary(
                keyMapper,
                _ => (IDictionary<object, object>)new Dictionary<object, object>(),
                StringComparer.Ordinal);
        }

        private readonly Image image;

        /**
         * Instantiate with an {@link Image}.
         *
         * @param image the image to translate
         */
        public ImageToJsonTranslator(Image image)
        {
            this.image = image;
        }

        /**
         * Gets the container configuration as a {@link Blob}.
         *
         * @return the container configuration {@link Blob}
         */
        public ContainerConfigurationTemplate GetContainerConfiguration()
        {
            // ISet up the JSON template.
            ContainerConfigurationTemplate template = new ContainerConfigurationTemplate();

            // Adds the layer diff IDs.
            foreach (ILayer layer in image.GetLayers())
            {
                template.AddLayerDiffId(layer.GetDiffId());
            }

            // Adds the history.
            foreach (HistoryEntry historyObject in image.GetHistory())
            {
                template.AddHistoryEntry(historyObject);
            }

            template.Created = image.GetCreated()?.ToString();
            template.Architecture = image.GetArchitecture();
            template.Os = image.GetOs();
            template.SetContainerEnvironment(EnvironmentMapToList(image.GetEnvironment()));
            template.SetContainerEntrypoint(image.GetEntrypoint());
            template.SetContainerCmd(image.GetProgramArguments());
            template.SetContainerExposedPorts(PortSetToMap(image.GetExposedPorts()));
            template.SetContainerVolumes(VolumesSetToMap(image.GetVolumes()));
            template.SetContainerLabels(image.GetLabels());
            template.SetContainerWorkingDir(image.GetWorkingDirectory());
            template.SetContainerUser(image.GetUser());

            // Ignore healthcheck if not Docker/command is empty
            DockerHealthCheck healthCheck = image.GetHealthCheck();
            if (image.GetImageFormat() == ManifestFormat.V22 && healthCheck != null)
            {
                template.SetContainerHealthCheckTest(healthCheck.GetCommand());
                healthCheck
                    .GetInterval()
                    .IfPresent(interval => template.SetContainerHealthCheckInterval((long)interval.TotalNanoseconds));
                healthCheck
                    .GetTimeout()
                    .IfPresent(timeout => template.SetContainerHealthCheckTimeout((long)timeout.TotalNanoseconds));
                healthCheck
                    .GetStartPeriod()
                    .IfPresent(
                        startPeriod => template.SetContainerHealthCheckStartPeriod((long)startPeriod.TotalNanoseconds));
                template.SetContainerHealthCheckRetries(healthCheck.GetRetries().AsNullable());
            }

            return template;
        }

        /**
         * Gets the manifest as a JSON template. The {@code containerConfigurationBlobDescriptor} must be
         * the {@link BlobDescriptor} obtained by writing out the container configuration JSON returned
         * from {@link #getContainerConfiguration()}.
         *
         * @param <T> child type of {@link BuildableManifestTemplate}.
         * @param manifestTemplateClass the JSON template to translate the image to.
         * @param containerConfigurationBlobDescriptor the container configuration descriptor.
         * @return the image contents serialized as JSON.
         */
        public IBuildableManifestTemplate GetManifestTemplate(
            ManifestFormat manifestFormat, BlobDescriptor containerConfigurationBlobDescriptor)
        {
            containerConfigurationBlobDescriptor =
                containerConfigurationBlobDescriptor
                ?? throw new ArgumentNullException(nameof(containerConfigurationBlobDescriptor));
            try
            {
                IBuildableManifestTemplate template;
                // ISet up the JSON template.
                switch (manifestFormat)
                {
                    case ManifestFormat.V22:
                        template = new V22ManifestTemplate();
                        break;
                    case ManifestFormat.OCI:
                        template = new OCIManifestTemplate();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(manifestFormat));
                }
                IBuildableManifestTemplate buildableTemplate = template;

                // Adds the container configuration reference.
                DescriptorDigest containerConfigurationDigest =
                    containerConfigurationBlobDescriptor.GetDigest();
                long containerConfigurationSize = containerConfigurationBlobDescriptor.GetSize();
                buildableTemplate.SetContainerConfiguration(containerConfigurationSize, containerConfigurationDigest);

                // Adds the layers.
                foreach (ILayer layer in image.GetLayers())
                {
                    buildableTemplate.AddLayer(
            layer.GetBlobDescriptor().GetSize(), layer.GetBlobDescriptor().GetDigest());
                }

                // Serializes into JSON.
                return template;
            }
            catch (JsonException ex)
            {
                throw new ArgumentException(manifestFormat + " cannot be instantiated", ex);
            }
        }
    }
}
