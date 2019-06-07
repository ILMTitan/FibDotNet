/*
 * Copyright 2018 Google LLC. All rights reserved.
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

namespace com.google.cloud.tools.jib.cache {

























/** Reads from the default cache storage engine. */
class CacheStorageReader {

  private readonly CacheStorageFiles cacheStorageFiles;

  CacheStorageReader(CacheStorageFiles cacheStorageFiles) {
    this.cacheStorageFiles = cacheStorageFiles;
  }

  /**
   * Lists all the layer digests stored.
   *
   * @return the list of layer digests
   * @throws CacheCorruptedException if the cache was found to be corrupted
   * @throws IOException if an I/O exception occurs
   */
  Set<DescriptorDigest> fetchDigests() {
    using (Stream<Path> layerDirectories = Files.list(cacheStorageFiles.getLayersDirectory())) {
      List<Path> layerDirectoriesList = layerDirectories.collect(Collectors.toList());
      Set<DescriptorDigest> layerDigests = new HashSet<>(layerDirectoriesList.size());
      foreach (Path layerDirectory in layerDirectoriesList)
      {
        try {
          layerDigests.add(DescriptorDigest.fromHash(layerDirectory.getFileName().toString()));

        } catch (DigestException ex) {
          throw new CacheCorruptedException(
              cacheStorageFiles.getCacheDirectory(),
              "Found non-digest file in layers directory",
              ex);
        }
      }
      return layerDigests;
    }
  }

  /**
   * Retrieves the cached manifest and container configuration for an image reference.
   *
   * @param imageReference the image reference
   * @return the manifest and container configuration for the image reference, if found
   * @throws IOException if an I/O exception occurs
   * @throws CacheCorruptedException if the cache is corrupted
   */
  Optional<ManifestAndConfig> retrieveMetadata(ImageReference imageReference)
      {
    Path imageDirectory = cacheStorageFiles.getImageDirectory(imageReference);
    Path manifestPath = imageDirectory.resolve("manifest.json");
    if (!Files.exists(manifestPath)) {
      return Optional.empty();
    }

    using (LockFile ignored = LockFile.@lock(imageDirectory.resolve("lock"))) {
      // TODO: Consolidate with ManifestPuller
      ObjectNode node =
          new ObjectMapper().readValue(Files.newInputStream(manifestPath), typeof(ObjectNode));
      if (!node.has("schemaVersion")) {
        throw new CacheCorruptedException(
            cacheStorageFiles.getCacheDirectory(), "Cannot find field 'schemaVersion' in manifest");
      }

      int schemaVersion = node.get("schemaVersion").asInt(-1);
      if (schemaVersion == -1) {
        throw new CacheCorruptedException(
            cacheStorageFiles.getCacheDirectory(),
            "`schemaVersion` field is not an integer in manifest");
      }

      if (schemaVersion == 1) {
        return Optional.of(
            new ManifestAndConfig(
                JsonTemplateMapper.readJsonFromFile(manifestPath, typeof(V21ManifestTemplate)),
                null));
      }
      if (schemaVersion == 2) {
        // 'schemaVersion' of 2 can be either Docker V2.2 or OCI.
        string mediaType = node.get("mediaType").asText();

        ManifestTemplate manifestTemplate;
        if (V22ManifestTemplate.MANIFEST_MEDIA_TYPE.equals(mediaType)) {
          manifestTemplate =
              JsonTemplateMapper.readJsonFromFile(manifestPath, typeof(V22ManifestTemplate));
        } else if (OCIManifestTemplate.MANIFEST_MEDIA_TYPE.equals(mediaType)) {
          manifestTemplate =
              JsonTemplateMapper.readJsonFromFile(manifestPath, typeof(OCIManifestTemplate));
        } else {
          throw new CacheCorruptedException(
              cacheStorageFiles.getCacheDirectory(), "Unknown manifest mediaType: " + mediaType);
        }

        Path configPath = imageDirectory.resolve("config.json");
        if (!Files.exists(configPath)) {
          throw new CacheCorruptedException(
              cacheStorageFiles.getCacheDirectory(),
              "Manifest found, but missing container configuration");
        }
        ContainerConfigurationTemplate config =
            JsonTemplateMapper.readJsonFromFile(configPath, typeof(ContainerConfigurationTemplate));

        return Optional.of(new ManifestAndConfig(manifestTemplate, config));
      }
      throw new CacheCorruptedException(
          cacheStorageFiles.getCacheDirectory(),
          "Unknown schemaVersion in manifest: " + schemaVersion + " - only 1 and 2 are supported");
    }
  }

  /**
   * Retrieves the {@link CachedLayer} for the layer with digest {@code layerDigest}.
   *
   * @param layerDigest the layer digest
   * @return the {@link CachedLayer} referenced by the layer digest, if found
   * @throws CacheCorruptedException if the cache was found to be corrupted
   * @throws IOException if an I/O exception occurs
   */
  Optional<CachedLayer> retrieve(DescriptorDigest layerDigest)
      {
    Path layerDirectory = cacheStorageFiles.getLayerDirectory(layerDigest);
    if (!Files.exists(layerDirectory)) {
      return Optional.empty();
    }

    CachedLayer.Builder cachedLayerBuilder = CachedLayer.builder().setLayerDigest(layerDigest);

    using (Stream<Path> filesInLayerDirectory = Files.list(layerDirectory)) {
      foreach (Path fileInLayerDirectory in filesInLayerDirectory.collect(Collectors.toList()))
      {
        if (CacheStorageFiles.isLayerFile(fileInLayerDirectory)) {
          if (cachedLayerBuilder.hasLayerBlob()) {
            throw new CacheCorruptedException(
                cacheStorageFiles.getCacheDirectory(),
                "Multiple layer files found for layer with digest "
                    + layerDigest.getHash()
                    + " in directory: "
                    + layerDirectory);
          }
          cachedLayerBuilder
              .setLayerBlob(Blobs.from(fileInLayerDirectory))
              .setLayerDiffId(cacheStorageFiles.getDiffId(fileInLayerDirectory))
              .setLayerSize(Files.size(fileInLayerDirectory));
        }
      }
    }

    return Optional.of(cachedLayerBuilder.build());
  }

  /**
   * Retrieves the layer digest selected by the {@code selector}.
   *
   * @param selector the selector
   * @return the layer digest {@code selector} selects, if found
   * @throws CacheCorruptedException if the selector file contents was not a valid layer digest
   * @throws IOException if an I/O exception occurs
   */
  Optional<DescriptorDigest> select(DescriptorDigest selector)
      {
    Path selectorFile = cacheStorageFiles.getSelectorFile(selector);
    if (!Files.exists(selectorFile)) {
      return Optional.empty();
    }

    string selectorFileContents =
        new string(Files.readAllBytes(selectorFile), StandardCharsets.UTF_8);
    try {
      return Optional.of(DescriptorDigest.fromHash(selectorFileContents));

    } catch (DigestException ex) {
      throw new CacheCorruptedException(
          cacheStorageFiles.getCacheDirectory(),
          "Expected valid layer digest as contents of selector file `"
              + selectorFile
              + "` for selector `"
              + selector.getHash()
              + "`, but got: "
              + selectorFileContents);
    }
  }
}
}
