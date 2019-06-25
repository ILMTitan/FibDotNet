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

using System;
using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Global;
using Newtonsoft.Json;

namespace Jib.Net.Core.Api
{
    /**
     * Represents a SHA-256 content descriptor digest as defined by the Registry HTTP API v2 reference.
     *
     * @see <a href="https://docs.docker.com/registry/spec/api/#content-digests">https://docs.docker.com/registry/spec/api/#content-digests</a>
     * @see <a href="https://github.com/opencontainers/image-spec/blob/master/descriptor.md#digests">OCI
     *     Content Descriptor Digest</a>
     */
     [JsonConverter(typeof(DescriptorDigestConverter))]
    public sealed class DescriptorDigest
    {
        public static readonly int HashLength = 64;

        /** Pattern matches a SHA-256 hash - 32 bytes in lowercase hexadecimal. */
        private static readonly string HASH_REGEX = $"[a-f0-9]{{{HashLength}}}";

        /** The algorithm prefix for the digest string. */
        private const string DIGEST_PREFIX = "sha256:";

        /** Pattern matches a SHA-256 digest - a SHA-256 hash prefixed with "sha256:". */
        public static readonly string DigestRegex = DIGEST_PREFIX + HASH_REGEX;

        private readonly string hash;

        /**
         * Creates a new instance from a valid hash string.
         *
         * @param hash the hash to generate the {@link DescriptorDigest} from
         * @return a new {@link DescriptorDigest} created from the hash
         * @throws DigestException if the hash is invalid
         */
        public static DescriptorDigest fromHash(string hash)
        {
            if (!hash.matches(HASH_REGEX))
            {
                throw new DigestException("Invalid hash: " + hash);
            }

            return new DescriptorDigest(hash);
        }

        /**
         * Creates a new instance from a valid digest string.
         *
         * @param digest the digest to generate the {@link DescriptorDigest} from
         * @return a new {@link DescriptorDigest} created from the digest
         * @throws DigestException if the digest is invalid
         */
        public static DescriptorDigest fromDigest(string digest)
        {
            digest = digest ?? throw new ArgumentNullException(nameof(digest));
            if (!digest.matches(DigestRegex))
            {
                throw new DigestException("Invalid digest: " + digest);
            }

            // Extracts the hash portion of the digest.
            string hash = digest.substring(DIGEST_PREFIX.length());
            return new DescriptorDigest(hash);
        }

        private DescriptorDigest(string hash)
        {
            this.hash = hash;
        }

        public string getHash()
        {
            return hash;
        }

        public override string ToString()
        {
            return "sha256:" + hash;
        }

        /** Pass-through hash code of the digest string. */

        public override int GetHashCode()
        {
            return hash.hashCode();
        }

        /** Two digest objects are equal if their digest strings are equal. */

        public override bool Equals(object obj)
        {
            if (obj is DescriptorDigest descriptorDigest)
            {
                return hash == descriptorDigest.hash;
            }

            return false;
        }
    }
}
