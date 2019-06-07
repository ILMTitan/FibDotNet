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















class PushLayersStep:AsyncStep<ImmutableList<AsyncStep<PushBlobStep>>>,
        Callable<ImmutableList<AsyncStep<PushBlobStep>>> {

  private static readonly string DESCRIPTION = "Setting up to push layers";

  private readonly BuildConfiguration buildConfiguration;
  private readonly ListeningExecutorService listeningExecutorService;
  private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

  private readonly AuthenticatePushStep authenticatePushStep;
  private readonly AsyncStep<IImmutableList<AsyncStep<CachedLayer>>>
      cachedLayerStep;

  private readonly ListenableFuture<ImmutableList<AsyncStep<PushBlobStep>>> listenableFuture;

  PushLayersStep(
      ListeningExecutorService listeningExecutorService,
      BuildConfiguration buildConfiguration,
      ProgressEventDispatcher.Factory progressEventDispatcherFactory,
      AuthenticatePushStep authenticatePushStep,
      AsyncStep<IReadOnlyList<AsyncStep<CachedLayer>>>
          cachedLayerStep) {
    this.listeningExecutorService = listeningExecutorService;
    this.buildConfiguration = buildConfiguration;
    this.progressEventDispatcherFactory = progressEventDispatcherFactory;
    this.authenticatePushStep = authenticatePushStep;
    this.cachedLayerStep = cachedLayerStep;

    listenableFuture =
        AsyncDependencies.@using(listeningExecutorService)
            .addStep(cachedLayerStep)
            .whenAllSucceed(this);
  }

  public ListenableFuture<ImmutableList<AsyncStep<PushBlobStep>>> getFuture() {
    return listenableFuture;
  }

  public ImmutableList<AsyncStep<PushBlobStep>> call() {
    using (TimerEventDispatcher ignored =
        new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION)) {
      IReadOnlyList<AsyncStep<CachedLayer>> cachedLayers =
          NonBlockingSteps.get(cachedLayerStep);

      using (ProgressEventDispatcher progressEventDispatcher =
          progressEventDispatcherFactory.create("setting up to push layers", cachedLayers.size())) {
        // Constructs a PushBlobStep for each layer.
        ImmutableList.Builder<AsyncStep<PushBlobStep>> pushBlobStepsBuilder =
            ImmutableList.builder();
        foreach (AsyncStep<CachedLayer> cachedLayerStep in cachedLayers)
        {
          ProgressEventDispatcher.Factory childProgressEventDispatcherFactory =
              progressEventDispatcher.newChildProducer();
          ListenableFuture<PushBlobStep> pushBlobStepFuture =
              Futures.whenAllSucceed(cachedLayerStep.getFuture())
                  .call(
                      () => makePushBlobStep(cachedLayerStep, childProgressEventDispatcherFactory),
                      listeningExecutorService);
          pushBlobStepsBuilder.add(() => pushBlobStepFuture);
        }

        return pushBlobStepsBuilder.build();
      }
    }
  }

  private PushBlobStep makePushBlobStep(
      AsyncStep<CachedLayer> cachedLayerStep,
      ProgressEventDispatcher.Factory progressEventDispatcherFactory)
      {
    CachedLayer cachedLayer = NonBlockingSteps.get(cachedLayerStep);

    return new PushBlobStep(
        listeningExecutorService,
        buildConfiguration,
        progressEventDispatcherFactory,
        authenticatePushStep,
        new BlobDescriptor(cachedLayer.getSize(), cachedLayer.getDigest()),
        cachedLayer.getBlob());
  }
}
}
