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
using System.IO;
using Jib.Net.Core.FileSystem;

namespace Jib.Net.Core.Api
{
    internal static class Files
    {
        internal static SystemPath createTempDirectory(object p, object x)
        {
            throw new NotImplementedException();
        }
        internal static SystemPath createTempDirectory(object p)
        {
            throw new NotImplementedException();
        }

        internal static bool isDirectory(SystemPath sourceFile)
        {
            throw new NotImplementedException();
        }

        internal static IEnumerable<SystemPath> list(SystemPath sourceFile)
        {
            throw new NotImplementedException();
        }

        internal static DateTime getLastModifiedTime(SystemPath systemPath)
        {
            throw new NotImplementedException();
        }

        internal static IEnumerable<SystemPath> walk(SystemPath rootDir)
        {
            throw new NotImplementedException();
        }

        internal static void createDirectories(SystemPath destPath)
        {
            throw new NotImplementedException();
        }

        internal static bool exists(SystemPath temporaryDirectory)
        {
            throw new NotImplementedException();
        }

        internal static void copy(SystemPath path, SystemPath destPath)
        {
            throw new NotImplementedException();
        }

        internal static Stream newInputStream(SystemPath jsonFile)
        {
            throw new NotImplementedException();
        }

        internal static void move(SystemPath source, SystemPath destination)
        {
            throw new NotImplementedException();
        }

        internal static SystemPath createTempFile(SystemPath systemPath, object p1, object p2)
        {
            throw new NotImplementedException();
        }

        internal static Stream newOutputStream(SystemPath temporaryFile)
        {
            throw new NotImplementedException();
        }

        internal static void move(SystemPath temporarySelectorFile, SystemPath selectorFile, StandardCopyOption aTOMIC_MOVE, StandardCopyOption rEPLACE_EXISTING)
        {
            throw new NotImplementedException();
        }

        internal static void move(SystemPath temporaryFile, SystemPath destination, StandardCopyOption rEPLACE_EXISTING)
        {
            throw new NotImplementedException();
        }

        internal static SystemPath createTempFile(object p1, object p2)
        {
            throw new NotImplementedException();
        }

        internal static long size(SystemPath fileInLayerDirectory)
        {
            throw new NotImplementedException();
        }
    }
}