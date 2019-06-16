﻿/*
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

using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.image.json
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
            this.MediaType = mediaType;
            this.Size = size;
            this.Digest = digest;
        }

        /** Necessary for Jackson to create from JSON. */
        private ContentDescriptorTemplate() { }

        public long getSize()
        {
            return Size;
        }

        private void setSize(long size)
        {
            this.Size = size;
        }

        public DescriptorDigest getDigest()
        {
            return Digest;
        }

        private void setDigest(DescriptorDigest digest)
        {
            this.Digest = digest;
        }

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
    }
}
