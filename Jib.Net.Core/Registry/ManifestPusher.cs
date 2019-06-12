/*
 * Copyright 2017 Google LLC.
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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace com.google.cloud.tools.jib.registry
{









    /** Pushes an image's manifest. */
    internal class ManifestPusher : RegistryEndpointProvider<DescriptorDigest>
    {
        /** Response header containing digest of pushed image. */
        private static readonly string RESPONSE_DIGEST_HEADER = "Docker-Content-Digest";

        /**
         * Makes the warning for when the registry responds with an image digest that is not the expected
         * digest of the image.
         *
         * @param expectedDigest the expected image digest
         * @param receivedDigests the received image digests
         * @return the warning message
         */
        private static string makeUnexpectedImageDigestWarning(
            DescriptorDigest expectedDigest, IList<string> receivedDigests)
        {
            if (receivedDigests.isEmpty())
            {
                return "Expected image digest " + expectedDigest + ", but received none";
            }

            StringJoiner message =
                new StringJoiner(", ", "Expected image digest " + expectedDigest + ", but received: ", "");
            foreach (string receivedDigest in receivedDigests)
            {
                message.add(receivedDigest);
            }
            return message.toString();
        }

        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
        private readonly BuildableManifestTemplate manifestTemplate;
        private readonly string imageTag;
        private readonly EventHandlers eventHandlers;

        public ManifestPusher(
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            BuildableManifestTemplate manifestTemplate,
            string imageTag,
            EventHandlers eventHandlers)
        {
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.manifestTemplate = manifestTemplate;
            this.imageTag = imageTag;
            this.eventHandlers = eventHandlers;
        }

        public BlobHttpContent getContent()
        {
            // TODO: Consider giving progress on manifest push as well?
            return new BlobHttpContent(
                Blobs.from(manifestTemplate), manifestTemplate.getManifestMediaType());
        }

        public IList<string> getAccept()
        {
            return Collections.emptyList<string>();
        }

        public DescriptorDigest handleHttpResponseException(HttpResponseMessage httpResponse)
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

            if (httpResponse.getStatusCode() != HttpStatusCode.BadRequest
                && httpResponse.getStatusCode() != HttpStatusCode.UnsupportedMediaType)
            {
                throw new HttpResponseException(httpResponse);
            }

            ErrorCodes errorCode = ErrorResponseUtil.getErrorCode(httpResponse);
            if (errorCode == ErrorCodes.MANIFEST_INVALID || errorCode == ErrorCodes.TAG_INVALID)
            {
                throw new RegistryErrorExceptionBuilder(getActionDescription(), httpResponse)
                    .addReason(
                        "Registry may not support pushing OCI Manifest or "
                            + "Docker Image Manifest Version 2, Schema 2")
                    .build();
            }
            // rethrow: unhandled error response code.
            throw new HttpResponseException(httpResponse);
        }

        public DescriptorDigest handleResponse(HttpResponseMessage response)
        {
            // Checks if the image digest is as expected.
            DescriptorDigest expectedDigest = Digests.computeJsonDigest(manifestTemplate);

            IList<string> receivedDigests = response.getHeader(RESPONSE_DIGEST_HEADER);
            if (receivedDigests.size() == 1)
            {
                try
                {
                    DescriptorDigest receivedDigest = DescriptorDigest.fromDigest(receivedDigests.get(0));
                    if (expectedDigest.Equals(receivedDigest))
                    {
                        return expectedDigest;
                    }
                }
                catch (DigestException)
                {
                    // Invalid digest.
                }
            }

            // The received digest is not as expected. Warns about this.
            eventHandlers.dispatch(
                LogEvent.warn(makeUnexpectedImageDigestWarning(expectedDigest, receivedDigests)));
            return expectedDigest;
        }

        public Uri getApiRoute(string apiRouteBase)
        {
            return new Uri(
                apiRouteBase + registryEndpointRequestProperties.getImageName() + "/manifests/" + imageTag);
        }

        public HttpMethod getHttpMethod()
        {
            return HttpMethod.Put;
        }

        public string getActionDescription()
        {
            return "push image manifest for "
                + registryEndpointRequestProperties.getServerUrl()
                + "/"
                + registryEndpointRequestProperties.getImageName()
                + ":"
                + imageTag;
        }
    }
}
