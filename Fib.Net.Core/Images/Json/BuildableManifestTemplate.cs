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
using Fib.Net.Core.Configuration;
using System.Collections.Generic;

namespace Fib.Net.Core.Images.Json
{
    /**
     * Parent class for image manifest JSON templates that can be built.
     *
     * @see V22ManifestTemplate Docker V2.2 format
     * @see OCIManifestTemplate OCI format
     */
    public interface IBuildableManifestTemplate : IManifestTemplate
    {
        /** @return the media type for this manifest, specific to the image format */
        string GetManifestMediaType();

        /** @return the content descriptor of the container configuration */
        ContentDescriptorTemplate GetContainerConfiguration();

        /** @return an unmodifiable view of the layers */
        IReadOnlyList<ContentDescriptorTemplate> Layers { get; }

        /**
         * Sets the content descriptor of the container configuration.
         *
         * @param size the size of the container configuration.
         * @param digest the container configuration content descriptor digest.
         */
        void SetContainerConfiguration(long size, DescriptorDigest digest);

        /**
         * Adds a layer to the manifest.
         *
         * @param size the size of the layer.
         * @param digest the layer descriptor digest.
         */
        void AddLayer(long size, DescriptorDigest digest);
        ManifestFormat GetFormat();
    }
}
