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

using Fib.Net.Core.FileSystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Fib.Net.Core.Json
{
    // TODO: Add JsonFactory for HTTP response parsing.
    /**
     * Helper class for serializing and deserializing JSON.
     *
     * <p>The interface uses Jackson as the JSON parser. Some useful annotations to include on classes
     * used as templates for JSON are:
     *
     * <p>{@code @JsonInclude(JsonInclude.Include.NON_NULL)}
     *
     * <ul>
     *   <li>Does not serialize fields that are {@code null}.
     * </ul>
     *
     * {@code @JsonAutoDetect(fieldVisibility = JsonAutoDetect.Visibility.ANY)}
     *
     * <ul>
     *   <li>Fields that are private are also accessible for serialization/deserialization.
     * </ul>
     *
     * @see <a href="https://github.com/FasterXML/jackson">https://github.com/FasterXML/jackson</a>
     */
    public static class JsonTemplateMapper
    {
        /**
         * Deserializes a JSON file via a JSON object template.
         *
         * @param <T> child type of {@link JsonTemplate}
         * @param jsonFile a file containing a JSON string
         * @param templateClass the template to deserialize the string to
         * @return the template filled with the values parsed from {@code jsonFile}
         * @throws IOException if an error occurred during reading the file or parsing the JSON
         */
        public static T ReadJsonFromFile<T>(SystemPath jsonFile)
        {
            jsonFile = jsonFile ?? throw new ArgumentNullException(nameof(jsonFile));
            using (StreamReader reader = jsonFile.ToFile().OpenText())
            {
                return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            }
        }

        /**
         * Deserializes a JSON file via a JSON object template with a shared lock on the file
         *
         * @param <T> child type of {@link JsonTemplate}
         * @param jsonFile a file containing a JSON string
         * @param templateClass the template to deserialize the string to
         * @return the template filled with the values parsed from {@code jsonFile}
         * @throws IOException if an error occurred during reading the file or parsing the JSON
         */
        public static T ReadJsonFromFileWithLock<T>(SystemPath jsonFile)
        {
            return ReadJsonFromFile<T>(jsonFile);
        }

        /**
         * Deserializes a JSON object from a JSON string.
         *
         * @param <T> child type of {@link JsonTemplate}
         * @param jsonString a JSON string
         * @param templateClass the template to deserialize the string to
         * @return the template filled with the values parsed from {@code jsonString}
         * @throws IOException if an error occurred during parsing the JSON
         */
        public static T ReadJson<T>(string jsonString) => JsonConvert.DeserializeObject<T>(jsonString);

        /**
         * Deserializes a JSON object list from a JSON string.
         *
         * @param <T> child type of {@link JsonTemplate}
         * @param jsonString a JSON string
         * @param templateClass the template to deserialize the string to
         * @return the template filled with the values parsed from {@code jsonString}
         * @throws IOException if an error occurred during parsing the JSON
         */
        public static List<T> ReadListOfJson<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<List<T>>(jsonString);
        }

        public static string ToUtf8String(object template)
        {
            return Encoding.UTF8.GetString(ToByteArray(template));
        }

        public static byte[] ToByteArray(object template)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(template));
        }

        public static async Task WriteToAsync(object template, Stream stream)
        {
            var jsonString = JsonConvert.SerializeObject(template);
            Debug.WriteLine(jsonString);
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(jsonString).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}
