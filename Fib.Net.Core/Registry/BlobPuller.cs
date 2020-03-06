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
using Fib.Net.Core.Hash;
using Fib.Net.Core.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fib.Net.Core.Registry
{
    /** Pulls an image's BLOB (layer or container configuration). */
    internal class BlobPuller : RegistryEndpointProvider<object>
    {
        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;

        /** The digest of the BLOB to pull. */
        private readonly DescriptorDigest blobDigest;

        /**
         * The {@link OutputStream} to write the BLOB to. Closes the {@link OutputStream} after writing.
         */
        private readonly Stream destinationOutputStream;

        private readonly Action<long> blobSizeListener;
        private readonly Action<long> writtenByteCountListener;

        public BlobPuller(
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            DescriptorDigest blobDigest,
            Stream destinationOutputStream,
            Action<long> blobSizeListener,
            Action<long> writtenByteCountListener)
        {
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.blobDigest = blobDigest;
            this.destinationOutputStream = destinationOutputStream;
            this.blobSizeListener = blobSizeListener;
            this.writtenByteCountListener = writtenByteCountListener;
        }

        public async Task<object> HandleResponseAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpResponseException(response);
            }
            blobSizeListener(response.Content.Headers.ContentLength ?? 0);

            using (Stream outputStream =
                new NotifyingOutputStream(destinationOutputStream, writtenByteCountListener, true))
            {
                BlobDescriptor receivedBlobDescriptor;
                using (Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    receivedBlobDescriptor = await Digests.ComputeDigestAsync(contentStream, outputStream).ConfigureAwait(false);
                }

                if (!blobDigest.Equals(receivedBlobDescriptor.GetDigest()))
                {
                    throw new UnexpectedBlobDigestException(
                        "The pulled BLOB has digest '"
                            + receivedBlobDescriptor.GetDigest()
                            + "', but the request digest was '"
                            + blobDigest
                            + "'");
                }
            }

            return null;
        }

        public BlobHttpContent GetContent()
        {
            return null;
        }

        public IList<string> GetAccept()
        {
            return new List<string>();
        }

        public Uri GetApiRoute(string apiRouteBase)
        {
            return new Uri(
                apiRouteBase + registryEndpointRequestProperties.GetImageName() + "/blobs/" + blobDigest);
        }

        public HttpMethod GetHttpMethod()
        {
            return HttpMethod.Get;
        }

        public string GetActionDescription()
        {
            return "pull BLOB for "
                + registryEndpointRequestProperties.GetRegistry()
                + "/"
                + registryEndpointRequestProperties.GetImageName()
                + " with digest "
                + blobDigest;
        }
    }
}
