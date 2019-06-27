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

using com.google.cloud.tools.jib.blob;
using Jib.Net.Core;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Images;
using System;

namespace Jib.Net.Core.Caching
{
    public class CachedLayerWithType : ICachedLayer
    {
        private readonly string layerType;

        public CachedLayerWithType(ICachedLayer cachedLayer, string layerType)
        {
            CachedLayer = cachedLayer;
            this.layerType = layerType;
        }

        public ICachedLayer CachedLayer { get; set; }

        public IBlob GetBlob()
        {
            return CachedLayer.GetBlob();
        }

        public BlobDescriptor GetBlobDescriptor()
        {
            return CachedLayer.GetBlobDescriptor();
        }

        public DescriptorDigest GetDiffId()
        {
            return CachedLayer.GetDiffId();
        }

        public DescriptorDigest GetDigest()
        {
            return CachedLayer.GetDigest();
        }

        public long GetSize()
        {
            return CachedLayer.GetSize();
        }

        public string GetLayerType()
        {
            return layerType;
        }
    }

    /** Default implementation of {@link CachedLayer}. */
    public sealed class CachedLayer : ILayer, ICachedLayer
    {
        /** Builds a {@link CachedLayer}. */
        public class Builder
        {
            private DescriptorDigest layerDigest;
            private DescriptorDigest layerDiffId;
            private long layerSize = -1;
            private IBlob layerBlob;

            public Builder() { }

            public Builder SetLayerDigest(DescriptorDigest layerDigest)
            {
                this.layerDigest = layerDigest;
                return this;
            }

            public Builder SetLayerDiffId(DescriptorDigest layerDiffId)
            {
                this.layerDiffId = layerDiffId;
                return this;
            }

            public Builder SetLayerSize(long layerSize)
            {
                this.layerSize = layerSize;
                return this;
            }

            public Builder SetLayerBlob(IBlob layerBlob)
            {
                this.layerBlob = layerBlob;
                return this;
            }

            public bool HasLayerBlob()
            {
                return layerBlob != null;
            }

            public CachedLayer Build()
            {
                return new CachedLayer(
                    Preconditions.CheckNotNull(layerDigest, "layerDigest required"),
                    Preconditions.CheckNotNull(layerDiffId, "layerDiffId required"),
                    layerSize,
                    Preconditions.CheckNotNull(layerBlob, "layerBlob required"));
            }
        }

        /**
         * Creates a new {@link Builder} for a {@link CachedLayer}.
         *
         * @return the new {@link Builder}
         */
        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        private readonly DescriptorDigest layerDiffId;
        private readonly BlobDescriptor blobDescriptor;
        private readonly IBlob layerBlob;

        private CachedLayer(
            DescriptorDigest layerDigest, DescriptorDigest layerDiffId, long layerSize, IBlob layerBlob)
        {
            this.layerDiffId = layerDiffId;
            this.layerBlob = layerBlob;
            blobDescriptor = new BlobDescriptor(layerSize, layerDigest);
        }

        public DescriptorDigest GetDigest()
        {
            return blobDescriptor.GetDigest();
        }

        public long GetSize()
        {
            return blobDescriptor.GetSize();
        }

        public DescriptorDigest GetDiffId()
        {
            return layerDiffId;
        }

        public IBlob GetBlob()
        {
            return layerBlob;
        }

        public BlobDescriptor GetBlobDescriptor()
        {
            return blobDescriptor;
        }

        public string GetLayerType()
        {
            throw new NotSupportedException();
        }
    }
}
