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

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Fib.Net.Core.Registry.Json
{
    /**
     * Template for the registry response body JSON when a request errored.
     *
     * <p>Example:
     *
     * <pre>{@code
     * {
     *   "errors": [
     *     {
     *       "code": "MANIFEST_UNKNOWN",
     *       "message": "manifest unknown",
     *       "detail": {"Tag": "latest"}
     *     }
     *   ]
     * }
     * }</pre>
     */
    [JsonObject(
        NamingStrategyType = typeof(CamelCaseNamingStrategy),
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ErrorResponseTemplate
    {
        private readonly List<ErrorEntryTemplate> _errors = new List<ErrorEntryTemplate>();

        public IReadOnlyList<ErrorEntryTemplate> Errors
        {
            get { return _errors; }
        }

        public ErrorResponseTemplate AddError(ErrorEntryTemplate errorEntryTemplate)
        {
            _errors.Add(errorEntryTemplate);
            return this;
        }
    }
}
