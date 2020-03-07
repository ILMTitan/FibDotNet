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
using System.Collections.Generic;
using System.IO;
using Fib.Net.Core.FileSystem;
using NodaTime;

namespace Fib.Net.Core.Api
{
    public static class Files
    {
        public static SystemPath CreateTempDirectory(SystemPath basePath, string name)
        {
            return CreateTempDirectory(basePath.ToString(), name);
        }

        internal static SystemPath CreateTempDirectory(string basePath, string name)
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

        internal static SystemPath CreateTempDirectory(string p)
        {
            return CreateTempDirectory(Path.GetTempPath(), p);
        }

        internal static bool IsDirectory(SystemPath sourceFile)
        {
            return Directory.Exists(sourceFile.ToString());
        }

        public static void Copy(SystemPath source, SystemPath destination)
        {
                File.Copy(source, destination);
        }

        public static SystemPath CreateDirectories(SystemPath directory)
        {
            directory = directory ?? throw new ArgumentNullException(directory);
            return new SystemPath(Directory.CreateDirectory(directory.ToString()));
        }

        public static IEnumerable<SystemPath> List(SystemPath sourceFile)
        {
            sourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
            foreach (var entry in sourceFile.ToDirectory().EnumerateFileSystemInfos())
            {
                yield return new SystemPath(entry);
            }
        }

        internal static Instant GetLastModifiedTime(SystemPath systemPath)
        {
            return Instant.FromDateTimeUtc(File.GetLastWriteTimeUtc(systemPath.ToString()));
        }

        internal static IEnumerable<SystemPath> Walk(SystemPath rootDir)
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

        internal static byte[] ReadAllBytes(SystemPath file)
        {
            return File.ReadAllBytes(file.ToString());
        }

        public static bool Exists(SystemPath path)
        {
            return File.Exists(path?.ToString()) || Directory.Exists(path?.ToString());
        }

        internal static Stream NewInputStream(SystemPath file)
        {
            return File.OpenRead(file.ToString());
        }

        internal static void Move(SystemPath source, SystemPath destination)
        {
            Directory.Move(source, destination);
        }

        internal static TemporaryFile CreateTempFile(SystemPath systemPath)
        {
            return new TemporaryFile(systemPath.Resolve(Path.GetRandomFileName()));
        }

        public static Stream NewOutputStream(SystemPath path)
        {
            return File.OpenWrite(path);
        }

        internal static void Move(SystemPath source, SystemPath destination, StandardCopyOption copyOption)
        {
            try
            {
                File.Move(source, destination);
            }
            catch (IOException) when (File.Exists(destination) && copyOption.HasFlag(StandardCopyOption.REPLACE_EXISTING))
            {
                var backupPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                File.Replace(source, destination, backupPath);
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }

        public static SystemPath CreateDirectory(SystemPath path)
        {
            return new SystemPath(Directory.CreateDirectory(path));
        }

        internal static void DeleteIfExists(SystemPath file)
        {
            File.Delete(file);
        }

        internal static TemporaryFile CreateTempFile()
        {
            TemporaryFile temporaryFile = new TemporaryFile();
            File.Create(temporaryFile.Path).Dispose();
            return temporaryFile;
        }

        internal static long Size(SystemPath path)
        {
            return new FileInfo(path).Length;
        }

        public static SystemPath CreateFile(SystemPath path)
        {
            File.Create(path).Dispose();
            return path;
        }

        public static SystemPath Write(SystemPath path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
            return path;
        }

        public static void SetLastModifiedTime(SystemPath path, DateTime newWriteTime)
        {
            new FileInfo(path).LastWriteTimeUtc = newWriteTime;
        }
    }
}