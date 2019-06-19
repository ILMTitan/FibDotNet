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

using System;
using Newtonsoft.Json;
using NodaTime;

namespace com.google.cloud.tools.jib.cache
{
    internal class InstantConverter : JsonConverter<Instant>
    {
        public override Instant ReadJson(JsonReader reader, Type objectType, Instant existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string s = serializer.Deserialize<string>(reader);
            return Instant.FromDateTimeUtc(DateTime.Parse(s).ToUniversalTime());
        }

        public override void WriteJson(JsonWriter writer, Instant value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToDateTimeUtc());
        }
    }
}