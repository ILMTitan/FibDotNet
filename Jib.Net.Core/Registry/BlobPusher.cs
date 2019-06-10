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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.http;
using Jib.Net.Core;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace com.google.cloud.tools.jib.registry {
















/**
 * Pushes an image's BLOB (layer or container configuration).
 *
 * <p>The BLOB is pushed in three stages:
 *
 * <ol>
 *   <li>Initialize - Gets a location back to write the BLOB content to
 *   <li>Write BLOB - Write the BLOB content to the received location
 *   <li>Commit BLOB - Commits the BLOB with its digest
 * </ol>
 */
class BlobPusher {

  private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
  private readonly DescriptorDigest blobDigest;
  private readonly Blob blob;
  private readonly string sourceRepository;

  /** Initializes the BLOB upload. */
  private class Initializer : RegistryEndpointProvider<Uri> {
            private readonly BlobPusher parent;

            public Initializer(BlobPusher parent)
            {
                this.parent = parent;
            }

            public BlobHttpContent getContent() {
      return null;
    }

    public IList<string> getAccept() {
      return Collections.emptyList<string>();
    }

    /**
     * @return a Uri to continue pushing the BLOB to, or {@code null} if the BLOB already exists on
     *     the registry
     */
    public Uri handleResponse(HttpResponseMessage response) {
      switch (response.getStatusCode()) {
                    case HttpStatusCode.Created:
          // The BLOB exists in the registry.
          return null;

        case HttpStatusCode.Accepted:
          return getRedirectLocation(response);

        default:
          throw parent.buildRegistryErrorException(
              "Received unrecognized status code " + response.getStatusCode());
      }
    }

    public Uri getApiRoute(string apiRouteBase) {
      StringBuilder url =
          new StringBuilder(apiRouteBase)
              .append(parent.registryEndpointRequestProperties.getImageName())
              .append("/blobs/uploads/");
      if (parent.sourceRepository != null) {
        url.append("?mount=").append(parent.blobDigest).append("&from=").append(parent.sourceRepository);
      }

      return new Uri(url.toString());
    }

    public HttpMethod getHttpMethod() {
      return HttpMethod.Post;
    }

            public string getActionDescription()
            {
                throw new NotImplementedException();
            }
        }

  /** Writes the BLOB content to the upload location. */
  private class Writer : RegistryEndpointProvider<Uri> {
            private readonly BlobPusher parent;
    private readonly Uri location;
    private readonly Consumer<long> writtenByteCountListener;

    public BlobHttpContent getContent() {
      return new BlobHttpContent(parent.blob, MediaType.OCTET_STREAM.toString(), writtenByteCountListener);
    }

    public IList<string> getAccept() {
      return Collections.emptyList<string>();
    }

    /** @return a Uri to continue pushing the BLOB to */

    public Uri handleResponse(HttpResponseMessage response) {
      // TODO: Handle 204 No Content
      return getRedirectLocation(response);
    }

    public Uri getApiRoute(string apiRouteBase) {
      return location;
    }

    public HttpMethod getHttpMethod() {
                return new HttpMethod("patch");
    }

            public string getActionDescription()
            {
                throw new NotImplementedException();
            }

            public Writer(Uri location, Consumer<long> writtenByteCountListener, BlobPusher parent)
            {
      this.location = location;
      this.writtenByteCountListener = writtenByteCountListener;
                this.parent = parent;
            }
  }

  /** Commits the written BLOB. */
  private class Committer : RegistryEndpointProvider<object> {

    private readonly Uri location;
            private BlobPusher parent;

            public BlobHttpContent getContent() {
      return null;
    }

    public IList<string> getAccept() {
      return Collections.emptyList<string>();
    }

    public object handleResponse(HttpResponseMessage response) {
      return null;
    }

    /** @return {@code location} with query parameter 'digest' set to the BLOB's digest */

    public Uri getApiRoute(string apiRouteBase) {
                UriBuilder builder = new UriBuilder(location);
                builder.Query = "?digest="+ parent.blobDigest;
                return builder.Uri;
    }

    public HttpMethod getHttpMethod() {
      return HttpMethod.Put;
    }

            public string getActionDescription()
            {
                throw new NotImplementedException();
            }

            public Committer(Uri location, BlobPusher parent)
            {
      this.location = location;
                this.parent = parent;
            }
  }

  public BlobPusher(
      RegistryEndpointRequestProperties registryEndpointRequestProperties,
      DescriptorDigest blobDigest,
      Blob blob,
      string sourceRepository) {
    this.registryEndpointRequestProperties = registryEndpointRequestProperties;
    this.blobDigest = blobDigest;
    this.blob = blob;
    this.sourceRepository = sourceRepository;
  }

  /**
   * @return a {@link RegistryEndpointProvider} for initializing the BLOB upload with an existence
   *     check
   */
  public RegistryEndpointProvider<Uri> initializer() {
    return new Initializer(this);
  }

  /**
   * @param location the upload Uri
   * @param blobProgressListener the listener for {@link Blob} push progress
   * @return a {@link RegistryEndpointProvider} for writing the BLOB to an upload location
   */
  public RegistryEndpointProvider<Uri> writer(Uri location, Consumer<long> writtenByteCountListener) {
    return new Writer(location, writtenByteCountListener, this);
  }

  /**
   * @param location the upload Uri
   * @return a {@link RegistryEndpointProvider} for committing the written BLOB with its digest
   */
  public RegistryEndpointProvider<object> committer(Uri location) {
    return new Committer(location, this);
  }

  private RegistryErrorException buildRegistryErrorException(string reason) {
    RegistryErrorExceptionBuilder registryErrorExceptionBuilder =
        new RegistryErrorExceptionBuilder(getActionDescription());
    registryErrorExceptionBuilder.addReason(reason);
    return registryErrorExceptionBuilder.build();
  }

  /**
   * @return the common action description for {@link Initializer}, {@link Writer}, and {@link
   *     Committer}
   */
  private string getActionDescription() {
    return "push BLOB for "
        + registryEndpointRequestProperties.getServerUrl()
        + "/"
        + registryEndpointRequestProperties.getImageName()
        + " with digest "
        + blobDigest;
  }

  /**
   * Extract the {@code Location} header from the response to get the new location for the next
   * request.
   *
   * <p>The {@code Location} header can be relative or absolute.
   *
   * @see <a
   *     href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Location#Directives">https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Location#Directives</a>
   * @param response the response to extract the 'Location' header from
   * @return the new location for the next request
   * @throws RegistryErrorException if there was not a single 'Location' header
   */
  public static Uri getRedirectLocation(HttpResponseMessage response) {
            return response.getRequestUrl().MakeRelativeUri(response.Headers.Location);
  }
}
}

namespace Jib.Net.Core
{
    enum HttpURLConnection
    {
        HTTP_CREATED,
        HTTP_ACCEPTED
    }
}