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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Fib.Net.Core.Registry
{
    internal class TolerantStringEnumConverter<T> : JsonConverter<T?> where T : struct
    {
        public readonly StringEnumConverter converter;

        public T? DefaultValue { get; }

        public TolerantStringEnumConverter(T defaultValue, Type namingStrategyType) : this(namingStrategyType)
        {
            DefaultValue = defaultValue;
        }

        public TolerantStringEnumConverter(Type namingStrategyType)
        {
            converter = new StringEnumConverter(namingStrategyType);
        }

        public override T? ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                return (T?) converter.ReadJson(reader, objectType, existingValue, serializer);
            }
            catch (JsonException)
            {
                return DefaultValue;
            }
        }

        public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
        {
            converter.WriteJson(writer, value, serializer);
        }
    }
}