/*
 * Copyright 2018 Google LLC. All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Api;

namespace Jib.Net.Core.FileSystem
{
    public class SystemPath : IComparable<SystemPath>
    {
        private readonly string path;

        public static implicit operator SystemPath(AbsoluteUnixPath absolutePath)
        {
            if(absolutePath.OriginalRoot == null)
            {
                return new SystemPath(absolutePath.ToString());
            }
            else
            {
                return new SystemPath(Path.Combine(
                    absolutePath.OriginalRoot.ToString(),
                    Path.Combine(absolutePath.PathComponents.ToArray())));
            }
        }
        public static implicit operator string(SystemPath path)
        {
            return path.path;
        }

        public SystemPath(string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (path != Path.DirectorySeparatorChar.ToString())
            {
                path = path.TrimEnd(Path.DirectorySeparatorChar);
            }
            this.path = path;
        }

        public SystemPath(FileSystemInfo fileInfo)
        {
            path = fileInfo.FullName;
        }

        public SystemPath(string[] pathParts)
        {
            path = Path.Combine(pathParts);
            if(path.Length > 1)
            {
                path = path.TrimEnd(Path.DirectorySeparatorChar);
            }
        }

        public SystemPath resolve(string pathToResolve)
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

        internal SystemPath getRoot()
        {
            if (Path.IsPathRooted(path))
            {
                return new SystemPath(Path.GetPathRoot(path));
            } else
            {
                return null;
            }
        }

        internal object getNameCount()
        {
            throw new NotImplementedException();
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

        internal DirectoryInfo toDirectory()
        {
            return new DirectoryInfo(path);
        }

        public SystemPath resolve(SystemPath relativePath)
        {
            return new SystemPath(Path.Combine(path, relativePath.path));
        }

        internal SystemPath relativize(SystemPath path)
        {
            Uri relativeUri = new Uri(this.path +Path.DirectorySeparatorChar).MakeRelativeUri(path.toURI());
            return new SystemPath(relativeUri.ToString());
        }

        public SystemPath getParent()
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

        public Uri toURI()
        {
            return new Uri(path);
        }

        internal FileInfo toFile()
        {
            return new FileInfo(path);
        }

        public override string ToString()
        {
            return path;
        }

        public RelativeUnixPath getFileName()
        {
            return RelativeUnixPath.get(Path.GetFileName(path));
        }

        internal AbsoluteUnixPath toAbsolutePath()
        {
            return AbsoluteUnixPath.fromPath(this);
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
            return string.Compare(path, other?.path);
        }
    }
}