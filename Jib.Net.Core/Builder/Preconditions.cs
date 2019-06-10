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
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;

namespace Jib.Net.Core.Global
{
    internal static class Preconditions
    {
        internal static void checkState(bool v)
        {
            throw new NotImplementedException();
        }
        internal static void checkState(bool v,string message)
        {
            throw new NotImplementedException();
        }

        internal static void checkArgument(bool v1)
        {
            throw new NotImplementedException();
        }

        internal static void checkArgument(bool v1, string v2, params object[] values)
        {
            throw new NotImplementedException();
        }

        internal static T checkNotNull<T>(T rootProgressAllocationDescription, string v)
        {
            throw new NotImplementedException();
        }

        internal static T checkNotNull<T>(T contentDescriptorTemplate)
        {
            throw new NotImplementedException();
        }
    }
}