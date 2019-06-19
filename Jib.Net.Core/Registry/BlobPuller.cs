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
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.http;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
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

        private readonly Consumer<long> blobSizeListener;
        private readonly Consumer<long> writtenByteCountListener;

        public BlobPuller(
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            DescriptorDigest blobDigest,
            Stream destinationOutputStream,
            Consumer<long> blobSizeListener,
            Consumer<long> writtenByteCountListener)
        {
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.blobDigest = blobDigest;
            this.destinationOutputStream = destinationOutputStream;
            this.blobSizeListener = blobSizeListener;
            this.writtenByteCountListener = writtenByteCountListener;
        }

        public async Task<object> handleResponseAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpResponseException(response);
            }
            blobSizeListener.accept(response.getContentLength() ?? 0);

            using (Stream outputStream =
                new NotifyingOutputStream(destinationOutputStream, writtenByteCountListener, true))
            {
                BlobDescriptor receivedBlobDescriptor =
                    await Digests.computeDigestAsync(await response.getBodyAsync(), outputStream);

                if (!blobDigest.Equals(receivedBlobDescriptor.getDigest()))
                {
                    throw new UnexpectedBlobDigestException(
                        "The pulled BLOB has digest '"
                            + receivedBlobDescriptor.getDigest()
                            + "', but the request digest was '"
                            + blobDigest
                            + "'");
                }
            }

            return null;
        }

        public BlobHttpContent getContent()
        {
            return null;
        }

        public IList<string> getAccept()
        {
            return Collections.emptyList<string>();
        }

        public Uri getApiRoute(string apiRouteBase)
        {
            return new Uri(
                apiRouteBase + registryEndpointRequestProperties.getImageName() + "/blobs/" + blobDigest);
        }

        public HttpMethod getHttpMethod()
        {
            return HttpMethod.Get;
        }

        public string getActionDescription()
        {
            return "pull BLOB for "
                + registryEndpointRequestProperties.getServerUrl()
                + "/"
                + registryEndpointRequestProperties.getImageName()
                + " with digest "
                + blobDigest;
        }
    }
}
