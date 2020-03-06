// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Fib.Net.Core.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fib.Net.Cli
{
    public class JsonPortsConverter : JsonConverter<IReadOnlyCollection<Port>>
    {
        public override IReadOnlyCollection<Port> ReadJson(JsonReader reader, Type objectType, IReadOnlyCollection<Port> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            IEnumerable<string> ports = serializer.Deserialize<IEnumerable<string>>(reader);
            return Port.Parse(ports);
        }

        public override void WriteJson(JsonWriter writer, IReadOnlyCollection<Port> value, JsonSerializer serializer)
        {
            serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            serializer.Serialize(writer, value.Select(p => p.ToString()));
        }
    }
}