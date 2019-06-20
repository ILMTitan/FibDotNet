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
using System.Globalization;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;

namespace Jib.Net.Core.Global
{
    internal static class Preconditions
    {
        internal static void checkState(bool v)
        {
            if (!v)
            {
                throw new ArgumentException();
            }
        }

        internal static void checkArgument(bool v1)
        {
            if (!v1)
            {
                throw new ArgumentException();
            }
        }

        internal static void checkArgument(bool isValid, string messageFormat, params object[] values)
        {
            if (!isValid)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, messageFormat, values));
            }
        }

        internal static T checkNotNull<T>(T value, string message) where T : class
        {
            return value ?? throw new ArgumentNullException(message, nameof(value));
        }

        internal static T checkNotNull<T>(T value) where T : class
        {
            return value ?? throw new ArgumentNullException();
        }

        internal static T checkNotNull<T>(T? value) where T: struct
        {
            return value ?? throw new ArgumentNullException();
        }
    }
}