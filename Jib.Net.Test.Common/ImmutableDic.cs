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
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.api
{
    public static class ImmutableDic
    {
        public static ImmutableDictionary<TKey, TValue> of<TKey, TValue>(TKey key, TValue value)
        {
            return new Dictionary<TKey, TValue> { [key] = value }.ToImmutableDictionary();
        }

        public static ImmutableDictionary<TKey, TValue> of<TKey, TValue>(TKey key1, TValue value1, TKey key2, TValue value2)
        {
            return new Dictionary<TKey, TValue> { [key1] = value1, [key2] = value2 }.ToImmutableDictionary();
        }

        public static ImmutableDictionary<T, T> of<T>(params T[] v1)
        {
            var builder = ImmutableDictionary.CreateBuilder<T, T>();
            for (int i = 0; i + 1 < v1.Length; i += 2)
            {
                builder.Add(v1[i], v1[i + 1]);
            }
            return builder.ToImmutable();
        }
    }
}