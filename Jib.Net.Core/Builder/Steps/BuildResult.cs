/*
 * Copyright 2018 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.image;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using System;

namespace com.google.cloud.tools.jib.builder.steps
{
    /** Used to record the results of a build. */
    public class BuildResult : IBuildResult
    {
        /**
         * Gets a {@link BuildResult} from an {@link Image}.
         *
         * @param image the image
         * @param targetFormat the target format of the image
         * @return a new {@link BuildResult} with the image's digest and id
         * @throws IOException if writing the digest or container configuration fails
         */
        public static BuildResult fromImage(Image image, ManifestFormat targetFormat)
        {
            ImageToJsonTranslator imageToJsonTranslator = new ImageToJsonTranslator(image);
            BlobDescriptor containerConfigurationBlobDescriptor =
                Digests.computeDigest(imageToJsonTranslator.getContainerConfiguration());
            BuildableManifestTemplate manifestTemplate =
                imageToJsonTranslator.getManifestTemplate(
                    targetFormat, containerConfigurationBlobDescriptor);
            DescriptorDigest imageDigest = Digests.computeJsonDigest(manifestTemplate);
            DescriptorDigest imageId = containerConfigurationBlobDescriptor.getDigest();
            return new BuildResult(imageDigest, imageId);
        }

        private readonly DescriptorDigest imageDigest;
        private readonly DescriptorDigest imageId;

        public BuildResult(DescriptorDigest imageDigest, DescriptorDigest imageId)
        {
            this.imageDigest = imageDigest;
            this.imageId = imageId;
        }

        public DescriptorDigest getImageDigest()
        {
            return imageDigest;
        }

        public DescriptorDigest getImageId()
        {
            return imageId;
        }

        public override int GetHashCode()
        {
            return Objects.hash(imageDigest, imageId);
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (!(other is BuildResult))
            {
                return false;
            }
            BuildResult otherBuildResult = (BuildResult)other;
            return imageDigest.Equals(otherBuildResult.imageDigest)
                && imageId.Equals(otherBuildResult.imageId);
        }
    }
}
