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


















/** Pushes a BLOB to the target registry. */
class PushBlobStep : AsyncStep<BlobDescriptor>, Callable<BlobDescriptor>  {
  private static readonly string DESCRIPTION = "Pushing BLOB ";

  private readonly BuildConfiguration buildConfiguration;
  private readonly ProgressEventDispatcher.Factory progressEventDipatcherFactory;

  private readonly AuthenticatePushStep authenticatePushStep;
  private readonly BlobDescriptor blobDescriptor;
  private readonly Blob blob;

  private readonly ListenableFuture<BlobDescriptor> listenableFuture;

  PushBlobStep(
      ListeningExecutorService listeningExecutorService,
      BuildConfiguration buildConfiguration,
      ProgressEventDispatcher.Factory progressEventDipatcherFactory,
      AuthenticatePushStep authenticatePushStep,
      BlobDescriptor blobDescriptor,
      Blob blob) {
    this.buildConfiguration = buildConfiguration;
    this.progressEventDipatcherFactory = progressEventDipatcherFactory;
    this.authenticatePushStep = authenticatePushStep;
    this.blobDescriptor = blobDescriptor;
    this.blob = blob;

    listenableFuture =
        AsyncDependencies.@using(listeningExecutorService)
            .addStep(authenticatePushStep)
            .whenAllSucceed(this);
  }

  public ListenableFuture<BlobDescriptor> getFuture() {
    return listenableFuture;
  }

  public BlobDescriptor call() {
    using(ProgressEventDispatcher progressEventDispatcher =
            progressEventDipatcherFactory.create(
                "pushing blob " + blobDescriptor.getDigest(), blobDescriptor.getSize()))
    using(TimerEventDispatcher ignored =
            new TimerEventDispatcher(
                buildConfiguration.getEventHandlers(), DESCRIPTION + blobDescriptor);
        ThrottledAccumulatingConsumer throttledProgressReporter =
            new ThrottledAccumulatingConsumer(progressEventDispatcher.dispatchProgress)))
    {

      RegistryClient registryClient =
          buildConfiguration
              .newTargetImageRegistryClientFactory()
              .setAuthorization(NonBlockingSteps.get(authenticatePushStep))
              .newRegistryClient();

      // check if the BLOB is available
      if (registryClient.checkBlob(blobDescriptor.getDigest()) != null) {
        buildConfiguration
            .getEventHandlers()
            .dispatch(LogEvent.info("BLOB : " + blobDescriptor + " already exists on registry"));
        return blobDescriptor;
      }

      // todo: leverage cross-repository mounts
      registryClient.pushBlob(blobDescriptor.getDigest(), blob, null, throttledProgressReporter);

      return blobDescriptor;
    }
  }
}
}
