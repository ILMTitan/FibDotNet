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

using Newtonsoft.Json;
using NodaTime;

namespace Fib.Net.Core.Images.Json
{
    /**
     * Represents an item in the container configuration's {@code history} list.
     *
     * @see <a href=https://github.com/opencontainers/image-spec/blob/master/config.md#properties>OCI
     *     image spec ({@code history} field)</a>
     */
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class HistoryEntry
    {
        public class Builder
        {
            private Instant? creationTimestamp;
            private string author;
            private string createdBy;
            private string comment;
            private bool emptyLayer;

            public Builder SetCreationTimestamp(Instant creationTimestamp)
            {
                this.creationTimestamp = creationTimestamp;
                return this;
            }

            public Builder SetAuthor(string author)
            {
                this.author = author;
                return this;
            }

            public Builder SetCreatedBy(string createdBy)
            {
                this.createdBy = createdBy;
                return this;
            }

            public Builder SetComment(string comment)
            {
                this.comment = comment;
                return this;
            }

            public Builder SetEmptyLayer(bool emptyLayer)
            {
                this.emptyLayer = emptyLayer;
                return this;
            }

            public HistoryEntry Build()
            {
                return new HistoryEntry(
                    creationTimestamp?.ToString(),
                    author,
                    createdBy,
                    comment,
                    emptyLayer);
            }

            public Builder() { }
        }

        /**
         * Creates a builder for a {@link HistoryEntry}.
         *
         * @return the builder
         */
        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        /** The ISO-8601 formatted timestamp at which the image was created. */
        [JsonProperty("created")]
        public string CreationTimestamp { get; set; }

        /** The name of the author specified when committing the image. */
        [JsonProperty("author")]
        public string Author { get; set; }

        /** The command used to build the layer. */
        [JsonProperty("created_by")]
        public string CreatedBy { get; set; }

        /** A custom message set when creating the layer. */
        [JsonProperty("comment")]
        public string Comment { get; set; }

        /**
         * Whether or not the entry corresponds to a layer in the container ({@code bool} to
         * make field optional).
         */
        [JsonProperty("empty_layer", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool EmptyLayer { get; set; }

        public HistoryEntry() { }

        private HistoryEntry(
            string creationTimestamp,
            string author,
            string createdBy,
            string comment,
            bool emptyLayer)
        {
            Author = author;
            CreationTimestamp = creationTimestamp;
            CreatedBy = createdBy;
            Comment = comment;
            EmptyLayer = emptyLayer;
        }

        /**
         * Returns whether or not the history object corresponds to a layer in the container.
         *
         * @return {@code true} if the history object corresponds to a layer in the container
         */
        public bool HasCorrespondingLayer()
        {
            return EmptyLayer;
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (other is HistoryEntry historyEntry)
            {
                HistoryEntry otherHistory = historyEntry;
                return Equals(otherHistory.CreationTimestamp, CreationTimestamp)
                    && Equals(otherHistory.Author, Author)
                    && Equals(otherHistory.CreatedBy, CreatedBy)
                    && Equals(otherHistory.Comment, Comment)
                    && Equals(otherHistory.EmptyLayer, EmptyLayer);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Objects.Hash(Author, CreationTimestamp, CreatedBy, Comment, EmptyLayer);
        }

        public override string ToString()
        {
            return CreatedBy ?? "";
        }
    }
}
