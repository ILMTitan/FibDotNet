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
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.image.json
{










    /**
     * Translates an {@link Image} into a manifest or container configuration JSON BLOB.
     *
     * <p>Example usage:
     *
     * <pre>{@code
     * ImageToJsonTranslator translator = new ImageToJsonTranslator(image);
     * Blob containerConfigurationBlob = translator.getContainerConfigurationBlob();
     * BlobDescriptor containerConfigurationBlobDescriptor = blob.writeTo(outputStream);
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

        public static IDictionary<string, IDictionary<object, object>> portSetToMap(ISet<Port> exposedPorts)
        {
            return setToMap(exposedPorts, port => port.getPort() + "/" + port.getProtocol());
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

        public static ImmutableSortedDictionary<string, IDictionary<object, object>> volumesSetToMap(ISet<AbsoluteUnixPath> volumes)
        {
            return setToMap(volumes, p => p.toString());
        }

        /**
         * Converts the map of environment variables to a list with items in the format "NAME=VALUE".
         *
         * @return the list
         */

        public static ImmutableArray<string> environmentMapToList(IDictionary<string, string> environment)
        {
            if (environment == null)
            {
                return ImmutableArray<string>.Empty;
            }
            Preconditions.checkArgument(
                environment.keySet().stream().noneMatch(key => key.contains("=")),
                "Illegal environment variable: name cannot contain '='");
            return environment
                .entrySet()
                .stream()
                .map(entry => entry.getKey() + "=" + entry.getValue())
                .collect(ImmutableArray.ToImmutableArray);
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
        private static ImmutableSortedDictionary<string, IDictionary<object, object>> setToMap<E>(
            ISet<E> set, Func<E, string> keyMapper)
        {
            if (set == null)
            {
                return null;
            }

            return set.stream()
                .collect(e => e.ToImmutableSortedDictionary(keyMapper, _ => Collections.emptyMap<object, object>(), StringComparer.Ordinal));
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
        public JsonTemplate getContainerConfiguration()
        {
            // ISet up the JSON template.
            ContainerConfigurationTemplate template = new ContainerConfigurationTemplate();

            // Adds the layer diff IDs.
            foreach (Layer layer in image.getLayers())
            {
                template.addLayerDiffId(layer.getDiffId());
            }

            // Adds the history.
            foreach (HistoryEntry historyObject in image.getHistory())
            {
                template.addHistoryEntry(historyObject);
            }

            template.setCreated(image.getCreated()?.toString());
            template.setArchitecture(image.getArchitecture());
            template.setOs(image.getOs());
            template.setContainerEnvironment(environmentMapToList(image.getEnvironment()));
            template.setContainerEntrypoint(image.getEntrypoint());
            template.setContainerCmd(image.getProgramArguments());
            template.setContainerExposedPorts(portSetToMap(image.getExposedPorts()));
            template.setContainerVolumes(volumesSetToMap(image.getVolumes()));
            template.setContainerLabels(image.getLabels());
            template.setContainerWorkingDir(image.getWorkingDirectory());
            template.setContainerUser(image.getUser());

            // Ignore healthcheck if not Docker/command is empty
            DockerHealthCheck healthCheck = image.getHealthCheck();
            if (image.getImageFormat().Type == typeof(V22ManifestTemplate) && healthCheck != null)
            {
                template.setContainerHealthCheckTest(healthCheck.getCommand());
                healthCheck
                    .getInterval()
                    .ifPresent(interval => template.setContainerHealthCheckInterval(interval.toNanos()));
                healthCheck
                    .getTimeout()
                    .ifPresent(timeout => template.setContainerHealthCheckTimeout(timeout.toNanos()));
                healthCheck
                    .getStartPeriod()
                    .ifPresent(
                        startPeriod => template.setContainerHealthCheckStartPeriod(startPeriod.toNanos()));
                template.setContainerHealthCheckRetries(healthCheck.getRetries().asNullable());
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
        public T getManifestTemplate<T>(
            IClass<T> manifestTemplateClass, BlobDescriptor containerConfigurationBlobDescriptor)
        {
            try
            {
                // ISet up the JSON template.
                T template = manifestTemplateClass.getDeclaredConstructor().newInstance();
                BuildableManifestTemplate buildableTemplate = (BuildableManifestTemplate)template;

                // Adds the container configuration reference.
                DescriptorDigest containerConfigurationDigest =
                    containerConfigurationBlobDescriptor.getDigest();
                long containerConfigurationSize = containerConfigurationBlobDescriptor.getSize();
                buildableTemplate.setContainerConfiguration(containerConfigurationSize, containerConfigurationDigest);

                // Adds the layers.
                foreach (Layer layer in image.getLayers())
                {
                    buildableTemplate.addLayer(
            layer.getBlobDescriptor().getSize(), layer.getBlobDescriptor().getDigest());
                }

                // Serializes into JSON.
                return template;
            }
            catch (Exception ex) when (ex is InstantiationException
              || ex is IllegalAccessException
              || ex is NoSuchMethodException
              || ex is InvocationTargetException)
            {
                throw new ArgumentException(manifestTemplateClass + " cannot be instantiated", ex);
            }
        }
    }
}
