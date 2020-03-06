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

using System;
using Newtonsoft.Json;

namespace Fib.Net.Core.Api
{
    public class AbsoluteUnixPathConverter : JsonConverter<AbsoluteUnixPath>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override AbsoluteUnixPath ReadJson(
            JsonReader reader,
            Type objectType,
            AbsoluteUnixPath existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            return AbsoluteUnixPath.Get(serializer.Deserialize<string>(reader));
        }

        public override void WriteJson(JsonWriter writer, AbsoluteUnixPath value, JsonSerializer serializer)
        {
            serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            serializer.Serialize(writer, value?.ToString());
        }
    }
}