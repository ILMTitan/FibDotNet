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


















/** Pushes the container configuration. */
class PushContainerConfigurationStep : AsyncStep<AsyncStep<PushBlobStep>>, Callable<AsyncStep<PushBlobStep>>  {
  private static readonly string DESCRIPTION = "Pushing container configuration";

  private readonly BuildConfiguration buildConfiguration;
  private readonly ListeningExecutorService listeningExecutorService;
  private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

  private readonly AuthenticatePushStep authenticatePushStep;
  private readonly BuildImageStep buildImageStep;

  private readonly ListenableFuture<AsyncStep<PushBlobStep>> listenableFuture;

  PushContainerConfigurationStep(
      ListeningExecutorService listeningExecutorService,
      BuildConfiguration buildConfiguration,
      ProgressEventDispatcher.Factory progressEventDispatcherFactory,
      AuthenticatePushStep authenticatePushStep,
      BuildImageStep buildImageStep) {
    this.listeningExecutorService = listeningExecutorService;
    this.buildConfiguration = buildConfiguration;
    this.progressEventDispatcherFactory = progressEventDispatcherFactory;
    this.authenticatePushStep = authenticatePushStep;
    this.buildImageStep = buildImageStep;

    listenableFuture =
        AsyncDependencies.@using(listeningExecutorService)
            .addStep(buildImageStep)
            .whenAllSucceed(this);
  }

  public ListenableFuture<AsyncStep<PushBlobStep>> getFuture() {
    return listenableFuture;
  }

  public AsyncStep<PushBlobStep> call() {
    ListenableFuture<PushBlobStep> pushBlobStepFuture =
        AsyncDependencies.@using(listeningExecutorService)
            .addStep(authenticatePushStep)
            .addStep(NonBlockingSteps.get(buildImageStep))
            .whenAllSucceed(this.afterBuildConfigurationFutureFuture);
    return () => pushBlobStepFuture;
  }

  private PushBlobStep afterBuildConfigurationFutureFuture()
      {
    using(ProgressEventDispatcher progressEventDispatcher =
            progressEventDispatcherFactory.create("pushing container configuration", 1))
    using(TimerEventDispatcher ignored =
            new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION)))
    {

      Image image = NonBlockingSteps.get(NonBlockingSteps.get(buildImageStep));
      JsonTemplate containerConfiguration =
          new ImageToJsonTranslator(image).getContainerConfiguration();
      BlobDescriptor blobDescriptor = Digests.computeDigest(containerConfiguration);

      return new PushBlobStep(
          listeningExecutorService,
          buildConfiguration,
          progressEventDispatcher.newChildProducer(),
          authenticatePushStep,
          blobDescriptor,
          Blobs.from(containerConfiguration));
    }
  }
}
}
