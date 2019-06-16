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

namespace Jib.Net.Core
{
    public static class PosixFilePermissionExtensions
    {
        public static string ToOctalString(this PosixFilePermission permissions)
        {
            return Convert.ToString((int)(permissions & PosixFilePermission.ALL), 8).PadLeft(3, '0');
        }
    }

    [Flags]
    public enum PosixFilePermission : int
    {
        NONE = 0,
        OTHERS_EXECUTE = 1<< 0,
        OTHERS_WRITE = 1 << 1,
        OTHERS_READ = 1 << 2,
        OTHERS_READ_EXECUTE = OTHERS_READ | OTHERS_EXECUTE,
        OTHERS_ALL = OTHERS_READ | OTHERS_WRITE | OTHERS_EXECUTE,
        GROUP_EXECUTE = 1 << 3,
        GROUP_WRITE = 1 << 4,
        GROUP_READ = 1 << 5,
        GROUP_READ_EXECUTE = GROUP_READ | GROUP_EXECUTE,
        GROUP_ALL = GROUP_READ | GROUP_WRITE | GROUP_EXECUTE,
        OWNER_EXECUTE = 1 << 6,
        OWNER_WRITE = 1 << 7,
        OWNER_READ = 1 << 8,
        OWNER_READ_EXECUTE = OWNER_READ | OWNER_EXECUTE,
        OWNER_ALL = OWNER_READ | OWNER_WRITE | OWNER_EXECUTE,
        ALL = OWNER_ALL | GROUP_ALL | OTHERS_ALL,

    }
}