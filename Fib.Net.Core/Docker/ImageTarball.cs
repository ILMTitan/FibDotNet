// Copyright 2018 Google LLC.
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
using Fib.Net.Core.Docker.Json;
using Fib.Net.Core.Images;
using Fib.Net.Core.Images.Json;
using Fib.Net.Core.Json;
using Fib.Net.Core.Tar;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Core.Docker
{
    /** Translates an {@link Image} to a tarball that can be loaded into Docker. */
    public class ImageTarball : IImageTarball
    {
        /** File name for the container configuration in the tarball. */
        private const string CONTAINER_CONFIGURATION_JSON_FILE_NAME = "config.json";

        /** File name for the manifest in the tarball. */
        private const string MANIFEST_JSON_FILE_NAME = "manifest.json";

        /** File name extension for the layer content files. */
        private const string LAYER_FILE_EXTENSION = ".tar.gz";

        private readonly Image image;

        private readonly IImageReference imageReference;

        /**
         * Instantiate with an {@link Image}.
         *
         * @param image the image to convert into a tarball
         * @param imageReference image reference to set in the manifest
         */
        public ImageTarball(Image image, IImageReference imageReference)
        {
            this.image = image;
            this.imageReference = imageReference;
        }

        public async Task WriteToAsync(Stream @out)
        {
            TarStreamBuilder tarStreamBuilder = new TarStreamBuilder();
            DockerLoadManifestEntryTemplate manifestTemplate = new DockerLoadManifestEntryTemplate();

            // Adds all the layers to the tarball and manifest.
            foreach (ILayer layer in image.GetLayers())
            {
                string layerName = layer.GetBlobDescriptor().GetDigest().GetHash() + LAYER_FILE_EXTENSION;

                tarStreamBuilder.AddBlobEntry(
                    layer.GetBlob(), layer.GetBlobDescriptor().GetSize(), layerName);
                manifestTemplate.AddLayerFile(layerName);
            }

            // Adds the container configuration to the tarball.
            ContainerConfigurationTemplate containerConfiguration =
                new ImageToJsonTranslator(image).GetContainerConfiguration();
            tarStreamBuilder.AddByteEntry(
                JsonTemplateMapper.ToByteArray(containerConfiguration),
                CONTAINER_CONFIGURATION_JSON_FILE_NAME);

            // Adds the manifest to tarball.
            manifestTemplate.SetRepoTags(imageReference.ToStringWithTag());
            tarStreamBuilder.AddByteEntry(
                JsonTemplateMapper.ToByteArray(new List<DockerLoadManifestEntryTemplate> { manifestTemplate }),
                MANIFEST_JSON_FILE_NAME);

            await tarStreamBuilder.WriteAsTarArchiveToAsync(@out).ConfigureAwait(false);
        }
    }
}
