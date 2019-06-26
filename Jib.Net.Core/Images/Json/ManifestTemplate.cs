/*
 * Copyright 2017 Google LLC.
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
using com.google.cloud.tools.jib.registry.json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jib.Net.Core.Images.Json
{
    /** Parent class for image manifest JSON templates. */
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public interface IManifestTemplate
    {
        int SchemaVersion { get; }
    }
}
