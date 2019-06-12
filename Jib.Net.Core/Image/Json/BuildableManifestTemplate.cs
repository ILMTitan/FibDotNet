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

using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.image.json
{
    /**
     * Parent class for image manifest JSON templates that can be built.
     *
     * @see V22ManifestTemplate Docker V2.2 format
     * @see OCIManifestTemplate OCI format
     */
    public interface BuildableManifestTemplate : ManifestTemplate
    {
        /** @return the media type for this manifest, specific to the image format */
        string getManifestMediaType();

        /** @return the content descriptor of the container configuration */
        ContentDescriptorTemplate getContainerConfiguration();

        /** @return an unmodifiable view of the layers */
        IReadOnlyList<ContentDescriptorTemplate> getLayers();

        /**
         * Sets the content descriptor of the container configuration.
         *
         * @param size the size of the container configuration.
         * @param digest the container configuration content descriptor digest.
         */
        void setContainerConfiguration(long size, DescriptorDigest digest);

        /**
         * Adds a layer to the manifest.
         *
         * @param size the size of the layer.
         * @param digest the layer descriptor digest.
         */
        void addLayer(long size, DescriptorDigest digest);
    }
}
