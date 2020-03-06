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
    /**
     * Represents a layer in an image. Implementations represent the various types of layers.
     *
     * <p>An image layer consists of:
     *
     * <ul>
     *   <li>Content BLOB
     *   <li>
     *       <ul>
     *         <li>The compressed archive (tarball gzip) of the partial filesystem changeset.
     *       </ul>
     *   <li>Content Digest
     *   <li>
     *       <ul>
     *         <li>The SHA-256 hash of the content BLOB.
     *       </ul>
     *   <li>Content Size
     *   <li>
     *       <ul>
     *         <li>The size (in bytes) of the content BLOB.
     *       </ul>
     *   <li>Diff ID
     *   <li>
     *       <ul>
     *         <li>The SHA-256 hash of the uncompressed archive (tarball) of the partial filesystem
     *             changeset.
     *       </ul>
     * </ul>
     */
    public interface ILayer
    {
        /**
         * @return the layer's content BLOB
         * @throws LayerPropertyNotFoundException if not available
         */
        IBlob GetBlob();
        // TODO: Remove this
        /**
         * @return the layer's content {@link BlobDescriptor}
         * @throws LayerPropertyNotFoundException if not available
         */
        BlobDescriptor GetBlobDescriptor();

        /**
         * @return the layer's diff ID
         * @throws LayerPropertyNotFoundException if not available
         */
        DescriptorDigest GetDiffId();
    }
}
