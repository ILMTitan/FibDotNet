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

using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace com.google.cloud.tools.jib.filesystem
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
        public static void copy(ImmutableArray<SystemPath> sourceFiles, SystemPath destDir)
        {
            foreach (SystemPath sourceFile in sourceFiles)
            {
                PathConsumer copyPathConsumer =
                    path =>
                    {
              // Creates the same path in the destDir.
              SystemPath destPath = destDir.resolve(sourceFile.getParent().relativize(path));
                        if (Files.isDirectory(path))
                        {
                            Files.createDirectories(destPath);
                        }
                        else
                        {
                            Files.copy(path, destPath);
                        }
                    };

                if (Files.isDirectory(sourceFile))
                {
                    new DirectoryWalker(sourceFile).walk(copyPathConsumer);
                }
                else
                {
                    copyPathConsumer.accept(sourceFile);
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
        public static Stream newLockingOutputStream(SystemPath file)
        {
            return file.toFile().Create();
        }

        private FileOperations() { }
    }
}
