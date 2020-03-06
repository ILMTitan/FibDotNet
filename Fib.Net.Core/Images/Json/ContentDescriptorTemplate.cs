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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Fib.Net.Core.Images.Json
{
    /**
     * Template for inner JSON object representing content descriptor for a layer or container
     * configuration.
     *
     * @see <a href="https://github.com/opencontainers/image-spec/blob/master/descriptor.md">OCI
     *     Content Descriptors</a>
     */
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ContentDescriptorTemplate : IEquatable<ContentDescriptorTemplate>
    {
        public string MediaType { get; set; }
        public DescriptorDigest Digest { get; set; }
        public long Size { get; set; }

        public ContentDescriptorTemplate(string mediaType, long size, DescriptorDigest digest)
        {
            MediaType = mediaType;
            Size = size;
            Digest = digest;
        }

        /** Necessary for Jackson to create from JSON. */
        private ContentDescriptorTemplate() { }

        public bool Equals(ContentDescriptorTemplate other)
        {
            if (this == other)
            {
                return true;
            }
            else if (other is null)
            {
                return false;
            }
            else
            {
                return Digest == other.Digest && MediaType == other.MediaType && Size == other.Size;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ContentDescriptorTemplate);
        }

        public override int GetHashCode()
        {
            var hashCode = 1122470636;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(MediaType);
            hashCode = (hashCode * -1521134295) + EqualityComparer<DescriptorDigest>.Default.GetHashCode(Digest);
            hashCode = (hashCode * -1521134295) + Size.GetHashCode();
            return hashCode;
        }
    }
}
