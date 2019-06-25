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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.http;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /**
     * Checks if an image's BLOB exists on a registry, and retrieves its {@link BlobDescriptor} if it
     * exists.
     */
    internal class BlobChecker : RegistryEndpointProvider<bool>
    {
        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
        private readonly BlobDescriptor blobDescriptor;

        public BlobChecker(
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            BlobDescriptor blobDigest)
        {
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.blobDescriptor = blobDigest;
        }

        /** @return the BLOB's content descriptor */

        public async Task<bool> handleResponseAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return handleResponseSuccess(response);
            }
            else
            {
                return await handleHttpResponseExceptionAsync(response).ConfigureAwait(false);
            }
        }

        public bool handleResponseSuccess(HttpResponseMessage response)
        {
            long? contentLength = response.getContentLength();
            if (contentLength < 0 || contentLength == null)
            {
                throw new RegistryErrorExceptionBuilder(getActionDescription())
                    .addReason("Did not receive Content-Length header")
                    .build();
            }

            if(blobDescriptor.getSize() > 0 && contentLength.GetValueOrDefault() != blobDescriptor.getSize())
            {
                throw new RegistryErrorExceptionBuilder(getActionDescription())
                    .addReason($"Expected size {blobDescriptor.getSize()} but got {contentLength.GetValueOrDefault()}")
                    .build();
            }

            return true;
        }

        public async Task<bool> handleHttpResponseExceptionAsync(HttpResponseMessage httpResponse)
        {
            if (httpResponse.getStatusCode() != HttpStatusCode.NotFound)
            {
                throw new HttpResponseException(httpResponse);
            }

            // Finds a BLOB_UNKNOWN error response code.
            if (string.IsNullOrEmpty(await httpResponse.getContentAsync().ConfigureAwait(false)))
            {
                // TODO: The Google HTTP client gives null content for HEAD requests. Make the content never
                // be null, even for HEAD requests.
                return false;
            }

            ErrorCode errorCode = await ErrorResponseUtil.getErrorCodeAsync(httpResponse).ConfigureAwait(false);
            if (errorCode == ErrorCode.BlobUnknown)
            {
                return false;
            }

            // BLOB_UNKNOWN was not found as a error response code.
            throw new HttpResponseException(httpResponse);
        }

        public Uri getApiRoute(string apiRouteBase)
        {
            return new Uri(
                apiRouteBase + registryEndpointRequestProperties.getImageName() + "/blobs/" + blobDescriptor.getDigest());
        }

        public BlobHttpContent getContent()
        {
            return null;
        }

        public IList<string> getAccept()
        {
            return Collections.emptyList<string>();
        }

        public HttpMethod getHttpMethod()
        {
            return HttpMethod.Head;
        }

        public string getActionDescription()
        {
            return "check BLOB exists for "
                + registryEndpointRequestProperties.getRegistry()
                + "/"
                + registryEndpointRequestProperties.getImageName()
                + " with digest "
                + blobDescriptor.getDigest();
        }
    }
}
