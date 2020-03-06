// Copyright 2017 Google LLC.
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
using Fib.Net.Core.Events;
using Fib.Net.Core.Hash;
using Fib.Net.Core.Http;
using Fib.Net.Core.Images.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fib.Net.Core.Registry
{
    /** Pushes an image's manifest. */
    internal class ManifestPusher : RegistryEndpointProvider<DescriptorDigest>
    {
        /** Response header containing digest of pushed image. */
        private const string RESPONSE_DIGEST_HEADER = "Docker-Content-Digest";

        /**
         * Makes the warning for when the registry responds with an image digest that is not the expected
         * digest of the image.
         *
         * @param expectedDigest the expected image digest
         * @param receivedDigests the received image digests
         * @return the warning message
         */
        private static string MakeUnexpectedImageDigestWarning(
            DescriptorDigest expectedDigest, IList<string> receivedDigests)
        {
            if (receivedDigests.Count == 0)
            {
                return "Expected image digest " + expectedDigest + ", but received none";
            }

            StringJoiner message =
                new StringJoiner(", ", "Expected image digest " + expectedDigest + ", but received: ", "");
            foreach (string receivedDigest in receivedDigests)
            {
                message.Add(receivedDigest);
            }
            return message.ToString();
        }

        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
        private readonly IBuildableManifestTemplate manifestTemplate;
        private readonly string imageTag;
        private readonly IEventHandlers eventHandlers;

        public ManifestPusher(
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            IBuildableManifestTemplate manifestTemplate,
            string imageTag,
            IEventHandlers eventHandlers)
        {
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.manifestTemplate = manifestTemplate;
            this.imageTag = imageTag;
            this.eventHandlers = eventHandlers;
        }

        public BlobHttpContent GetContent()
        {
            // TODO: Consider giving progress on manifest push as well?
            return new BlobHttpContent(
                Blobs.FromJson(manifestTemplate), manifestTemplate.GetManifestMediaType());
        }

        public IList<string> GetAccept()
        {
            return new List<string>();
        }

        public async Task<DescriptorDigest> HandleHttpResponseExceptionAsync(HttpResponseMessage httpResponse)
        {
            // docker registry 2.0 and 2.1 returns:
            //   400 Bad Request
            //   {"errors":[{"code":"TAG_INVALID","message":"manifest tag did not match URI"}]}
            // docker registry:2.2 returns:
            //   400 Bad Request
            //   {"errors":[{"code":"MANIFEST_INVALID","message":"manifest invalid","detail":{}}]}
            // quay.io returns:
            //   415 UNSUPPORTED MEDIA TYPE
            //   {"errors":[{"code":"MANIFEST_INVALID","detail":
            //   {"message":"manifest schema version not supported"},"message":"manifest invalid"}]}

            if (httpResponse.StatusCode != HttpStatusCode.BadRequest
                && httpResponse.StatusCode != HttpStatusCode.UnsupportedMediaType)
            {
                throw new HttpResponseException(httpResponse);
            }

            ErrorCode errorCode = await ErrorResponseUtil.GetErrorCodeAsync(httpResponse).ConfigureAwait(false);
            if (errorCode == ErrorCode.ManifestInvalid || errorCode == ErrorCode.TagInvalid)
            {
                throw new RegistryErrorExceptionBuilder(GetActionDescription(), httpResponse)
                    .AddReason(
                        "Registry may not support pushing OCI Manifest or "
                            + "Docker Image Manifest Version 2, Schema 2")
                    .Build();
            }
            // rethrow: unhandled error response code.
            throw new HttpResponseException(httpResponse);
        }

        public async Task<DescriptorDigest> HandleResponseAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return await HandleHttpResponseExceptionAsync(response).ConfigureAwait(false);
            }
            return await HandleSuccessResponseAsync(response).ConfigureAwait(false);
        }

        public async Task<DescriptorDigest> HandleSuccessResponseAsync(HttpResponseMessage response)
        {
            // Checks if the image digest is as expected.
            DescriptorDigest expectedDigest = await Digests.ComputeJsonDigestAsync(manifestTemplate).ConfigureAwait(false);

            if (response.Headers.TryGetValues(RESPONSE_DIGEST_HEADER, out var receivedDigestEnum))
            {
                var receivedDigests = receivedDigestEnum.ToList();
                if (receivedDigests.Count == 1)
                {
                    try
                    {
                        if (expectedDigest.Equals(DescriptorDigest.FromDigest(receivedDigests[0])))
                        {
                            return expectedDigest;
                        }
                    }
                    catch (DigestException)
                    {
                        // Invalid digest.
                    }
                }
                eventHandlers.Dispatch(
                    LogEvent.Warn(MakeUnexpectedImageDigestWarning(expectedDigest, receivedDigests)));
                return expectedDigest;
            }

            eventHandlers.Dispatch(
                LogEvent.Warn(MakeUnexpectedImageDigestWarning(expectedDigest, Array.Empty<string>())));
            // The received digest is not as expected. Warns about this.
            return expectedDigest;
        }

        public Uri GetApiRoute(string apiRouteBase)
        {
            return new Uri(
                apiRouteBase + registryEndpointRequestProperties.GetImageName() + "/manifests/" + imageTag);
        }

        public HttpMethod GetHttpMethod()
        {
            return HttpMethod.Put;
        }

        public string GetActionDescription()
        {
            return "push image manifest for "
                + registryEndpointRequestProperties.GetRegistry()
                + "/"
                + registryEndpointRequestProperties.GetImageName()
                + ":"
                + imageTag;
        }
    }
}
