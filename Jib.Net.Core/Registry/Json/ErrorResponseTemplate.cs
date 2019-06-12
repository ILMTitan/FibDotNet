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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Global;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.registry.json
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
    public class ErrorResponseTemplate : JsonTemplate
    {
        private readonly List<ErrorEntryTemplate> errors = new List<ErrorEntryTemplate>();

        public IReadOnlyList<ErrorEntryTemplate> getErrors()
        {
            return Collections.unmodifiableList(errors);
        }

        public ErrorResponseTemplate addError(ErrorEntryTemplate errorEntryTemplate)
        {
            errors.add(errorEntryTemplate);
            return this;
        }
    }
}
