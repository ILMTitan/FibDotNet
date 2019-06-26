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
using com.google.cloud.tools.jib.docker;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Test.Common;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace com.google.cloud.tools.jib.filesystem
{
    /** Tests for {@link FileOperations}. */
    public class FileOperationsTest : IDisposable
    {
        private static void verifyWriteWithLock(SystemPath file)
        {
            using (Stream fileOutputStream = FileOperations.newLockingOutputStream(file))
            {
                try
                {
                    // Checks that the file was locked.
                    File.ReadAllText(file.toString());
                    Assert.Fail("Lock attempt should have failed");
                }
                catch (IOException)
                {
                    // pass
                }

                fileOutputStream.write("jib".getBytes(Encoding.UTF8));
            }

            Assert.AreEqual("jib", File.ReadAllText(file.toString()));
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        [Test]
        public void testCopy()
        {
            SystemPath destDir = temporaryFolder.newFolder().toPath();
            SystemPath libraryA =
                Paths.get(TestResources.getResource("core/application/dependencies/libraryA.jar").ToURI());
            SystemPath libraryB =
                Paths.get(TestResources.getResource("core/application/dependencies/libraryB.jar").ToURI());
            SystemPath dirLayer = Paths.get(TestResources.getResource("core/layer").ToURI());

            FileOperations.copy(ImmutableArray.Create(libraryA, libraryB, dirLayer), destDir);

            assertFilesEqual(libraryA, destDir.Resolve("libraryA.jar"));
            assertFilesEqual(libraryB, destDir.Resolve("libraryB.jar"));
            Assert.IsTrue(Files.exists(destDir.Resolve("layer").Resolve("a").Resolve("b")));
            Assert.IsTrue(Files.exists(destDir.Resolve("layer").Resolve("c")));
            assertFilesEqual(
                dirLayer.Resolve("a").Resolve("b").Resolve("bar"),
                destDir.Resolve("layer").Resolve("a").Resolve("b").Resolve("bar"));
            assertFilesEqual(
                dirLayer.Resolve("c").Resolve("cat"), destDir.Resolve("layer").Resolve("c").Resolve("cat"));
            assertFilesEqual(dirLayer.Resolve("foo"), destDir.Resolve("layer").Resolve("foo"));
        }

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [Test]
        public void testNewLockingOutputStream_newFile()
        {
            using (TemporaryFile file = Files.createTempFile())
            {
                // Ensures file doesn't exist.
                Files.deleteIfExists(file.Path);

                verifyWriteWithLock(file.Path);
            }
        }

        [Test]
        public void testNewLockingOutputStream_existingFile()
        {
            using (TemporaryFile file = Files.createTempFile())
            {
                // Writes out more bytes to ensure proper truncated.
                byte[] dataBytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                Files.write(file.Path, dataBytes);
                Assert.IsTrue(Files.exists(file.Path));
                Assert.AreEqual(10, Files.size(file.Path));

                verifyWriteWithLock(file.Path);
            }
        }

        private void assertFilesEqual(SystemPath file1, SystemPath file2)
        {
            CollectionAssert.AreEqual(Files.readAllBytes(file1), Files.readAllBytes(file2));
        }
    }
}
