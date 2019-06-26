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

using Jib.Net.Core.Api;

namespace com.google.cloud.tools.jib.api
{
    // TODO: Move to com.google.cloud.tools.jib once that package is cleaned up.

    /** Build containers with Jib. */
    // TODO: Add tests once JibContainerBuilder#containerize() is added.
    public sealed class Jib
    {
        /**
         * Starts building the container from a base image. The base image should be publicly-available.
         * For a base image that requires credentials, use {@link #from(RegistryImage)}.
         *
         * @param baseImageReference the base image reference
         * @return a new {@link JibContainerBuilder} to continue building the container
         * @throws InvalidImageReferenceException if the {@code baseImageReference} is not a valid image
         *     reference
         */
        public static JibContainerBuilder From(string baseImageReference)
        {
            return From(RegistryImage.Named(baseImageReference));
        }

        /**
         * Starts building the container from a base image. The base image should be publicly-available.
         * For a base image that requires credentials, use {@link #from(RegistryImage)}.
         *
         * @param baseImageReference the base image reference
         * @return a new {@link JibContainerBuilder} to continue building the container
         */
        public static JibContainerBuilder From(ImageReference baseImageReference)
        {
            return From(RegistryImage.Named(baseImageReference));
        }

        /**
         * Starts building the container from a base image.
         *
         * @param registryImage the {@link RegistryImage} that defines base container registry and
         *     credentials
         * @return a new {@link JibContainerBuilder} to continue building the container
         */
        public static JibContainerBuilder From(RegistryImage registryImage)
        {
            return new JibContainerBuilder(registryImage);
        }

        /**
         * Starts building the container from an empty base image.
         *
         * @return a new {@link JibContainerBuilder} to continue building the container
         */
        public static JibContainerBuilder FromScratch()
        {
            return From(ImageReference.Scratch());
        }

        private Jib() { }
    }
}
