﻿/*
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

namespace com.google.cloud.tools.jib.api
{
    internal static class Objects
    {
        internal static int hash(params object[] values)
        {
            int nullHashValue = EqualityComparer<object>.Default.GetHashCode(null);
            int hashValue = nullHashValue;
            foreach(var value in values)
            {
                hashValue *= 31;
                hashValue += value?.GetHashCode() ?? nullHashValue;
            }
            return hashValue;
        }

        internal static bool isNull(object arg)
        {
            return arg == null;
        }

        internal static bool nonNull(object arg)
        {
            return arg != null;
        }

        internal static bool nonNull<T>(T? arg) where T : struct
        {
            return arg != null;
        }
    }
}