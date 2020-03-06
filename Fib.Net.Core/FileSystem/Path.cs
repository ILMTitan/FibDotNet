// Copyright 2018 Google LLC. All rights reserved.
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Fib.Net.Core.Api;
using Newtonsoft.Json;

namespace Fib.Net.Core.FileSystem
{
    [JsonObject]
    [JsonConverter(typeof(SystemPathConverter))]
    public class SystemPath : IComparable<SystemPath>
    {
        private readonly string path;

        public static implicit operator string(SystemPath path)
        {
            return path?.path;
        }

        public static implicit operator SystemPath(FileInfo fileInfo)
        {
            return FromFileInfo(fileInfo);
        }

        public static SystemPath FromFileInfo(FileInfo fileInfo)
        {
            var fullName = fileInfo?.FullName;
            if (fullName is null)
            {
                return null;
            }
            return new SystemPath(fullName);
        }

        public SystemPath(string path)
        {
            path = path ?? throw new ArgumentNullException(nameof(path));
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (path != Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture))
            {
                path = path.TrimEnd(Path.DirectorySeparatorChar);
            }
            this.path = path;
        }

        public SystemPath(FileSystemInfo fileInfo)
        {
            path = fileInfo?.FullName ?? throw new ArgumentNullException(nameof(fileInfo));
        }

        public SystemPath(string[] pathParts)
        {
            path = Path.Combine(pathParts);
            if(path.Length > 1)
            {
                path = path.TrimEnd(Path.DirectorySeparatorChar);
            }
        }

        public SystemPath Resolve()
        {
            if (Path.IsPathRooted(path))
            {
                return this;
            }
            else
            {
                return new SystemPath(Directory.GetCurrentDirectory()).Resolve(path);
            }
        }

        public SystemPath Resolve(string pathToResolve)
        {
            if (string.IsNullOrEmpty(pathToResolve))
            {
                return this;
            }
            else if (Path.IsPathRooted(pathToResolve))
            {
                return new SystemPath(pathToResolve);
            }
            return new SystemPath(Path.Combine(path,pathToResolve));
        }

        internal SystemPath GetRoot()
        {
            if (Path.IsPathRooted(path))
            {
                return new SystemPath(Path.GetPathRoot(path));
            } else
            {
                return null;
            }
        }

        public IEnumerator<SystemPath> GetEnumerator()
        {
            string rootlessPath;
            if (Path.IsPathRooted(path))
            {
                rootlessPath = path.Substring(Path.GetPathRoot(path).Length);
            } else
            {
                rootlessPath = path;
            }
            char[] separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            return rootlessPath.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => new SystemPath(p))
                .GetEnumerator();
        }

        internal DirectoryInfo ToDirectory()
        {
            return new DirectoryInfo(path);
        }

        public SystemPath Resolve(SystemPath relativePath)
        {
            relativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
            return new SystemPath(Path.Combine(path, relativePath.path));
        }

        internal SystemPath Relativize(SystemPath path)
        {
            Uri relativeUri = new Uri(this.path +Path.DirectorySeparatorChar).MakeRelativeUri(path.ToURI());
            return new SystemPath(relativeUri.ToString());
        }

        public SystemPath GetParent()
        {
            if (Path.GetPathRoot(path) == path)
            {
                return null;
            }
            var dirName = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dirName))
            {
                return new SystemPath(dirName);
            }
            else if (string.IsNullOrEmpty(Path.GetPathRoot(path)))
            {
                return null;
            }
            else
            {
                return new SystemPath(Path.GetPathRoot(path));
            }
        }

        public Uri ToURI()
        {
            return new Uri(path);
        }

        internal FileInfo ToFile()
        {
            return new FileInfo(path);
        }

        public override string ToString()
        {
            return path;
        }

        public RelativeUnixPath GetFileName()
        {
            return RelativeUnixPath.Get(Path.GetFileName(path));
        }

        internal AbsoluteUnixPath ToAbsolutePath()
        {
            return AbsoluteUnixPath.FromPath(this);
        }

        public static bool operator == (SystemPath path1, SystemPath path2)
        {
            return Equals(path1, path2);
        }

        public static bool operator != (SystemPath path1, SystemPath path2)
        {
            return !Equals(path1, path2);
        }

        public override bool Equals(object obj)
        {
            return obj is SystemPath otherPath &&
                Equals(path, otherPath.path);
        }

        public override int GetHashCode()
        {
            return -1757656154 + EqualityComparer<string>.Default.GetHashCode(path);
        }

        public int CompareTo(SystemPath other)
        {
            return string.CompareOrdinal(path, other?.path);
        }

        public static bool operator <(SystemPath left, SystemPath right)
        {
            return left is null ? !(right is null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(SystemPath left, SystemPath right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(SystemPath left, SystemPath right)
        {
            return !(left is null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(SystemPath left, SystemPath right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        public static SystemPath From(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            return new SystemPath(path);
        }
    }
}