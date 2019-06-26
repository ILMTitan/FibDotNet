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

using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Test.Common;
using NUnit.Framework;
using System;
using System.IO;

namespace com.google.cloud.tools.jib.filesystem
{
    /** Tests for {@link TemporaryDirectory}. */
    public class TemporaryDirectoryTest : IDisposable
    {
        private static void createFilesInDirectory(SystemPath directory)
        {
            SystemPath testFilesDirectory = Paths.get(TestResources.getResource("core/layer").ToURI());
            new DirectoryWalker(testFilesDirectory)
                .filterRoot()
                .walk(path =>
                {
                    if (File.Exists(path))
                    {
                        Files.copy(path, directory.Resolve(testFilesDirectory.Relativize(path)));
                    }
                    else if (Directory.Exists(path))
                    {
                        Directory.CreateDirectory(directory.Resolve(testFilesDirectory.Relativize(path)));
                    }
                });
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [Test]
        public void testClose_directoryDeleted()
        {
            using (TemporaryDirectory temporaryDirectory =
                new TemporaryDirectory(temporaryFolder.newFolder().toPath()))
            {
                createFilesInDirectory(temporaryDirectory.getDirectory());

                temporaryDirectory.close();
                Assert.IsFalse(Files.exists(temporaryDirectory.getDirectory()));
            }
        }

        [Test]
        public void testClose_directoryNotDeletedIfMoved()
        {
            SystemPath destinationParent = temporaryFolder.newFolder().toPath();

            using (TemporaryDirectory temporaryDirectory =
                new TemporaryDirectory(temporaryFolder.newFolder().toPath()))
            {
                createFilesInDirectory(temporaryDirectory.getDirectory());

                Assert.IsFalse(Files.exists(destinationParent.Resolve("destination")));
                Files.move(temporaryDirectory.getDirectory(), destinationParent.Resolve("destination"));

                temporaryDirectory.close();
                Assert.IsFalse(Files.exists(temporaryDirectory.getDirectory()));
                Assert.IsTrue(Files.exists(destinationParent.Resolve("destination")));
            }
        }
    }
}
