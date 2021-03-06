﻿// Copyright 2018 Google LLC.
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

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fib.Net.Test.Common
{
    public static class ImmutableDic
    {
        public static ImmutableDictionary<TKey, TValue> Of<TKey, TValue>(TKey key, TValue value)
        {
            return ImmutableDictionary<TKey, TValue>.Empty.Add(key, value);
        }

        public static ImmutableDictionary<TKey, TValue> Of<TKey, TValue>(TKey key1, TValue value1, TKey key2, TValue value2)
        {
            return new Dictionary<TKey, TValue> { [key1] = value1, [key2] = value2 }.ToImmutableDictionary();
        }
    }
}