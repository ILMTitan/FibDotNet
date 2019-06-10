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

namespace com.google.cloud.tools.jib.image {




/**
 * A {@link Layer} reference that <b>does not</b> have the underlying content. It references the
 * layer with its digest, size, and diff ID.
 */
public class ReferenceLayer : Layer {

  /** The {@link BlobDescriptor} of the compressed layer content. */
  private readonly BlobDescriptor blobDescriptor;

  /** The digest of the uncompressed layer content. */
  private readonly DescriptorDigest diffId;

  /**
   * Instantiate with a {@link BlobDescriptor} and diff ID.
   *
   * @param blobDescriptor the blob descriptor
   * @param diffId the diff ID
   */
  public ReferenceLayer(BlobDescriptor blobDescriptor, DescriptorDigest diffId) {
    this.blobDescriptor = blobDescriptor;
    this.diffId = diffId;
  }

  public Blob getBlob() {
    throw new LayerPropertyNotFoundException("Blob not available for reference layer");
  }

  public BlobDescriptor getBlobDescriptor() {
    return blobDescriptor;
  }

  public DescriptorDigest getDiffId() {
    return diffId;
  }
}
}
