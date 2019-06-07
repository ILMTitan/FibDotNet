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

namespace com.google.cloud.tools.jib.docker {












/** Translates an {@link Image} to a tarball that can be loaded into Docker. */
public class ImageTarball {

  /** File name for the container configuration in the tarball. */
  private static readonly string CONTAINER_CONFIGURATION_JSON_FILE_NAME = "config.json";

  /** File name for the manifest in the tarball. */
  private static readonly string MANIFEST_JSON_FILE_NAME = "manifest.json";

  /** File name extension for the layer content files. */
  private static readonly string LAYER_FILE_EXTENSION = ".tar.gz";

  private readonly Image image;

  private readonly ImageReference imageReference;

  /**
   * Instantiate with an {@link Image}.
   *
   * @param image the image to convert into a tarball
   * @param imageReference image reference to set in the manifest
   */
  public ImageTarball(Image image, ImageReference imageReference) {
    this.image = image;
    this.imageReference = imageReference;
  }

  public void writeTo(OutputStream out) {
    TarStreamBuilder tarStreamBuilder = new TarStreamBuilder();
    DockerLoadManifestEntryTemplate manifestTemplate = new DockerLoadManifestEntryTemplate();

    // Adds all the layers to the tarball and manifest.
    foreach (Layer layer in image.getLayers())
    {
      string layerName = layer.getBlobDescriptor().getDigest().getHash() + LAYER_FILE_EXTENSION;

      tarStreamBuilder.addBlobEntry(
          layer.getBlob(), layer.getBlobDescriptor().getSize(), layerName);
      manifestTemplate.addLayerFile(layerName);
    }

    // Adds the container configuration to the tarball.
    JsonTemplate containerConfiguration =
        new ImageToJsonTranslator(image).getContainerConfiguration();
    tarStreamBuilder.addByteEntry(
        JsonTemplateMapper.toByteArray(containerConfiguration),
        CONTAINER_CONFIGURATION_JSON_FILE_NAME);

    // Adds the manifest to tarball.
    manifestTemplate.setRepoTags(imageReference.toStringWithTag());
    tarStreamBuilder.addByteEntry(
        JsonTemplateMapper.toByteArray(Collections.singletonList(manifestTemplate)),
        MANIFEST_JSON_FILE_NAME);

    tarStreamBuilder.writeAsTarArchiveTo(out);
  }
}
}
