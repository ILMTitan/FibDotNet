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

namespace com.google.cloud.tools.jib.builder.steps {


















/** Builds and caches application layers. */
class BuildAndCacheApplicationLayerStep : AsyncStep<CachedLayer>, Callable<CachedLayer>
    {
  private static readonly string DESCRIPTION = "Building application layers";

  /**
   * Makes a list of {@link BuildAndCacheApplicationLayerStep} for dependencies, resources, and
   * classes layers. Optionally adds an extra layer if configured to do so.
   */
  static ImmutableList<BuildAndCacheApplicationLayerStep> makeList(
      ListeningExecutorService listeningExecutorService,
      BuildConfiguration buildConfiguration,
      ProgressEventDispatcher.Factory progressEventDispatcherFactory) {
    int layerCount = buildConfiguration.getLayerConfigurations().size();

    using(ProgressEventDispatcher progressEventDispatcher =
            progressEventDispatcherFactory.create(
                "setting up to build application layers", layerCount))

    using(TimerEventDispatcher ignored =
            new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION)))

    {

      ImmutableList.Builder<BuildAndCacheApplicationLayerStep> buildAndCacheApplicationLayerSteps =
          ImmutableList.builderWithExpectedSize(layerCount);
      foreach (LayerConfiguration layerConfiguration in buildConfiguration.getLayerConfigurations())
      {
        // Skips the layer if empty.
        if (layerConfiguration.getLayerEntries().isEmpty()) {
          continue;
        }

        buildAndCacheApplicationLayerSteps.add(
            new BuildAndCacheApplicationLayerStep(
                listeningExecutorService,
                buildConfiguration,
                progressEventDispatcher.newChildProducer(),
                layerConfiguration.getName(),
                layerConfiguration));
      }
      ImmutableList<BuildAndCacheApplicationLayerStep> steps =
          buildAndCacheApplicationLayerSteps.build();
      return steps;
    }
  }

  private readonly BuildConfiguration buildConfiguration;
  private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

  private readonly string layerType;
  private readonly LayerConfiguration layerConfiguration;

  private readonly ListenableFuture<CachedLayer> listenableFuture;

  private BuildAndCacheApplicationLayerStep(
      ListeningExecutorService listeningExecutorService,
      BuildConfiguration buildConfiguration,
      ProgressEventDispatcher.Factory progressEventDispatcherFactory,
      string layerType,
      LayerConfiguration layerConfiguration) {
    this.buildConfiguration = buildConfiguration;
    this.progressEventDispatcherFactory = progressEventDispatcherFactory;
    this.layerType = layerType;
    this.layerConfiguration = layerConfiguration;

    listenableFuture = listeningExecutorService.submit(this);
  }

  public ListenableFuture<CachedLayer> getFuture() {
    return listenableFuture;
  }

  public CachedLayer call() {
    string description = "Building " + layerType + " layer";

    buildConfiguration.getEventHandlers().dispatch(LogEvent.progress(description + "..."));

    using(ProgressEventDispatcher ignored =
            progressEventDispatcherFactory.create("building " + layerType + " layer", 1))

    using(TimerEventDispatcher ignored2 =
            new TimerEventDispatcher(buildConfiguration.getEventHandlers(), description)))

    {

      Cache cache = buildConfiguration.getApplicationLayersCache();

      // Don't build the layer if it exists already.
      Optional<CachedLayer> optionalCachedLayer =
          cache.retrieve(layerConfiguration.getLayerEntries());
      if (optionalCachedLayer.isPresent()) {
        return optionalCachedLayer.get();
      }

      Blob layerBlob = new ReproducibleLayerBuilder(layerConfiguration.getLayerEntries()).build();
      CachedLayer cachedLayer =
          cache.writeUncompressedLayer(layerBlob, layerConfiguration.getLayerEntries());

      buildConfiguration
          .getEventHandlers()
          .dispatch(LogEvent.debug(description + " built " + cachedLayer.getDigest()));

      return cachedLayer;
    }
  }

  string getLayerType() {
    return layerType;
  }
}
}
