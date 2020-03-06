
// Copyright 2018 Google LLC.
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

namespace Fib.Net.Core
{
    internal static class Objects
    {
        internal static int Hash(params object[] values)
        {
            int nullHashValue = EqualityComparer<object>.Default.GetHashCode(null);
            int hashValue = nullHashValue;
            foreach (var value in values)
            {
                hashValue *= 31;
                hashValue += value?.GetHashCode() ?? nullHashValue;
            }
            return hashValue;
        }
    }
}