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

using Fib.Net.Core.Blob;
using Fib.Net.Core.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fib.Net.Core.Registry
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
            blobDescriptor = blobDigest;
        }

        /** @return the BLOB's content descriptor */

        public async Task<bool> HandleResponseAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return HandleResponseSuccess(response);
            }
            else
            {
                return await HandleHttpResponseExceptionAsync(response).ConfigureAwait(false);
            }
        }

        public bool HandleResponseSuccess(HttpResponseMessage response)
        {
            long? contentLength = response.Content.Headers.ContentLength;
            if (contentLength < 0 || contentLength == null)
            {
                throw new RegistryErrorExceptionBuilder(GetActionDescription())
                    .AddReason("Did not receive Content-Length header")
                    .Build();
            }

            if (blobDescriptor.GetSize() > 0 && contentLength.GetValueOrDefault() != blobDescriptor.GetSize())
            {
                throw new RegistryErrorExceptionBuilder(GetActionDescription())
                    .AddReason($"Expected size {blobDescriptor.GetSize()} but got {contentLength.GetValueOrDefault()}")
                    .Build();
            }

            return true;
        }

        public async Task<bool> HandleHttpResponseExceptionAsync(HttpResponseMessage httpResponse)
        {
            if (httpResponse.StatusCode != HttpStatusCode.NotFound)
            {
                throw new HttpResponseException(httpResponse);
            }

            // Finds a BLOB_UNKNOWN error response code.
            if (string.IsNullOrEmpty(await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false)))
            {
                // TODO: The Google HTTP client gives null content for HEAD requests. Make the content never
                // be null, even for HEAD requests.
                return false;
            }

            ErrorCode errorCode = await ErrorResponseUtil.GetErrorCodeAsync(httpResponse).ConfigureAwait(false);
            if (errorCode == ErrorCode.BlobUnknown)
            {
                return false;
            }

            // BLOB_UNKNOWN was not found as a error response code.
            throw new HttpResponseException(httpResponse);
        }

        public Uri GetApiRoute(string apiRouteBase)
        {
            return new Uri(
                apiRouteBase + registryEndpointRequestProperties.GetImageName() + "/blobs/" + blobDescriptor.GetDigest());
        }

        public BlobHttpContent GetContent()
        {
            return null;
        }

        public IList<string> GetAccept()
        {
            return new List<string>();
        }

        public HttpMethod GetHttpMethod()
        {
            return HttpMethod.Head;
        }

        public string GetActionDescription()
        {
            return "check BLOB exists for "
                + registryEndpointRequestProperties.GetRegistry()
                + "/"
                + registryEndpointRequestProperties.GetImageName()
                + " with digest "
                + blobDescriptor.GetDigest();
        }
    }
}
