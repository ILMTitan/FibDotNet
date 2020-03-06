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

using System;
using System.Globalization;

namespace Fib.Net.Core
{
    internal static class Preconditions
    {
        internal static void CheckArgument(bool isValid, string messageFormat, params object[] values)
        {
            if (!isValid)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, messageFormat, values));
            }
        }

        internal static T CheckNotNull<T>(T value, string message) where T : class
        {
            return value ?? throw new ArgumentNullException(nameof(value), message);
        }

        internal static T CheckNotNull<T>(T value) where T : class
        {
            return value ?? throw new ArgumentNullException(nameof(value));
        }

        internal static T CheckNotNull<T>(T? value) where T : struct
        {
            return value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}