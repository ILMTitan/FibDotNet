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

namespace com.google.cloud.tools.jib.filesystem
{
    internal class Splitter
    {
        internal static Splitter on(char v)
        {
            throw new NotImplementedException();
        }

        internal static Splitter on(string v)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<string> split(string unixPath)
        {
            throw new NotImplementedException();
        }

        internal IList<string> splitToList(string layers)
        {
            throw new NotImplementedException();
        }
    }
}