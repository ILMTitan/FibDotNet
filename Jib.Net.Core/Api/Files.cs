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
using System.Linq;
using com.google.cloud.tools.jib.cache;
using Jib.Net.Core.FileSystem;
using NodaTime;

namespace Jib.Net.Core.Api
{
    public static class Files
    {
        internal static SystemPath createTempDirectory(SystemPath basePath, string name)
        {
            return createTempDirectory(basePath.ToString(), name);
        }

        internal static SystemPath createTempDirectory(string basePath, string name)
        {
            string newPath;
            if (string.IsNullOrWhiteSpace(name))
            {
                newPath = Path.Combine(basePath, Path.GetRandomFileName());
            }
            else
            {
                newPath = Path.Combine(basePath, name);
            }
            Directory.CreateDirectory(newPath);
            return new SystemPath(newPath);
        }

        internal static SystemPath createTempDirectory(string p)
        {
            return createTempDirectory(Path.GetTempPath(), p);
        }

        internal static bool isDirectory(SystemPath sourceFile)
        {
            return Directory.Exists(sourceFile.ToString());
        }

        public static void copy(SystemPath source, SystemPath destination)
        {
                File.Copy(source, destination);
        }

        public static SystemPath createDirectories(SystemPath directory)
        {
            return new SystemPath(Directory.CreateDirectory(directory.ToString()));
        }

        public static IEnumerable<SystemPath> list(SystemPath sourceFile)
        {
            foreach(var entry in sourceFile.toDirectory().EnumerateFileSystemInfos())
            {
                yield return new SystemPath(entry);
            }
        }

        internal static Instant getLastModifiedTime(SystemPath systemPath)
        {
            return Instant.FromDateTimeUtc(File.GetLastWriteTimeUtc(systemPath.ToString()));
        }

        internal static IEnumerable<SystemPath> walk(SystemPath rootDir)
        {
            Stack<SystemPath> pathStack = new Stack<SystemPath>();
            pathStack.Push(rootDir);
            while (pathStack.Count > 0)
            {
                SystemPath currentPath = pathStack.Pop();
                yield return currentPath;
                if (Directory.Exists(currentPath))
                {
                    foreach (var path in Directory.EnumerateFileSystemEntries(currentPath))
                    {
                        pathStack.Push(new SystemPath(path));
                    }
                }
            }
        }

        internal static byte[] readAllBytes(SystemPath file)
        {
            return File.ReadAllBytes(file.ToString());
        }

        public static bool exists(SystemPath path)
        {
            return File.Exists(path.ToString()) || Directory.Exists(path.ToString());
        }

        internal static Stream newInputStream(SystemPath file)
        {
            return File.OpenRead(file.ToString());
        }

        internal static void move(SystemPath source, SystemPath destination)
        {
            Directory.Move(source, destination);
        }

        internal static TemporaryFile createTempFile(SystemPath systemPath)
        {
            return new TemporaryFile(systemPath.resolve(Path.GetRandomFileName()));
        }

        public static Stream newOutputStream(SystemPath path)
        {
            return File.OpenWrite(path);
        }

        internal static void move(SystemPath source, SystemPath destination, StandardCopyOption copyOption)
        {
            try
            {
                File.Move(source, destination);
            }
            catch (IOException)
            {
                if (File.Exists(destination) && copyOption.HasFlag(StandardCopyOption.REPLACE_EXISTING))
                {
                    var backupPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    File.Replace(source, destination, backupPath);
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        public static SystemPath createDirectory(SystemPath path)
        {
            return new SystemPath(Directory.CreateDirectory(path));
        }

        internal static void deleteIfExists(SystemPath file)
        {
            File.Delete(file);
        }

        internal static TemporaryFile createTempFile()
        {
            TemporaryFile temporaryFile = new TemporaryFile();
            File.Create(temporaryFile.Path).Dispose();
            return temporaryFile;
        }

        internal static long size(SystemPath path)
        {
            return new FileInfo(path).Length;
        }

        public static SystemPath createFile(SystemPath path)
        {
            File.Create(path).Dispose();
            return path;
        }

        public static SystemPath write(SystemPath path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
            return path;
        }

        public static void setLastModifiedTime(SystemPath path, DateTime newWriteTime)
        {
            new FileInfo(path).LastWriteTimeUtc = newWriteTime;
        }
    }
}