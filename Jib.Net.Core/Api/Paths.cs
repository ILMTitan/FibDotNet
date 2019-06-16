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

using Jib.Net.Core.FileSystem;
using System;
using System.IO;
using System.Linq;

namespace Jib.Net.Core.Api
{
    public static class Paths
    {
        public static SystemPath get(params string[] pathParts)
        {
            return new SystemPath(
                pathParts.Select(
                    pathPart => pathPart.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))
                .ToArray());
        }

        internal static SystemPath get(Uri uri)
        {
            if(uri.Scheme == "file")
            {
                return new SystemPath(uri.AbsolutePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            }
            else
            {
                throw new ArgumentException($"Scheme must be \"file\" but was \"{uri.Scheme}\"", nameof(uri));
            }
        }
    }
}