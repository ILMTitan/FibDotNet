// Copyright 2018 Google LLC. All rights reserved.
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
using System.Diagnostics;
using Newtonsoft.Json;

namespace Fib.Net.Core.FileSystem
{
    public class SystemPathConverter : JsonConverter<SystemPath>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override SystemPath ReadJson(
            JsonReader reader,
            Type objectType,
            SystemPath existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            Debug.Assert(serializer != null);
            return SystemPath.From(serializer.Deserialize<string>(reader));
        }

        public override void WriteJson(JsonWriter writer, SystemPath value, JsonSerializer serializer)
        {
            Debug.Assert(serializer != null);
            serializer.Serialize(writer, (string)value);
        }
    }
}