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























/** Pushes the final image. Outputs the pushed image digest. */
class PushImageStep : $2 {
  private static readonly string DESCRIPTION = "Pushing new image";

  private readonly BuildConfiguration buildConfiguration;
  private readonly ListeningExecutorService listeningExecutorService;
  private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

  private readonly AuthenticatePushStep authenticatePushStep;

  private readonly PushLayersStep pushBaseImageLayersStep;
  private readonly PushLayersStep pushApplicationLayersStep;
  private readonly PushContainerConfigurationStep pushContainerConfigurationStep;
  private readonly BuildImageStep buildImageStep;

  private readonly ListenableFuture<BuildResult> listenableFuture;

  PushImageStep(
      ListeningExecutorService listeningExecutorService,
      BuildConfiguration buildConfiguration,
      ProgressEventDispatcher.Factory progressEventDispatcherFactory,
      AuthenticatePushStep authenticatePushStep,
      PushLayersStep pushBaseImageLayersStep,
      PushLayersStep pushApplicationLayersStep,
      PushContainerConfigurationStep pushContainerConfigurationStep,
      BuildImageStep buildImageStep) {
    this.listeningExecutorService = listeningExecutorService;
    this.buildConfiguration = buildConfiguration;
    this.progressEventDispatcherFactory = progressEventDispatcherFactory;
    this.authenticatePushStep = authenticatePushStep;
    this.pushBaseImageLayersStep = pushBaseImageLayersStep;
    this.pushApplicationLayersStep = pushApplicationLayersStep;
    this.pushContainerConfigurationStep = pushContainerConfigurationStep;
    this.buildImageStep = buildImageStep;

    listenableFuture =
        AsyncDependencies.@using(listeningExecutorService)
            .addStep(pushBaseImageLayersStep)
            .addStep(pushApplicationLayersStep)
            .addStep(pushContainerConfigurationStep)
            .whenAllSucceed(this);
  }

  public ListenableFuture<BuildResult> getFuture() {
    return listenableFuture;
  }

  public BuildResult call() {
    return AsyncDependencies.@using(listeningExecutorService)
        .addStep(authenticatePushStep)
        .addSteps(NonBlockingSteps.get(pushBaseImageLayersStep))
        .addSteps(NonBlockingSteps.get(pushApplicationLayersStep))
        .addStep(NonBlockingSteps.get(pushContainerConfigurationStep))
        .addStep(NonBlockingSteps.get(buildImageStep))
        .whenAllSucceed(this::afterPushSteps)
        .get();
  }

  private BuildResult afterPushSteps() {
    AsyncDependencies dependencies = AsyncDependencies.@using(listeningExecutorService);
    foreach (AsyncStep<PushBlobStep> pushBaseImageLayerStep in NonBlockingSteps.get(pushBaseImageLayersStep))
    {
      dependencies.addStep(NonBlockingSteps.get(pushBaseImageLayerStep));
    }
    foreach (AsyncStep<PushBlobStep> pushApplicationLayerStep in NonBlockingSteps.get(pushApplicationLayersStep))
    {
      dependencies.addStep(NonBlockingSteps.get(pushApplicationLayerStep));
    }
    return dependencies
        .addStep(NonBlockingSteps.get(NonBlockingSteps.get(pushContainerConfigurationStep)))
        .whenAllSucceed(this.afterAllPushed)
        .get();
  }

  private BuildResult afterAllPushed()
      {
    ImmutableSet<string> targetImageTags = buildConfiguration.getAllTargetImageTags();
    ProgressEventDispatcher progressEventDispatcher =
        progressEventDispatcherFactory.create("pushing image manifest", targetImageTags.size());

    using (TimerEventDispatcher ignored =
        new TimerEventDispatcher(buildConfiguration.getEventHandlers(), DESCRIPTION)) {
      RegistryClient registryClient =
          buildConfiguration
              .newTargetImageRegistryClientFactory()
              .setAuthorization(NonBlockingSteps.get(authenticatePushStep))
              .newRegistryClient();

      // Constructs the image.
      ImageToJsonTranslator imageToJsonTranslator =
          new ImageToJsonTranslator(NonBlockingSteps.get(NonBlockingSteps.get(buildImageStep)));

      // Gets the image manifest to push.
      BlobDescriptor containerConfigurationBlobDescriptor =
          NonBlockingSteps.get(
              NonBlockingSteps.get(NonBlockingSteps.get(pushContainerConfigurationStep)));
      BuildableManifestTemplate manifestTemplate =
          imageToJsonTranslator.getManifestTemplate(
              buildConfiguration.getTargetFormat(), containerConfigurationBlobDescriptor);

      // Pushes to all target image tags.
      List<ListenableFuture<Void>> pushAllTagsFutures = new ArrayList<>();
      foreach (string tag in targetImageTags)
      {
        ProgressEventDispatcher.Factory progressEventDispatcherFactory =
            progressEventDispatcher.newChildProducer();
        pushAllTagsFutures.add(
            listeningExecutorService.submit(
                () => {
                  using (ProgressEventDispatcher ignored2 =
                      progressEventDispatcherFactory.create("tagging with " + tag, 1)) {
                    buildConfiguration
                        .getEventHandlers()
                        .dispatch(LogEvent.info("Tagging with " + tag + "..."));
                    registryClient.pushManifest(manifestTemplate, tag);
                  }
                  return null;
                }));
      }

      DescriptorDigest imageDigest = Digests.computeJsonDigest(manifestTemplate);
      DescriptorDigest imageId = containerConfigurationBlobDescriptor.getDigest();
      BuildResult result = new BuildResult(imageDigest, imageId);

      return Futures.whenAllSucceed(pushAllTagsFutures)
          .call(
              () => {
                progressEventDispatcher.close();
                return result;
              },
              listeningExecutorService)
          .get();
    }
  }
}
}
