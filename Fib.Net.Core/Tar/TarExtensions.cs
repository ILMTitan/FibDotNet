// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using ICSharpCode.SharpZipLib.Tar;
using System;

namespace Fib.Net.Core.Tar
{
    public static class TarExtensions
    {
        public static bool IsFile(this TarEntry entry)
        {
            entry = entry ?? throw new ArgumentNullException(nameof(entry));
            return entry.TarHeader.TypeFlag != TarHeader.LF_DIR;
        }

        public static void SetMode(this TarEntry entry, PosixFilePermissions mode)
        {
            entry = entry ?? throw new ArgumentNullException(nameof(entry));
            entry.TarHeader.Mode = (int)mode;
        }

        public static PosixFilePermissions GetMode(this TarEntry entry)
        {
            entry = entry ?? throw new ArgumentNullException(nameof(entry));
            return (PosixFilePermissions)entry.TarHeader.Mode;
        }
    }
}
