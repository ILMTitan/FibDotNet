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

using com.google.cloud.tools.jib.image.json;
using Iesi.Collections.Generic;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.image
{

    /** Holds the layers for an image. */
    public sealed class ImageLayers : IEnumerable<Layer>
    {
        public class Builder
        {
            private readonly IList<Layer> layers = new List<Layer>();

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
            public Builder add(Layer layer)
            {
                layerDigestsBuilder.add(layer.getBlobDescriptor().getDigest());
                layers.add(layer);
                return this;
            }

            /**
             * Adds all layers in {@code layers}.
             *
             * @param layers the layers to add
             * @return this
             * @throws LayerPropertyNotFoundException if adding a layer fails
             */
            public Builder addAll(ImageLayers layers)
            {
                foreach (Layer layer in layers)
                {
                    add(layer);
                }
                return this;
            }

            /**
             * Remove any duplicate layers, keeping the last occurrence of the layer.
             *
             * @return this
             */
            public Builder removeDuplicates()
            {
                _removeDuplicates = true;
                return this;
            }

            public ImageLayers build()
            {
                if (!_removeDuplicates)
                {
                    return new ImageLayers(ImmutableArray.CreateRange(layers), layerDigestsBuilder.build());
                }

                // LinkedHashSet maintains the order but keeps the first occurrence. Keep last occurrence by
                // adding elements in reverse, and then reversing the result
                ISet<Layer> dedupedButReversed = new LinkedHashSet<Layer>(layers.reverse());
                ImmutableArray<Layer> deduped = ImmutableArray.CreateRange(dedupedButReversed.reverse());
                return new ImageLayers(deduped, layerDigestsBuilder.build());
            }
        }

        public static Builder builder()
        {
            return new Builder();
        }

        /** The layers of the image, in the order in which they are applied. */
        private readonly ImmutableArray<Layer> layers;

        /** Keeps track of the layers already added. */
        private readonly ImmutableHashSet<DescriptorDigest> layerDigests;

        private ImageLayers(ImmutableArray<Layer> layers, ImmutableHashSet<DescriptorDigest> layerDigests)
        {
            this.layers = layers;
            this.layerDigests = layerDigests;
        }

        /** @return a read-only view of the image layers. */
        public ImmutableArray<Layer> getLayers()
        {
            return layers;
        }

        /** @return the layer count */
        public int size()
        {
            return layers.size();
        }

        public bool isEmpty()
        {
            return layers.isEmpty();
        }

        /**
         * @param index the index of the layer to get
         * @return the layer at the specified index
         */
        public Layer get(int index)
        {
            return layers.get(index);
        }

        /**
         * @param digest the digest used to retrieve the layer
         * @return the layer found, or {@code null} if not found
         * @throws LayerPropertyNotFoundException if getting the layer's blob descriptor fails
         */
        public Layer get(DescriptorDigest digest)
        {
            if (!has(digest))
            {
                return null;
            }
            foreach (Layer layer in layers)
            {
                if (layer.getBlobDescriptor().getDigest().Equals(digest))
                {
                    return layer;
                }
            }
            throw new InvalidOperationException("Layer digest exists but layer not found");
        }

        /**
         * @param digest the digest to check for
         * @return true if the layer with the specified digest exists; false otherwise
         */
        public bool has(DescriptorDigest digest)
        {
            return layerDigests.contains(digest);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(getLayers().GetEnumerator());
        }

        IEnumerator<Layer> IEnumerable<Layer>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<Layer>
        {
            private ImmutableArray<Layer>.Enumerator inner;

            public Enumerator(ImmutableArray<Layer>.Enumerator inner)
            {
                this.inner = inner;
            }

            public Layer Current => inner.Current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return inner.MoveNext();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
