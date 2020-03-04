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
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace Jib.Net.Core.Unit.Tests.FileSystem
{
    /** Tests for {@link FileOperations}. */
    public class FileOperationsTest : IDisposable
    {
        private static void VerifyWriteWithLock(SystemPath file)
        {
            using (Stream fileOutputStream = FileOperations.NewLockingOutputStream(file))
            {
                try
                {
                    // Checks that the file was locked.
                    File.ReadAllText(JavaExtensions.ToString(file));
                    Assert.Fail("Lock attempt should have failed");
                }
                catch (IOException)
                {
                    // pass
                }

                JavaExtensions.Write(fileOutputStream, Encoding.UTF8.GetBytes("jib"));
            }

            Assert.AreEqual("jib", File.ReadAllText(JavaExtensions.ToString(file)));
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        [Test]
        public void TestCopy()
        {
            SystemPath destDir = temporaryFolder.NewFolder().ToPath();
            SystemPath libraryA =
                Paths.Get(TestResources.GetResource("core/application/dependencies/libraryA.jar").ToURI());
            SystemPath libraryB =
                Paths.Get(TestResources.GetResource("core/application/dependencies/libraryB.jar").ToURI());
            SystemPath dirLayer = Paths.Get(TestResources.GetResource("core/layer").ToURI());

            FileOperations.Copy(ImmutableArray.Create(libraryA, libraryB, dirLayer), destDir);

            AssertFilesEqual(libraryA, destDir.Resolve("libraryA.jar"));
            AssertFilesEqual(libraryB, destDir.Resolve("libraryB.jar"));
            Assert.IsTrue(Files.Exists(destDir.Resolve("layer").Resolve("a").Resolve("b")));
            Assert.IsTrue(Files.Exists(destDir.Resolve("layer").Resolve("c")));
            AssertFilesEqual(
                dirLayer.Resolve("a").Resolve("b").Resolve("bar"),
                destDir.Resolve("layer").Resolve("a").Resolve("b").Resolve("bar"));
            AssertFilesEqual(
                dirLayer.Resolve("c").Resolve("cat"), destDir.Resolve("layer").Resolve("c").Resolve("cat"));
            AssertFilesEqual(dirLayer.Resolve("foo"), destDir.Resolve("layer").Resolve("foo"));
        }

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [Test]
        public void TestNewLockingOutputStream_newFile()
        {
            using (TemporaryFile file = Files.CreateTempFile())
            {
                // Ensures file doesn't exist.
                Files.DeleteIfExists(file.Path);

                VerifyWriteWithLock(file.Path);
            }
        }

        [Test]
        public void TestNewLockingOutputStream_existingFile()
        {
            using (TemporaryFile file = Files.CreateTempFile())
            {
                // Writes out more bytes to ensure proper truncated.
                byte[] dataBytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                Files.Write(file.Path, dataBytes);
                Assert.IsTrue(Files.Exists(file.Path));
                Assert.AreEqual(10, Files.Size(file.Path));

                VerifyWriteWithLock(file.Path);
            }
        }

        private void AssertFilesEqual(SystemPath file1, SystemPath file2)
        {
            CollectionAssert.AreEqual(Files.ReadAllBytes(file1), Files.ReadAllBytes(file2));
        }
    }
}
