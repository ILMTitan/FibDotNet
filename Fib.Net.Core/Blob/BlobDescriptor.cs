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

namespace Fib.Net.Core.Blob
{
    /** Contains properties describing a BLOB, including its digest and possibly its size (in bytes). */
    public class BlobDescriptor
    {
        private readonly DescriptorDigest digest;

        /** The size of the BLOB (in bytes). Negative if unknown. */
        private readonly long size;

        public BlobDescriptor(long size, DescriptorDigest digest)
        {
            this.size = size;
            this.digest = digest;
        }

        /**
         * Initialize with just digest.
         *
         * @param digest the digest to initialize the {@link BlobDescriptor} from
         */
        public BlobDescriptor(DescriptorDigest digest) : this(-1, digest)
        {
        }

        public bool HasSize()
        {
            return size >= 0;
        }

        public DescriptorDigest GetDigest()
        {
            return digest;
        }

        public long GetSize()
        {
            return size;
        }

        /**
         * Two {@link BlobDescriptor} objects are equal if their
         *
         * <ol>
         *   <li>{@code digest}s are not null and equal, and
         *   <li>{@code size}s are non-negative and equal
         * </ol>
         */

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            if (size < 0 || !(obj is BlobDescriptor other))
            {
                return false;
            }

            return size == other.GetSize() && digest.Equals(other.GetDigest());
        }

        public override int GetHashCode()
        {
            int result = digest.GetHashCode();
            return (31 * result) + (int)(size ^ size >> 32);
        }

        public override string ToString()
        {
            return "digest: " + digest + ", size: " + size;
        }
    }
}
