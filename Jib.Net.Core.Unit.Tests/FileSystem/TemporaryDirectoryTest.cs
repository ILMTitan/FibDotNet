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
using Jib.Net.Core.Global;
using Jib.Net.Test.Common;
using NUnit.Framework;
using System;
using System.IO;

namespace Jib.Net.Core.Unit.Tests.FileSystem
{
    /** Tests for {@link TemporaryDirectory}. */
    public class TemporaryDirectoryTest : IDisposable
    {
        private static void CreateFilesInDirectory(SystemPath directory)
        {
            SystemPath testFilesDirectory = Paths.Get(TestResources.GetResource("core/layer").ToURI());
            new DirectoryWalker(testFilesDirectory)
                .FilterRoot()
                .Walk(path =>
                {
                    if (File.Exists(path))
                    {
                        Files.Copy(path, directory.Resolve(testFilesDirectory.Relativize(path)));
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
        public void TestClose_directoryDeleted()
        {
            using (TemporaryDirectory temporaryDirectory =
                new TemporaryDirectory(temporaryFolder.NewFolder().ToPath()))
            {
                CreateFilesInDirectory(temporaryDirectory.GetDirectory());

                temporaryDirectory.Dispose();
                Assert.IsFalse(Files.Exists(temporaryDirectory.GetDirectory()));
            }
        }

        [Test]
        public void TestClose_directoryNotDeletedIfMoved()
        {
            SystemPath destinationParent = temporaryFolder.NewFolder().ToPath();

            using (TemporaryDirectory temporaryDirectory =
                new TemporaryDirectory(temporaryFolder.NewFolder().ToPath()))
            {
                CreateFilesInDirectory(temporaryDirectory.GetDirectory());

                Assert.IsFalse(Files.Exists(destinationParent.Resolve("destination")));
                Files.Move(temporaryDirectory.GetDirectory(), destinationParent.Resolve("destination"));

                temporaryDirectory.Dispose();
                Assert.IsFalse(Files.Exists(temporaryDirectory.GetDirectory()));
                Assert.IsTrue(Files.Exists(destinationParent.Resolve("destination")));
            }
        }
    }
}
