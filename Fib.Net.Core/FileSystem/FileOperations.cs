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

using Fib.Net.Core.Api;
using System;
using System.Collections.Immutable;
using System.IO;

namespace Fib.Net.Core.FileSystem
{
    /** Static methods for operating on the filesystem. */
    public sealed class FileOperations
    {
        /**
         * Copies {@code sourceFiles} to the {@code destDir} directory.
         *
         * @param sourceFiles the list of source files.
         * @param destDir the directory to copy the files to.
         * @throws IOException if the copy fails.
         */
        public static void Copy(ImmutableArray<SystemPath> sourceFiles, SystemPath destDir)
        {
            foreach (SystemPath sourceFile in sourceFiles)
            {
                PathConsumer copyPathConsumer =
                    path =>
                    {
                        // Creates the same path in the destDir.
                        SystemPath destPath = destDir.Resolve(sourceFile.GetParent().Relativize(path));
                        if (Files.IsDirectory(path))
                        {
                            Files.CreateDirectories(destPath);
                        }
                        else
                        {
                            Files.Copy(path, destPath);
                        }
                    };

                if (Files.IsDirectory(sourceFile))
                {
                    new DirectoryWalker(sourceFile).Walk(copyPathConsumer);
                }
                else
                {
                    copyPathConsumer.Accept(sourceFile);
                }
            }
        }

        /**
         * Acquires an exclusive {@link FileLock} on the {@code file} and opens an {@link OutputStream} to
         * write to it. The file will be created if it does not exist, or truncated to length 0 if it does
         * exist. The {@link OutputStream} must be closed to release the lock.
         *
         * <p>The locking mechanism should not be used as a concurrency management feature. Rather, this
         * should be used as a way to prevent concurrent writes to {@code file}. Concurrent attempts to
         * lock {@code file} will result in {@link OverlappingFileLockException}s.
         *
         * @param file the file to write to
         * @return an {@link OutputStream} that writes to the file
         * @throws IOException if an I/O exception occurs
         */
        public static Stream NewLockingOutputStream(SystemPath file)
        {
            file = file ?? throw new ArgumentNullException(nameof(file));
            return file.ToFile().Create();
        }

        private FileOperations() { }
    }
}
