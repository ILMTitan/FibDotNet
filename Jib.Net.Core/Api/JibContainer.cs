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

using Jib.Net.Core.Global;

namespace Jib.Net.Core.Api
{
    /** The container built by Jib. */
    public class JibContainer
    {
        private readonly DescriptorDigest imageDigest;
        private readonly DescriptorDigest imageId;

        public JibContainer(DescriptorDigest imageDigest, DescriptorDigest imageId)
        {
            this.imageDigest = imageDigest;
            this.imageId = imageId;
        }

        /**
         * Gets the digest of the registry image manifest built by Jib. This digest can be used to fetch a
         * specific image from the registry in the form {@code myregistry/myimage@digest}.
         *
         * @return the image digest
         */
        public DescriptorDigest GetDigest()
        {
            return imageDigest;
        }

        /**
         * Gets the digest of the container configuration built by Jib.
         *
         * @return the image ID
         */
        public DescriptorDigest GetImageId()
        {
            return imageId;
        }

        public override int GetHashCode()
        {
            return Objects.Hash(imageDigest, imageId);
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (!(other is JibContainer otherContainer))
            {
                return false;
            }
            return imageDigest.Equals(otherContainer.imageDigest) && imageId.Equals(otherContainer.imageId);
        }
    }
}
