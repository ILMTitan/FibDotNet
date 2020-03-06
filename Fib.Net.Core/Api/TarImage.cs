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

using Fib.Net.Core.FileSystem;

namespace Fib.Net.Core.Api
{
    /**
     * Builds to a tarball archive.
     *
     * <p>Usage example:
     *
     * <pre>{@code
     * TarImage tarImage = TarImage.named("myimage")
     *                             .saveTo(Paths.get("image.tar"));
     * }</pre>
     */
    public sealed class TarImage
    {
        /** Finishes constructing a {@link TarImage}. */
        public class Builder
        {
            private readonly ImageReference imageReference;

            public Builder(ImageReference imageReference)
            {
                this.imageReference = imageReference;
            }

            /**
             * Sets the output file to save the tarball archive to.
             *
             * @param outputFile the output file
             * @return a new {@link TarImage}
             */
            public TarImage SaveTo(SystemPath outputFile)
            {
                return new TarImage(imageReference, outputFile);
            }

            /**
             * Sets the output file to save the tarball archive to.
             *
             * @param outputFile the output file
             * @return a new {@link TarImage}
             */
            public TarImage SaveTo(string outputFilePath)
            {
                return SaveTo(new SystemPath(outputFilePath));
            }
        }

        /**
         * Configures the output tarball archive with an image reference. This image reference will be the
         * name of the image if loaded into the Docker daemon.
         *
         * @param imageReference the image reference
         * @return a {@link Builder} to finish constructing a new {@link TarImage}
         */
        public static Builder Named(ImageReference imageReference)
        {
            return new Builder(imageReference);
        }

        /**
         * Configures the output tarball archive with an image reference to set as its tag.
         *
         * @param imageReference the image reference
         * @return a {@link Builder} to finish constructing a new {@link TarImage}
         * @throws InvalidImageReferenceException if {@code imageReference} is not a valid image reference
         */
        public static Builder Named(string imageReference)
        {
            return Named(ImageReference.Parse(imageReference));
        }

        private readonly ImageReference imageReference;
        private readonly SystemPath outputFile;

        /** Instantiate with {@link #named}. */
        private TarImage(ImageReference imageReference, SystemPath outputFile)
        {
            this.imageReference = imageReference;
            this.outputFile = outputFile;
        }

        /**
         * Gets the output file to save the tarball archive to.
         *
         * @return the output file
         */
        public SystemPath GetOutputFile()
        {
            return outputFile;
        }

        public ImageReference GetImageReference()
        {
            return imageReference;
        }
    }
}
