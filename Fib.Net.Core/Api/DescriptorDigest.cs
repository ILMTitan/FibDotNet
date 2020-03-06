// Copyright 2017 Google LLC.
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

using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Fib.Net.Core.Api
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
        public static DescriptorDigest FromHash(string hash)
        {
            var match = Regex.Match(hash, HASH_REGEX);
            if (!match.Success || match.Value != hash)
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
        public static DescriptorDigest FromDigest(string digest)
        {
            digest = digest ?? throw new ArgumentNullException(nameof(digest));
            if (!IsValidDigest(digest))
            {
                throw new DigestException("Invalid digest: " + digest);
            }

            // Extracts the hash portion of the digest.
            string hash = digest.Substring(DIGEST_PREFIX.Length);
            return new DescriptorDigest(hash);
        }

        public static bool IsValidDigest(string digest)
        {
            var match = Regex.Match(digest, DigestRegex);
            return match.Success && match.Value == digest;
        }

        private DescriptorDigest(string hash)
        {
            this.hash = hash;
        }

        public string GetHash()
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
#if NETCOREAPP2_0
            return hash.GetHashCode(StringComparison.Ordinal);
#else
            return hash.GetHashCode();
#endif
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
