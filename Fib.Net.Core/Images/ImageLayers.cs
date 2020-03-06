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

using Iesi.Collections.Generic;
using Fib.Net.Core.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Fib.Net.Core.Images
{
    /** Holds the layers for an image. */
    public sealed class ImageLayers : IEnumerable<ILayer>
    {
        public class Builder
        {
            private readonly IList<ILayer> layers = new List<ILayer>();

            private readonly ImmutableHashSet<DescriptorDigest>.Builder layerDigestsBuilder =
                ImmutableHashSet.CreateBuilder<DescriptorDigest>();

            private bool _removeDuplicates = false;

            /**
             * Adds a layer. Removes any prior occurrences of the same layer.
             *
             * <p>Note that only subclasses of {@link Layer} that implement {@code equals/hashCode} will be
             * guaranteed to not be duplicated.
             *
             * @param layer the layer to add
             * @return this
             * @throws LayerPropertyNotFoundException if adding the layer fails
             */
            public Builder Add(ILayer layer)
            {
                layer = layer ?? throw new ArgumentNullException(nameof(layer));
                layerDigestsBuilder.Add(layer.GetBlobDescriptor().GetDigest());
                layers.Add(layer);
                return this;
            }

            /**
             * Adds all layers in {@code layers}.
             *
             * @param layers the layers to add
             * @return this
             * @throws LayerPropertyNotFoundException if adding a layer fails
             */
            public Builder AddAll(ImageLayers layers)
            {
                layers = layers ?? throw new ArgumentNullException(nameof(layers));
                foreach (ILayer layer in layers)
                {
                    Add(layer);
                }
                return this;
            }

            /**
             * Remove any duplicate layers, keeping the last occurrence of the layer.
             *
             * @return this
             */
            public Builder RemoveDuplicates()
            {
                _removeDuplicates = true;
                return this;
            }

            public ImageLayers Build()
            {
                if (!_removeDuplicates)
                {
                    return new ImageLayers(ImmutableArray.CreateRange(layers), layerDigestsBuilder.ToImmutable());
                }

                // LinkedHashSet maintains the order but keeps the first occurrence. Keep last occurrence by
                // adding elements in reverse, and then reversing the result
                ISet<ILayer> dedupedButReversed = new LinkedHashSet<ILayer>(layers.Reverse());
                ImmutableArray<ILayer> deduped = ImmutableArray.CreateRange(dedupedButReversed.Reverse());
                return new ImageLayers(deduped, layerDigestsBuilder.ToImmutable());
            }
        }

        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        /** The layers of the image, in the order in which they are applied. */
        private readonly ImmutableArray<ILayer> layers;

        /** Keeps track of the layers already added. */
        private readonly ImmutableHashSet<DescriptorDigest> layerDigests;

        private ImageLayers(ImmutableArray<ILayer> layers, ImmutableHashSet<DescriptorDigest> layerDigests)
        {
            this.layers = layers;
            this.layerDigests = layerDigests;
        }

        /** @return a read-only view of the image layers. */
        public ImmutableArray<ILayer> GetLayers()
        {
            return layers;
        }

        /** @return the layer count */
        public int Size()
        {
            return layers.Length;
        }

        public bool IsEmpty()
        {
            return layers.Length == 0;
        }

        /**
         * @param index the index of the layer to get
         * @return the layer at the specified index
         */
        public ILayer Get(int index)
        {
            return layers[index];
        }

        /**
         * @param digest the digest used to retrieve the layer
         * @return the layer found, or {@code null} if not found
         * @throws LayerPropertyNotFoundException if getting the layer's blob descriptor fails
         */
        public ILayer Get(DescriptorDigest digest)
        {
            if (!Has(digest))
            {
                return null;
            }
            foreach (ILayer layer in layers)
            {
                if (layer.GetBlobDescriptor().GetDigest().Equals(digest))
                {
                    return layer;
                }
            }
            throw new InvalidOperationException(Resources.ImageLayersMissingLayerExceptionMessage);
        }

        /**
         * @param digest the digest to check for
         * @return true if the layer with the specified digest exists; false otherwise
         */
        public bool Has(DescriptorDigest digest)
        {
            return layerDigests.Contains(digest);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(GetLayers().GetEnumerator());
        }

        IEnumerator<ILayer> IEnumerable<ILayer>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<ILayer>
        {
            private ImmutableArray<ILayer>.Enumerator inner;

            public Enumerator(ImmutableArray<ILayer>.Enumerator inner)
            {
                this.inner = inner;
            }

            public ILayer Current => inner.Current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return inner.MoveNext();
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}
