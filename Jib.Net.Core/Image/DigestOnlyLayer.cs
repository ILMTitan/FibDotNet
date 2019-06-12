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

using com.google.cloud.tools.jib.blob;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;

namespace com.google.cloud.tools.jib.image
{
    /** A {@link Layer} reference that only has its {@link DescriptorDigest}. */
    public class DigestOnlyLayer : Layer
    {
        /** The {@link BlobDescriptor} of the compressed layer content. */
        private readonly BlobDescriptor blobDescriptor;

        /**
         * Instantiate with a {@link DescriptorDigest}.
         *
         * @param digest the digest to instantiate the {@link DigestOnlyLayer} from
         */
        public DigestOnlyLayer(DescriptorDigest digest)
        {
            blobDescriptor = new BlobDescriptor(digest);
        }

        public Blob getBlob()
        {
            throw new LayerPropertyNotFoundException("Blob not available for digest-only layer");
        }

        public BlobDescriptor getBlobDescriptor()
        {
            return blobDescriptor;
        }

        public DescriptorDigest getDiffId()
        {
            throw new LayerPropertyNotFoundException("Diff ID not available for digest-only layer");
        }
    }
}
