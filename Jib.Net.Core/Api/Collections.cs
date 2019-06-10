/*
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
using System.Collections.Generic;
using System.Linq;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry.json;

namespace com.google.cloud.tools.jib.api
{
    internal static class Collections
    {
        internal static IDictionary<TKey, TValue> emptyMap<TKey, TValue>()
        {
            throw new NotImplementedException();
        }

        internal static List<T> singletonList<T>(T value)
        {
            return new List<T> { value };
        }

        internal static void sort<T>(List<T> jsonTemplates) where T: IComparable<T>
        {

            jsonTemplates.Sort();
        }

        internal static IReadOnlyList<T> unmodifiableList<T>(IList<T> errors)
        {
            return errors as IReadOnlyList<T> ?? errors.ToList();
        }

        internal static IList<T> emptyList<T>()
        {
            return new T[0];
        }
    }
}