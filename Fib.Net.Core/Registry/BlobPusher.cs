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
using Fib.Net.Core.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace Fib.Net.Core.Registry
{
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
    internal class BlobPusher
    {
        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
        private readonly DescriptorDigest blobDigest;
        private readonly IBlob blob;
        private readonly string sourceRepository;

        /** Initializes the BLOB upload. */
        private class Initializer : RegistryEndpointProvider<Uri>
        {
            private readonly BlobPusher parent;

            public Initializer(BlobPusher parent)
            {
                this.parent = parent;
            }

            public BlobHttpContent GetContent()
            {
                return null;
            }

            public IList<string> GetAccept()
            {
                return new List<string>();
            }

            /**
             * @return a Uri to continue pushing the BLOB to, or {@code null} if the BLOB already exists on
             *     the registry
             */
            public Task<Uri> HandleResponseAsync(HttpResponseMessage response)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        // The BLOB exists in the registry.
                        return Task.FromResult(default(Uri));

                    case HttpStatusCode.Accepted:
                        return Task.FromResult(GetRedirectLocation(response));

                    default:
                        throw parent.BuildRegistryErrorException(
                            "Received unrecognized status code " + response.StatusCode);
                }
            }

            public Uri GetApiRoute(string apiRouteBase)
            {
                StringBuilder url = new StringBuilder(apiRouteBase)
                    .Append(parent.registryEndpointRequestProperties.GetImageName())
                    .Append("/blobs/uploads/");
                if (parent.sourceRepository != null)
                {
                    url.Append("?mount=").Append(parent.blobDigest).Append("&from=").Append(parent.sourceRepository);
                }

                return new Uri(url.ToString());
            }

            public HttpMethod GetHttpMethod()
            {
                return HttpMethod.Post;
            }

            public string GetActionDescription()
            {
                return parent.GetActionDescription();
            }
        }

        /** Writes the BLOB content to the upload location. */
        private class Writer : RegistryEndpointProvider<Uri>
        {
            private readonly BlobPusher parent;
            private readonly Uri location;
            private readonly Action<long> writtenByteCountListener;

            public BlobHttpContent GetContent()
            {
                return new BlobHttpContent(parent.blob, MediaTypeNames.Application.Octet, writtenByteCountListener);
            }

            public IList<string> GetAccept()
            {
                return new List<string>();
            }

            /** @return a Uri to continue pushing the BLOB to */

            public Task<Uri> HandleResponseAsync(HttpResponseMessage response)
            {
                // TODO: Handle 204 No Content
                return Task.FromResult(GetRedirectLocation(response));
            }

            public Uri GetApiRoute(string apiRouteBase)
            {
                return location;
            }

            public HttpMethod GetHttpMethod()
            {
                return new HttpMethod("PATCH");
            }

            public string GetActionDescription()
            {
                return parent.GetActionDescription();
            }

            public Writer(Uri location, Action<long> writtenByteCountListener, BlobPusher parent)
            {
                this.location = location;
                this.writtenByteCountListener = writtenByteCountListener;
                this.parent = parent;
            }
        }

        /** Commits the written BLOB. */
        private class Committer : RegistryEndpointProvider<object>
        {
            private readonly Uri location;
            private readonly BlobPusher parent;

            public BlobHttpContent GetContent()
            {
                return null;
            }

            public IList<string> GetAccept()
            {
                return new List<string>();
            }

            public Task<object> HandleResponseAsync(HttpResponseMessage response)
            {
                return Task.FromResult(default(object));
            }

            /** @return {@code location} with query parameter 'digest' set to the BLOB's digest */

            public Uri GetApiRoute(string apiRouteBase)
            {
                UriBuilder builder = new UriBuilder(location);
                if (string.IsNullOrEmpty(builder.Query))
                {
                    builder.Query = "?digest=" + parent.blobDigest;
                }
                else
                {
                    builder.Query += "&digest=" + parent.blobDigest;
                }
                return builder.Uri;
            }

            public HttpMethod GetHttpMethod()
            {
                return HttpMethod.Put;
            }

            public string GetActionDescription()
            {
                return parent.GetActionDescription();
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
      IBlob blob,
      string sourceRepository)
        {
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.blobDigest = blobDigest;
            this.blob = blob;
            this.sourceRepository = sourceRepository;
        }

        /**
         * @return a {@link RegistryEndpointProvider} for initializing the BLOB upload with an existence
         *     check
         */
        public RegistryEndpointProvider<Uri> CreateInitializer()
        {
            return new Initializer(this);
        }

        /**
         * @param location the upload Uri
         * @param blobProgressListener the listener for {@link Blob} push progress
         * @return a {@link RegistryEndpointProvider} for writing the BLOB to an upload location
         */
        public RegistryEndpointProvider<Uri> CreateWriter(Uri location, Action<long> writtenByteCountListener)
        {
            return new Writer(location, writtenByteCountListener, this);
        }

        /**
         * @param location the upload Uri
         * @return a {@link RegistryEndpointProvider} for committing the written BLOB with its digest
         */
        public RegistryEndpointProvider<object> CreateCommitter(Uri location)
        {
            return new Committer(location, this);
        }

        private RegistryErrorException BuildRegistryErrorException(string reason)
        {
            RegistryErrorExceptionBuilder registryErrorExceptionBuilder =
                new RegistryErrorExceptionBuilder(GetActionDescription());
            registryErrorExceptionBuilder.AddReason(reason);
            return registryErrorExceptionBuilder.Build();
        }

        /**
         * @return the common action description for {@link Initializer}, {@link Writer}, and {@link
         *     Committer}
         */
        private string GetActionDescription()
        {
            return "push BLOB for "
                + registryEndpointRequestProperties.GetRegistry()
                + "/"
                + registryEndpointRequestProperties.GetImageName()
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
        public static Uri GetRedirectLocation(HttpResponseMessage response)
        {
            if(response?.Headers?.Location == null)
            {
                throw new RegistryErrorException(Resources.BlobPusherRedirectLocationErrorMessage, response);
            }
            return new Uri(response.RequestMessage.RequestUri, response.Headers.Location);
        }
    }
}

namespace Fib.Net.Core
{
    internal enum HttpURLConnection
    {
        HTTP_CREATED,
        HTTP_ACCEPTED
    }
}