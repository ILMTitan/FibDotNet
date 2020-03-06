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

using Fib.Net.Core.Async;
using Fib.Net.Core.Blob;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Events;
using Fib.Net.Core.Events.Progress;
using Fib.Net.Core.Events.Time;
using Fib.Net.Core.Http;
using Fib.Net.Core.Registry;
using System.Threading.Tasks;

namespace Fib.Net.Core.BuildSteps
{
    /** Pushes a BLOB to the target registry. */
    internal class PushBlobStep : IAsyncStep<BlobDescriptor>
    {
        private const string DESCRIPTION = "Pushing BLOB ";

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDipatcherFactory;

        private readonly AuthenticatePushStep authenticatePushStep;
        private readonly BlobDescriptor blobDescriptor;
        private readonly IBlob blob;

        private readonly Task<BlobDescriptor> listenableFuture;

        public PushBlobStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDipatcherFactory,
            AuthenticatePushStep authenticatePushStep,
            BlobDescriptor blobDescriptor,
            IBlob blob)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDipatcherFactory = progressEventDipatcherFactory;
            this.authenticatePushStep = authenticatePushStep;
            this.blobDescriptor = blobDescriptor;
            this.blob = blob;

            listenableFuture = CallAsync();
        }

        public Task<BlobDescriptor> GetFuture()
        {
            return listenableFuture;
        }

        public async Task<BlobDescriptor> CallAsync()
        {
            Authorization authorization = await authenticatePushStep.GetFuture().ConfigureAwait(false);
            using (ProgressEventDispatcher progressEventDispatcher =
                    progressEventDipatcherFactory.Create(
                        "pushing blob " + blobDescriptor.GetDigest(), blobDescriptor.GetSize()))
            using (TimerEventDispatcher ignored =
                    new TimerEventDispatcher(
                        buildConfiguration.GetEventHandlers(), DESCRIPTION + blobDescriptor))
            using (ThrottledAccumulatingConsumer throttledProgressReporter =
                new ThrottledAccumulatingConsumer(progressEventDispatcher.DispatchProgress))
            {
                RegistryClient registryClient =
                    buildConfiguration
                        .NewTargetImageRegistryClientFactory()
                        .SetAuthorization(authorization)
                        .NewRegistryClient();

                // check if the BLOB is available
                if (await registryClient.CheckBlobAsync(blobDescriptor).ConfigureAwait(false))
                {
                    buildConfiguration
                        .GetEventHandlers()
                        .Dispatch(LogEvent.Info("BLOB : " + blobDescriptor + " already exists on registry"));
                    return blobDescriptor;
                }

                // todo: leverage cross-repository mounts
                await registryClient.PushBlobAsync(blobDescriptor.GetDigest(), blob, null, throttledProgressReporter.Accept).ConfigureAwait(false);

                return blobDescriptor;
            }
        }
    }
}
