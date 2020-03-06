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

namespace Fib.Net.Core.Registry
{
    /** Properties of registry endpoint requests. */
    public class RegistryEndpointRequestProperties
    {
        private readonly string registry;
        private readonly string imageName;

        /**
         * @param serverUrl the server Uri for the registry (for example, {@code gcr.io})
         * @param imageName the image/repository name (also known as, namespace)
         */
        public RegistryEndpointRequestProperties(string registry, string imageName)
        {
            this.registry = registry;
            this.imageName = imageName;
        }

        public string GetRegistry()
        {
            return registry;
        }

        public string GetImageName()
        {
            return imageName;
        }
    }
}
