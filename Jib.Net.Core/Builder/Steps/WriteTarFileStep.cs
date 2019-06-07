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




















public class WriteTarFileStep : AsyncStep<BuildResult>, Callable<BuildResult> {

  private readonly ListeningExecutorService listeningExecutorService;
  private readonly BuildConfiguration buildConfiguration;
  private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

  private readonly Path outputPath;
  private readonly PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep;
  private readonly ImmutableList<BuildAndCacheApplicationLayerStep> buildAndCacheApplicationLayerSteps;
  private readonly BuildImageStep buildImageStep;

  private readonly ListenableFuture<BuildResult> listenableFuture;

  WriteTarFileStep(
      ListeningExecutorService listeningExecutorService,
      BuildConfiguration buildConfiguration,
      ProgressEventDispatcher.Factory progressEventDispatcherFactory,
      Path outputPath,
      PullAndCacheBaseImageLayersStep pullAndCacheBaseImageLayersStep,
      ImmutableList<BuildAndCacheApplicationLayerStep> buildAndCacheApplicationLayerSteps,
      BuildImageStep buildImageStep) {
    this.listeningExecutorService = listeningExecutorService;
    this.buildConfiguration = buildConfiguration;
    this.progressEventDispatcherFactory = progressEventDispatcherFactory;
    this.outputPath = outputPath;
    this.pullAndCacheBaseImageLayersStep = pullAndCacheBaseImageLayersStep;
    this.buildAndCacheApplicationLayerSteps = buildAndCacheApplicationLayerSteps;
    this.buildImageStep = buildImageStep;

    listenableFuture =
        AsyncDependencies.@using(listeningExecutorService)
            .addStep(pullAndCacheBaseImageLayersStep)
            .addStep(buildImageStep)
            .whenAllSucceed(this);
  }

  public ListenableFuture<BuildResult> getFuture() {
    return listenableFuture;
  }

  public BuildResult call() {
    return AsyncDependencies.@using(listeningExecutorService)
        .addSteps(NonBlockingSteps.get(pullAndCacheBaseImageLayersStep))
        .addSteps(buildAndCacheApplicationLayerSteps)
        .addStep(NonBlockingSteps.get(buildImageStep))
        .whenAllSucceed(this.writeTarFile)
        .get();
  }

  private BuildResult writeTarFile() {
    buildConfiguration
        .getEventHandlers()
        .dispatch(LogEvent.progress("Building image to tar file..."));

    using (ProgressEventDispatcher ignored =
        progressEventDispatcherFactory.create("writing to tar file", 1)) {
      Image image = NonBlockingSteps.get(NonBlockingSteps.get(buildImageStep));

      // Builds the image to a tarball.
      Files.createDirectories(outputPath.getParent());
      using (OutputStream outputStream =
          new BufferedOutputStream(FileOperations.newLockingOutputStream(outputPath))) {
        new ImageTarball(image, buildConfiguration.getTargetImageConfiguration().getImage())
            .writeTo(outputStream);
      }

      return BuildResult.fromImage(image, buildConfiguration.getTargetFormat());
    }
  }
}
}
