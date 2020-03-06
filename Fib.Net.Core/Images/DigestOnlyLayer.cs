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

namespace Fib.Net.Core.Images
{
    /** A {@link Layer} reference that only has its {@link DescriptorDigest}. */
    public class DigestOnlyLayer : ILayer
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

        public IBlob GetBlob()
        {
            throw new LayerPropertyNotFoundException(Resources.DigestOnlyLayerGetBlobExceptionMessage);
        }

        public BlobDescriptor GetBlobDescriptor()
        {
            return blobDescriptor;
        }

        public DescriptorDigest GetDiffId()
        {
            throw new LayerPropertyNotFoundException(Resources.DigestOnlyLayerGetDiffIdExceptionMessage);
        }
    }
}
