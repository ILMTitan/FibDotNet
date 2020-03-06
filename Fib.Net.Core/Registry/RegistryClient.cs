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

using Fib.Net.Core.Api;
using Fib.Net.Core.Blob;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Events.Time;
using Fib.Net.Core.Http;
using Fib.Net.Core.Images.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Fib.Net.Core.Registry
{
    /** Interfaces with a registry. */
    public sealed class RegistryClient
    {
        public static readonly ProductInfoHeaderValue defaultFibUserAgent =
            new ProductInfoHeaderValue(new ProductHeaderValue("fib", ProjectInfo.VERSION));

        /** Factory for creating {@link RegistryClient}s. */
        public class Factory
        {
            private readonly IEventHandlers eventHandlers;
            private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;

            private bool allowInsecureRegistries = false;
            private readonly List<ProductInfoHeaderValue> additionalUserAgentValues = new List<ProductInfoHeaderValue>();
            private Authorization authorization;

            public Factory(
                IEventHandlers eventHandlers,
                RegistryEndpointRequestProperties registryEndpointRequestProperties)
            {
                this.eventHandlers = eventHandlers;
                this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            }

            /**
             * Sets whether or not to allow insecure registries (ignoring certificate validation failure or
             * communicating over HTTP if all else fail).
             *
             * @param allowInsecureRegistries if {@code true}, insecure connections will be allowed
             * @return this
             */
            public Factory SetAllowInsecureRegistries(bool allowInsecureRegistries)
            {
                this.allowInsecureRegistries = allowInsecureRegistries;
                return this;
            }

            /**
             * Sets the authentication credentials to use to authenticate with the registry.
             *
             * @param authorization the {@link Authorization} to access the registry/repository
             * @return this
             */
            public Factory SetAuthorization(Authorization authorization)
            {
                this.authorization = authorization;
                return this;
            }

            /**
             * Sets a suffix to append to {@code User-Agent} headers.
             *
             * @param userAgentSuffix the suffix to append
             * @return this
             */
            public Factory AddUserAgentValues(IEnumerable<ProductInfoHeaderValue> userAgentSuffix)
            {
                additionalUserAgentValues.AddRange(userAgentSuffix);
                return this;
            }

            /**
             * Sets a suffix to append to {@code User-Agent} headers.
             *
             * @param userAgentSuffix the suffix to append
             * @return this
             */
            public Factory AddUserAgentValue(ProductInfoHeaderValue userAgentSuffix)
            {
                if (userAgentSuffix != null)
                {
                    additionalUserAgentValues.Add(userAgentSuffix);
                }
                return this;
            }

            /**
             * Creates a new {@link RegistryClient}.
             *
             * @return the new {@link RegistryClient}
             */
            public RegistryClient NewRegistryClient()
            {
                return new RegistryClient(
                    eventHandlers,
                    authorization,
                    registryEndpointRequestProperties,
                    allowInsecureRegistries,
                    MakeUserAgent());
            }

            /**
             * The {@code User-Agent} is in the form of {@code fib <version> <type>}. For example: {@code
             * fib 0.9.0 fib-maven-plugin}.
             *
             * @return the {@code User-Agent} header to send. The {@code User-Agent} can be disabled by
             *     setting the system property variable {@code _JIB_DISABLE_USER_AGENT} to any non-empty
             *     string.
             */
            private IEnumerable<ProductInfoHeaderValue> MakeUserAgent()
            {
                if (!FibSystemProperties.IsUserAgentEnabled())
                {
                    yield break;
                }

                yield return defaultFibUserAgent;
                foreach (var value in additionalUserAgentValues)
                {
                    yield return value;
                }
            }
        }

        /**
         * Creates a new {@link Factory} for building a {@link RegistryClient}.
         *
         * @param eventHandlers the event handlers used for dispatching log events
         * @param serverUrl the server Uri for the registry (for example, {@code gcr.io})
         * @param imageName the image/repository name (also known as, namespace)
         * @return the new {@link Factory}
         */
        public static Factory CreateFactory(IEventHandlers eventHandlers, string registry, string imageName)
        {
            return new Factory(eventHandlers, new RegistryEndpointRequestProperties(registry, imageName));
        }

        private readonly IEventHandlers eventHandlers;
        private readonly Authorization authorization;
        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
        private readonly bool allowInsecureRegistries;
        private readonly IEnumerable<ProductInfoHeaderValue> userAgent;

        /**
         * Instantiate with {@link #factory}.
         *
         * @param eventHandlers the event handlers used for dispatching log events
         * @param authorization the {@link Authorization} to access the registry/repository
         * @param registryEndpointRequestProperties properties of registry endpoint requests
         * @param allowInsecureRegistries if {@code true}, insecure connections will be allowed
         */
        private RegistryClient(
            IEventHandlers eventHandlers,
            Authorization authorization,
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            bool allowInsecureRegistries,
            IEnumerable<ProductInfoHeaderValue> userAgent)
        {
            this.eventHandlers = eventHandlers;
            this.authorization = authorization;
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.allowInsecureRegistries = allowInsecureRegistries;
            this.userAgent = userAgent;
        }

        /**
         * @return the {@link RegistryAuthenticator} to authenticate pulls/pushes with the registry, or
         *     {@code null} if no token authentication is necessary
         * @throws IOException if communicating with the endpoint fails
         * @throws RegistryException if communicating with the endpoint fails
         */
        public async Task<RegistryAuthenticator> GetRegistryAuthenticatorAsync()
        {
            // Gets the WWW-Authenticate header (eg. 'WWW-Authenticate: Bearer
            // realm="https://gcr.io/v2/token",service="gcr.io"')
            return await CallRegistryEndpointAsync(
                new AuthenticationMethodRetriever(registryEndpointRequestProperties, GetUserAgent())).ConfigureAwait(false);
        }

        /**
         * Pulls the image manifest for a specific tag.
         *
         * @param <T> child type of ManifestTemplate
         * @param imageTag the tag to pull on
         * @param manifestTemplateClass the specific version of manifest template to pull, or {@link
         *     ManifestTemplate} to pull either {@link V22ManifestTemplate} or {@link V21ManifestTemplate}
         * @return the manifest template
         * @throws IOException if communicating with the endpoint fails
         * @throws RegistryException if communicating with the endpoint fails
         */
        public async Task<T> PullManifestAsync<T>(
            string imageTag) where T : IManifestTemplate
        {
            ManifestPuller<T> manifestPuller =
                new ManifestPuller<T>(registryEndpointRequestProperties, imageTag);
            T manifestTemplate = await CallRegistryEndpointAsync(manifestPuller).ConfigureAwait(false);
            if (manifestTemplate == null)
            {
                throw new InvalidOperationException(Resources.RegistryClientManifestPullerReturnedNullExceptionMessage);
            }
            return manifestTemplate;
        }

        public async Task<IManifestTemplate> PullManifestAsync(string imageTag)
        {
            return await PullAnyManifestAsync(imageTag).ConfigureAwait(false);
        }

        /**
         * Pulls the image manifest for a specific tag.
         *
         * @param <T> child type of ManifestTemplate
         * @param imageTag the tag to pull on
         * @param manifestTemplateClass the specific version of manifest template to pull, or {@link
         *     ManifestTemplate} to pull either {@link V22ManifestTemplate} or {@link V21ManifestTemplate}
         * @return the manifest template
         * @throws IOException if communicating with the endpoint fails
         * @throws RegistryException if communicating with the endpoint fails
         */
        public async Task<IManifestTemplate> PullAnyManifestAsync(string imageTag)
        {
            ManifestPuller manifestPuller =
                new ManifestPuller(registryEndpointRequestProperties, imageTag);
            IManifestTemplate manifestTemplate = await CallRegistryEndpointAsync(manifestPuller).ConfigureAwait(false);
            if (manifestTemplate == null)
            {
                throw new InvalidOperationException(Resources.RegistryClientManifestPullerReturnedNullExceptionMessage);
            }
            return manifestTemplate;
        }

        /**
         * Pushes the image manifest for a specific tag.
         *
         * @param manifestTemplate the image manifest
         * @param imageTag the tag to push on
         * @return the digest of the pushed image
         * @throws IOException if communicating with the endpoint fails
         * @throws RegistryException if communicating with the endpoint fails
         */
        public async Task<DescriptorDigest> PushManifestAsync(IBuildableManifestTemplate manifestTemplate, string imageTag)
        {
            ManifestPusher pusher = new ManifestPusher(registryEndpointRequestProperties, manifestTemplate, imageTag, eventHandlers);
            DescriptorDigest descriptorDigest = await CallRegistryEndpointAsync(pusher).ConfigureAwait(false);
            Debug.Assert(descriptorDigest != null);
            return descriptorDigest;
        }

        /**
         * @param blobDigest the blob digest to check for
         * @return the BLOB's {@link BlobDescriptor} if the BLOB exists on the registry, or {@code null}
         *     if it doesn't
         * @throws IOException if communicating with the endpoint fails
         * @throws RegistryException if communicating with the endpoint fails
         */
        public async Task<bool> CheckBlobAsync(BlobDescriptor blobDigest)
        {
            BlobChecker blobChecker = new BlobChecker(registryEndpointRequestProperties, blobDigest);
            return await CallRegistryEndpointAsync(blobChecker).ConfigureAwait(false);
        }

        /**
         * Gets the BLOB referenced by {@code blobDigest}. Note that the BLOB is only pulled when it is
         * written out.
         *
         * @param blobDigest the digest of the BLOB to download
         * @param blobSizeListener callback to receive the total size of the BLOb to pull
         * @param writtenByteCountListener listens on byte count written to an output stream during the
         *     pull
         * @return a {@link Blob}
         */
        public IBlob PullBlob(
            DescriptorDigest blobDigest,
            Action<long> blobSizeListener,
            Action<long> writtenByteCountListener)
        {
            return Blobs.From(
                async outputStream =>
                {
                    try
                    {
                        await CallRegistryEndpointAsync(new BlobPuller(
                        registryEndpointRequestProperties,
                        blobDigest,
                        outputStream,
                        blobSizeListener,
                        writtenByteCountListener)).ConfigureAwait(false);
                    }
                    catch (RegistryException ex)
                    {
                        throw new IOException("", ex);
                    }
                }, -1);
        }

        /**
         * Pushes the BLOB. If the {@code sourceRepository} is provided then the remote registry may skip
         * if the BLOB already exists on the registry.
         *
         * @param blobDigest the digest of the BLOB, used for existence-check
         * @param blob the BLOB to push
         * @param sourceRepository if pushing to the same registry then the source image, or {@code null}
         *     otherwise; used to optimize the BLOB push
         * @param writtenByteCountListener listens on byte count written to the registry during the push
         * @return {@code true} if the BLOB already exists on the registry and pushing was skipped; false
         *     if the BLOB was pushed
         * @throws IOException if communicating with the endpoint fails
         * @throws RegistryException if communicating with the endpoint fails
         */
        public async Task<bool> PushBlobAsync(
            DescriptorDigest blobDigest,
            IBlob blob,
            string sourceRepository,
            Action<long> writtenByteCountListener)
        {
            BlobPusher blobPusher =
                new BlobPusher(registryEndpointRequestProperties, blobDigest, blob, sourceRepository);

            using (TimerEventDispatcher timerEventDispatcher =
                new TimerEventDispatcher(eventHandlers, "pushBlob"))
            {
                timerEventDispatcher.Lap("pushBlob POST " + blobDigest);
                
                // POST /v2/<name>/blobs/uploads/ OR
                // POST /v2/<name>/blobs/uploads/?mount={blob.digest}&from={sourceRepository}
                Uri patchLocation = await CallRegistryEndpointAsync(blobPusher.CreateInitializer()).ConfigureAwait(false);
                if (patchLocation == null)
                {
                    // The BLOB exists already.
                    return true;
                }
                timerEventDispatcher.Lap("pushBlob PATCH " + blobDigest);

                // PATCH <Location> with BLOB
                Uri putLocation =
                    await CallRegistryEndpointAsync(blobPusher.CreateWriter(patchLocation, writtenByteCountListener)).ConfigureAwait(false);
                Preconditions.CheckNotNull(putLocation);
                timerEventDispatcher.Lap("pushBlob PUT " + blobDigest);

                // PUT <Location>?digest={blob.digest}
                await CallRegistryEndpointAsync(blobPusher.CreateCommitter(putLocation)).ConfigureAwait(false);

                return false;
            }
        }

        /** @return the registry endpoint's API root, without the protocol */

        public string GetApiRouteBase()
        {
            return registryEndpointRequestProperties.GetRegistry() + "/v2/";
        }

        public IEnumerable<ProductInfoHeaderValue> GetUserAgent()
        {
            return userAgent;
        }

        /**
         * Calls the registry endpoint.
         *
         * @param registryEndpointProvider the {@link RegistryEndpointProvider} to the endpoint
         * @throws IOException if communicating with the endpoint fails
         * @throws RegistryException if communicating with the endpoint fails
         */
        private async Task<T> CallRegistryEndpointAsync<T>(RegistryEndpointProvider<T> registryEndpointProvider)
        {
            return await new RegistryEndpointCaller<T>(
                    eventHandlers,
                    userAgent,
                    GetApiRouteBase(),
                    registryEndpointProvider,
                    authorization,
                    registryEndpointRequestProperties,
                    allowInsecureRegistries)
                .CallAsync().ConfigureAwait(false);
        }
    }
}
