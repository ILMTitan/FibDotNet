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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.cache
{
    /**
     * Generates a selector based on {@link LayerEntry}s for a layer. Selectors are secondary references
     * for a cache entries.
     *
     * <p>The selector is the SHA256 hash of the list of layer entries serialized in the following form:
     *
     * <pre>{@code
     * [
     *   {
     *     "sourceFile": "source/file/for/layer/entry/1",
     *     "extractionPath": "/extraction/path/for/layer/entry/1"
     *     "lastModifiedTime": "2018-10-03T15:48:32.416152Z"
     *     "permissions": "777"
     *   },
     *   {
     *     "sourceFile": "source/file/for/layer/entry/2",
     *     "extractionPath": "/extraction/path/for/layer/entry/2"
     *     "lastModifiedTime": "2018-10-03T15:48:32.416152Z"
     *     "permissions": "777"
     *   }
     * ]
     * }</pre>
     */
    internal static class LayerEntriesSelector
    {
        /** Serialized form of a {@link LayerEntry}. */
               [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public class LayerEntryTemplate : IComparable<LayerEntryTemplate>
        {
            public string SourceFile { get; }
            public string ExtractionPath { get; }

            [JsonConverter(typeof(InstantConverter))]
            public Instant LastModifiedTime { get; }

            public string Permissions { get; }

            public LayerEntryTemplate(LayerEntry layerEntry)
            {
                SourceFile = layerEntry.getSourceFile().ToAbsolutePath().toString();
                ExtractionPath = layerEntry.getExtractionPath().toString();
                LastModifiedTime = Files.getLastModifiedTime(layerEntry.getSourceFile());
                Permissions = layerEntry.getPermissions().toOctalString();
            }

            public int CompareTo(LayerEntryTemplate otherLayerEntryTemplate)
            {
                int sourceFileComparison = SourceFile.compareTo(otherLayerEntryTemplate.SourceFile);
                if (sourceFileComparison != 0)
                {
                    return sourceFileComparison;
                }
                int extractionPathComparison =
                    ExtractionPath.compareTo(otherLayerEntryTemplate.ExtractionPath);
                if (extractionPathComparison != 0)
                {
                    return extractionPathComparison;
                }
                int lastModifiedTimeComparison =
                    LastModifiedTime.compareTo(otherLayerEntryTemplate.LastModifiedTime);
                if (lastModifiedTimeComparison != 0)
                {
                    return lastModifiedTimeComparison;
                }
                return Permissions.compareTo(otherLayerEntryTemplate.Permissions);
            }

            public override bool Equals(object other)
            {
                if (this == other)
                {
                    return true;
                }
                if (!(other is LayerEntryTemplate))
                {
                    return false;
                }
                LayerEntryTemplate otherLayerEntryTemplate = (LayerEntryTemplate)other;
                return SourceFile == otherLayerEntryTemplate.SourceFile
                    && ExtractionPath ==otherLayerEntryTemplate.ExtractionPath
                    && LastModifiedTime == otherLayerEntryTemplate.LastModifiedTime
                    && Permissions == otherLayerEntryTemplate.Permissions;
            }

            public override int GetHashCode()
            {
                return Objects.hash(SourceFile, ExtractionPath, LastModifiedTime, Permissions);
            }

            public override string ToString()
            {
                return $"{SourceFile} => {ExtractionPath}";
            }
        }

        /**
         * Converts a list of {@link LayerEntry}s into a list of {@link LayerEntryTemplate}. The list is
         * sorted by source file first, then extraction path (see {@link LayerEntryTemplate#compareTo}).
         *
         * @param layerEntries the list of {@link LayerEntry} to convert
         * @return list of {@link LayerEntryTemplate} after sorting
         * @throws IOException if checking the file creation time of a layer entry fails
         */

        public static List<LayerEntryTemplate> toSortedJsonTemplates(IList<LayerEntry> layerEntries)
        {
            List<LayerEntryTemplate> jsonTemplates = new List<LayerEntryTemplate>();
            foreach (LayerEntry entry in layerEntries)
            {
                jsonTemplates.add(new LayerEntryTemplate(entry));
            }
            jsonTemplates.Sort();
            return jsonTemplates;
        }

        /**
         * Generates a selector for the list of {@link LayerEntry}s. The selector is unique to each unique
         * set of layer entries, regardless of order. TODO: Should we care about order?
         *
         * @param layerEntries the layer entries
         * @return the selector
         * @throws IOException if an I/O exception occurs
         */
        public static async Task<DescriptorDigest> generateSelectorAsync(ImmutableArray<LayerEntry> layerEntries)
        {
            return await Digests.computeJsonDigestAsync(toSortedJsonTemplates(layerEntries)).ConfigureAwait(false);
        }
    }
}
