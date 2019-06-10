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

namespace com.google.cloud.tools.jib.registry {













/**
 * Checks if an image's BLOB exists on a registry, and retrieves its {@link BlobDescriptor} if it
 * exists.
 */
class BlobChecker : RegistryEndpointProvider<BlobDescriptor> {
  private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
  private readonly DescriptorDigest blobDigest;

  public BlobChecker(
      RegistryEndpointRequestProperties registryEndpointRequestProperties,
      DescriptorDigest blobDigest) {
    this.registryEndpointRequestProperties = registryEndpointRequestProperties;
    this.blobDigest = blobDigest;
  }

        /** @return the BLOB's content descriptor */

        public BlobDescriptor handleResponse(HttpResponseMessage response) {
            if (response.IsSuccessStatusCode)
            {
                return handleResponseSuccess(response);
            } else
            {
                return handleHttpResponseException(response);
            }
        }
        public BlobDescriptor handleResponseSuccess(HttpResponseMessage response) { 
    long? contentLength = response.getContentLength();
    if (contentLength < 0 || contentLength == null) {
      throw new RegistryErrorExceptionBuilder(getActionDescription())
          .addReason("Did not receive Content-Length header")
          .build();
    }

    return new BlobDescriptor(contentLength.GetValueOrDefault(), blobDigest);
  }

  public BlobDescriptor handleHttpResponseException(HttpResponseMessage httpResponse)
      {
    if (httpResponse.getStatusCode() != HttpStatusCode.NotFound) {
      throw new HttpResponseException(httpResponse);
    }

    // Finds a BLOB_UNKNOWN error response code.
    if (httpResponse.getContent() == null) {
      // TODO: The Google HTTP client gives null content for HEAD requests. Make the content never
      // be null, even for HEAD requests.
      return null;
    }

    ErrorCodes errorCode = ErrorResponseUtil.getErrorCode(httpResponse);
    if (errorCode == ErrorCodes.BLOB_UNKNOWN) {
      return null;
    }

    // BLOB_UNKNOWN was not found as a error response code.
    throw new HttpResponseException(httpResponse);
  }

  public Uri getApiRoute(string apiRouteBase) {
    return new Uri(
        apiRouteBase + registryEndpointRequestProperties.getImageName() + "/blobs/" + blobDigest);
  }

  public BlobHttpContent getContent() {
    return null;
  }

  public IList<string> getAccept() {
    return Collections.emptyList<string>();
  }

  public HttpMethod getHttpMethod() {
    return HttpMethod.Head;
  }

  public string getActionDescription() {
    return "check BLOB exists for "
        + registryEndpointRequestProperties.getServerUrl()
        + "/"
        + registryEndpointRequestProperties.getImageName()
        + " with digest "
        + blobDigest;
  }
}
}
