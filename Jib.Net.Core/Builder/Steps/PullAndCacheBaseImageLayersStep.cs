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















/** Pulls and caches the base image layers. */
class PullAndCacheBaseImageLayersStep : AsyncStep<ImmutableList<PullAndCacheBaseImageLayerStep>>,
        Callable<ImmutableList<PullAndCacheBaseImageLayerStep>>  {
  private static readonly string DESCRIPTION = "Setting up base image caching";

  private readonly BuildConfiguration buildConfiguration;
  private readonly ListeningExecutorService listeningExecutorService;
  private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

  private readonly PullBaseImageStep pullBaseImageStep;

  private readonly ListenableFuture<ImmutableList<PullAndCacheBaseImageLayerStep>> listenableFuture;

  PullAndCacheBaseImageLayersStep(
      ListeningExecutorService listeningExecutorService,
      BuildConfiguration buildConfiguration,
      ProgressEventDispatcher.Factory progressEventDispatcherFactory,
      PullBaseImageStep pullBaseImageStep) {
    this.listeningExecutorService = listeningExecutorService;
    this.buildConfiguration = buildConfiguration;
    this.progressEventDispatcherFactory = progressEventDispatcherFactory;
    this.pullBaseImageStep = pullBaseImageStep;

    listenableFuture =
        AsyncDependencies.@using(listeningExecutorService)
            .addStep(pullBaseImageStep)
            .whenAllSucceed(this);
  }

  public ListenableFuture<ImmutableList<PullAndCacheBaseImageLayerStep>> getFuture() {
    return listenableFuture;
  }

  public ImmutableList<PullAndCacheBaseImageLayerStep> call()
      {
    BaseImageWithAuthorization pullBaseImageStepResult = NonBlockingSteps.get(pullBaseImageStep);
    ImmutableList<Layer> baseImageLayers = pullBaseImageStepResult.getBaseImage().getLayers();

    using(ProgressEventDispatcher progressEventDispatcher =
            progressEventDispatcherFactory.create(
                "checking base image layers", baseImageLayers.size()))

    using(TimerEventDispatcher ignored =
            new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION))

    {

      ImmutableList.Builder<PullAndCacheBaseImageLayerStep> pullAndCacheBaseImageLayerStepsBuilder =
          ImmutableList.builderWithExpectedSize(baseImageLayers.size());
      foreach (Layer layer in baseImageLayers)
      {
        pullAndCacheBaseImageLayerStepsBuilder.add(
            new PullAndCacheBaseImageLayerStep(
                listeningExecutorService,
                buildConfiguration,
                progressEventDispatcher.newChildProducer(),
                layer.getBlobDescriptor().getDigest(),
                pullBaseImageStepResult.getBaseImageAuthorization()));
      }

      return pullAndCacheBaseImageLayerStepsBuilder.build();
    }
  }
}
}
