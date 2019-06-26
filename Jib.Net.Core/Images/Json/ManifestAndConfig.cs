/*
 * Copyright 2019 Google LLC.
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

using com.google.cloud.tools.jib.api;

namespace com.google.cloud.tools.jib.image.json
{
    /** Stores a manifest and container config. */
    public class ManifestAndConfig
    {
        private readonly IManifestTemplate manifest;
        private readonly ContainerConfigurationTemplate config;

        public ManifestAndConfig(
            IManifestTemplate manifest, ContainerConfigurationTemplate config)
        {
            this.manifest = manifest;
            this.config = config;
        }

        /**
         * Gets the manifest.
         *
         * @return the manifest
         */
        public IManifestTemplate getManifest()
        {
            return manifest;
        }

        /**
         * Gets the container configuration.
         *
         * @return the container configuration
         */
        public Option<ContainerConfigurationTemplate> getConfig()
        {
            return Option.OfNullable(config);
        }
    }
}
