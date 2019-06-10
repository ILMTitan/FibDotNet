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

using System;
using com.google.cloud.tools.jib.json;

namespace com.google.cloud.tools.jib.registry
{
    internal class ObjectNode : JsonTemplate
    {
        internal bool has(string v)
        {
            throw new NotImplementedException();
        }

        internal ObjectNode get(string v)
        {
            throw new NotImplementedException();
        }

        internal int asInt(int v)
        {
            throw new NotImplementedException();
        }

        internal string asText()
        {
            throw new NotImplementedException();
        }
    }
}