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

namespace Fib.Net.Core
{
    public static class PosixFilePermissionExtensions
    {
        public static string ToOctalString(this PosixFilePermissions permissions)
        {
            return Convert.ToString((int)(permissions & PosixFilePermissions.All), 8).PadLeft(3, '0');
        }
    }

    [Flags]
    public enum PosixFilePermissions : int
    {
        None = 0,
        OthersExecute = 1 << 0,
        OthersWrite = 1 << 1,
        OthersRead = 1 << 2,
        OthersReadExecute = OthersRead | OthersExecute,
        OthersAll = OthersRead | OthersWrite | OthersExecute,
        GroupExecute = 1 << 3,
        GroupWrite = 1 << 4,
        GroupRead = 1 << 5,
        GroupReadExecute = GroupRead | GroupExecute,
        GroupAll = GroupRead | GroupWrite | GroupExecute,
        OwnerExecute = 1 << 6,
        OwnerWrite = 1 << 7,
        OwnerRead = 1 << 8,
        OwnerReadExecute = OwnerRead | OwnerExecute,
        OwnerAll = OwnerRead | OwnerWrite | OwnerExecute,
        All = OwnerAll | GroupAll | OthersAll,

    }
}