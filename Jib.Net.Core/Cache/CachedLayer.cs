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

namespace com.google.cloud.tools.jib.cache {







/** Default implementation of {@link CachedLayer}. */
public class CachedLayer : Layer {

  /** Builds a {@link CachedLayer}. */
  static class Builder {

    private DescriptorDigest layerDigest;
    private DescriptorDigest layerDiffId;
    private long layerSize = -1;
    private Blob layerBlob;

    private Builder() {}

    Builder setLayerDigest(DescriptorDigest layerDigest) {
      this.layerDigest = layerDigest;
      return this;
    }

    Builder setLayerDiffId(DescriptorDigest layerDiffId) {
      this.layerDiffId = layerDiffId;
      return this;
    }

    Builder setLayerSize(long layerSize) {
      this.layerSize = layerSize;
      return this;
    }

    Builder setLayerBlob(Blob layerBlob) {
      this.layerBlob = layerBlob;
      return this;
    }

    bool hasLayerBlob() {
      return layerBlob != null;
    }

    CachedLayer build() {
      return new CachedLayer(
          Preconditions.checkNotNull(layerDigest, "layerDigest required"),
          Preconditions.checkNotNull(layerDiffId, "layerDiffId required"),
          layerSize,
          Preconditions.checkNotNull(layerBlob, "layerBlob required"));
    }
  }

  /**
   * Creates a new {@link Builder} for a {@link CachedLayer}.
   *
   * @return the new {@link Builder}
   */
  static Builder builder() {
    return new Builder();
  }

  private readonly DescriptorDigest layerDiffId;
  private readonly BlobDescriptor blobDescriptor;
  private readonly Blob layerBlob;

  private CachedLayer(
      DescriptorDigest layerDigest, DescriptorDigest layerDiffId, long layerSize, Blob layerBlob) {
    this.layerDiffId = layerDiffId;
    this.layerBlob = layerBlob;
    this.blobDescriptor = new BlobDescriptor(layerSize, layerDigest);
  }

  public DescriptorDigest getDigest() {
    return blobDescriptor.getDigest();
  }

  public long getSize() {
    return blobDescriptor.getSize();
  }

  public DescriptorDigest getDiffId() {
    return layerDiffId;
  }

  public Blob getBlob() {
    return layerBlob;
  }

  public BlobDescriptor getBlobDescriptor() {
    return blobDescriptor;
  }
}
}
