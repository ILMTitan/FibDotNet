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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.docker;
using ICSharpCode.SharpZipLib.Tar;
using Jib.Net.Core;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images;
using Jib.Net.Test.Common;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace com.google.cloud.tools.jib.image
{
    /** Tests for {@link ReproducibleLayerBuilder}. */
    public class ReproducibleLayerBuilderTest : IDisposable
    {
        /**
         * Verifies the correctness of the next {@link TarArchiveEntry} in the {@link
         * TarArchiveInputStream}.
         *
         * @param tarArchiveInputStream the {@link TarArchiveInputStream} to read from
         * @param expectedExtractionPath the expected extraction path of the next entry
         * @param expectedFile the file to match against the contents of the next entry
         * @throws IOException if an I/O exception occurs
         */
        private static void VerifyNextTarArchiveEntry(
            TarInputStream tarArchiveInputStream, string expectedExtractionPath, SystemPath expectedFile)
        {
            TarEntry header = tarArchiveInputStream.GetNextTarEntry();
            Assert.AreEqual(expectedExtractionPath, header.GetName());

            string expectedString = Encoding.UTF8.GetString(Files.ReadAllBytes(expectedFile));

            string extractedString =
                CharStreams.ToString(new StreamReader(tarArchiveInputStream, Encoding.UTF8));

            Assert.AreEqual(expectedString, extractedString);
        }

        /**
         * Verifies that the next {@link TarArchiveEntry} in the {@link TarArchiveInputStream} is a
         * directory with correct permissions.
         *
         * @param tarArchiveInputStream the {@link TarArchiveInputStream} to read from
         * @param expectedExtractionPath the expected extraction path of the next entry
         * @throws IOException if an I/O exception occurs
         */
        private static void VerifyNextTarArchiveEntryIsDirectory(
            TarInputStream tarArchiveInputStream, string expectedExtractionPath)
        {
            TarEntry extractionPathEntry = tarArchiveInputStream.GetNextTarEntry();
            Assert.AreEqual(expectedExtractionPath, extractionPathEntry.GetName());
            Assert.IsTrue(extractionPathEntry.IsDirectory());
        }

        private static LayerEntry DefaultLayerEntry(SystemPath source, AbsoluteUnixPath destination)
        {
            return new LayerEntry(
                source,
                destination,
                LayerConfiguration.DefaultFilePermissionsProvider.Apply(source, destination),
                LayerConfiguration.DefaultModifiedTime);
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [Test]
        public async System.Threading.Tasks.Task TestBuildAsync()
        {
            SystemPath layerDirectory = Paths.Get(TestResources.GetResource("core/layer").ToURI());
            SystemPath blobA = Paths.Get(TestResources.GetResource("core/blobA").ToURI());

            ReproducibleLayerBuilder layerBuilder =
                new ReproducibleLayerBuilder(
                    LayerConfiguration.builder()
                        .AddEntryRecursive(
                            layerDirectory, AbsoluteUnixPath.Get("/extract/here/apple/layer"))
                        .AddEntry(blobA, AbsoluteUnixPath.Get("/extract/here/apple/blobA"))
                        .AddEntry(blobA, AbsoluteUnixPath.Get("/extract/here/banana/blobA"))
                        .Build()
                        .GetLayerEntries());

            // Writes the layer tar to a temporary file.
            IBlob unwrittenBlob = layerBuilder.Build();
            SystemPath temporaryFile = temporaryFolder.NewFile().ToPath();
            using (Stream temporaryFileOutputStream =
                new BufferedStream(Files.NewOutputStream(temporaryFile)))
            {
                await unwrittenBlob.WriteToAsync(temporaryFileOutputStream).ConfigureAwait(false);
            }

            // Reads the file back.
            using (TarInputStream tarArchiveInputStream =
                new TarInputStream(Files.NewInputStream(temporaryFile)))
            {
                VerifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/");
                VerifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/");
                VerifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/apple/");
                VerifyNextTarArchiveEntry(tarArchiveInputStream, "extract/here/apple/blobA", blobA);
                VerifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/apple/layer/");
                VerifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/apple/layer/a/");
                VerifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/apple/layer/a/b/");
                VerifyNextTarArchiveEntry(
                    tarArchiveInputStream,
                    "extract/here/apple/layer/a/b/bar",
                    Paths.Get(TestResources.GetResource("core/layer/a/b/bar").ToURI()));
                VerifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/apple/layer/c/");
                VerifyNextTarArchiveEntry(
                    tarArchiveInputStream,
                    "extract/here/apple/layer/c/cat",
                    Paths.Get(TestResources.GetResource("core/layer/c/cat").ToURI()));
                VerifyNextTarArchiveEntry(
                    tarArchiveInputStream,
                    "extract/here/apple/layer/foo",
                    Paths.Get(TestResources.GetResource("core/layer/foo").ToURI()));
                VerifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/banana/");
                VerifyNextTarArchiveEntry(tarArchiveInputStream, "extract/here/banana/blobA", blobA);
            }
        }

        [Test]
        public async System.Threading.Tasks.Task TestToBlob_reproducibilityAsync()
        {
            SystemPath testRoot = temporaryFolder.GetRoot().ToPath();
            SystemPath root1 = Files.CreateDirectories(testRoot.Resolve("files1"));
            SystemPath root2 = Files.CreateDirectories(testRoot.Resolve("files2"));

            // TODO: Currently this test only covers variation in order and modified time, even though
            // TODO: the code is designed to clean up userid/groupid, this test does not check that yet.
            const string contentA = "abcabc";
            SystemPath fileA1 = CreateFile(root1, "fileA", contentA, 10000);
            SystemPath fileA2 = CreateFile(root2, "fileA", contentA, 20000);
            const string contentB = "yumyum";
            SystemPath fileB1 = CreateFile(root1, "fileB", contentB, 10000);
            SystemPath fileB2 = CreateFile(root2, "fileB", contentB, 20000);

            // check if modified times are off
            Assert.AreNotEqual(Files.GetLastModifiedTime(fileA1), Files.GetLastModifiedTime(fileA2));
            Assert.AreNotEqual(Files.GetLastModifiedTime(fileB1), Files.GetLastModifiedTime(fileB2));

            // create layers of exact same content but ordered differently and with different timestamps
            IBlob layer =
                new ReproducibleLayerBuilder(
                        ImmutableArray.Create(
                            DefaultLayerEntry(fileA1, AbsoluteUnixPath.Get("/somewhere/fileA")),
                            DefaultLayerEntry(fileB1, AbsoluteUnixPath.Get("/somewhere/fileB"))))
                    .Build();
            IBlob reproduced =
                new ReproducibleLayerBuilder(
                        ImmutableArray.Create(
                            DefaultLayerEntry(fileB2, AbsoluteUnixPath.Get("/somewhere/fileB")),
                            DefaultLayerEntry(fileA2, AbsoluteUnixPath.Get("/somewhere/fileA"))))
                    .Build();

            byte[] layerContent = await Blobs.WriteToByteArrayAsync(layer).ConfigureAwait(false);
            byte[] reproducedLayerContent = await Blobs.WriteToByteArrayAsync(reproduced).ConfigureAwait(false);

            Assert.AreEqual(layerContent, reproducedLayerContent);
        }

        [Test]
        public async System.Threading.Tasks.Task TestBuild_parentDirBehaviorAsync()
        {
            SystemPath testRoot = temporaryFolder.GetRoot().ToPath();

            // the path doesn't really matter on source files, but these are structured
            SystemPath parent = Files.CreateDirectories(testRoot.Resolve("aaa"));
            SystemPath fileA = Files.CreateFile(parent.Resolve("fileA"));
            SystemPath ignoredParent = Files.CreateDirectories(testRoot.Resolve("bbb-ignored"));
            SystemPath fileB = Files.CreateFile(ignoredParent.Resolve("fileB"));
            SystemPath fileC =
                Files.CreateFile(Files.CreateDirectories(testRoot.Resolve("ccc-absent")).Resolve("fileC"));

            IBlob layer =
                new ReproducibleLayerBuilder(
                        ImmutableArray.Create(
                            new LayerEntry(
                                parent,
                                AbsoluteUnixPath.Get("/root/aaa"),
                                FilePermissions.FromOctalString("111"),
                                Instant.FromUnixTimeSeconds(10)),
                            new LayerEntry(
                                fileA,
                                AbsoluteUnixPath.Get("/root/aaa/fileA"),
                                FilePermissions.FromOctalString("222"),
                                Instant.FromUnixTimeSeconds(20)),
                            new LayerEntry(
                                fileB,
                                AbsoluteUnixPath.Get("/root/bbb-ignored/fileB"),
                                FilePermissions.FromOctalString("333"),
                                Instant.FromUnixTimeSeconds(30)),
                            new LayerEntry(
                                ignoredParent,
                                AbsoluteUnixPath.Get("/root/bbb-ignored"),
                                FilePermissions.FromOctalString("444"),
                                Instant.FromUnixTimeSeconds(40)),
                            new LayerEntry(
                                fileC,
                                AbsoluteUnixPath.Get("/root/ccc-absent/file3"),
                                FilePermissions.FromOctalString("555"),
                                Instant.FromUnixTimeSeconds(50))))
                    .Build();

            SystemPath tarFile = temporaryFolder.NewFile().ToPath();
            using (Stream @out = new BufferedStream(Files.NewOutputStream(tarFile)))
            {
                await layer.WriteToAsync(@out).ConfigureAwait(false);
            }

            using (TarInputStream @in = new TarInputStream(Files.NewInputStream(tarFile)))
            {
                // root (default folder permissions)
                TarEntry root = @in.GetNextTarEntry();
                Assert.AreEqual("755", root.GetMode().ToOctalString());
                Assert.AreEqual(Instant.FromUnixTimeSeconds(1), root.GetModTime().ToInstant());

                // parentAAA (custom permissions, custom timestamp)
                TarEntry rootParentAAA = @in.GetNextTarEntry();
                Assert.AreEqual("111", rootParentAAA.GetMode().ToOctalString());
                Assert.AreEqual(Instant.FromUnixTimeSeconds(10), rootParentAAA.GetModTime().ToInstant());

                // skip over fileA
                @in.GetNextTarEntry();

                // parentBBB (default permissions - ignored custom permissions, since fileB added first)
                TarEntry rootParentBBB = @in.GetNextTarEntry();
                // TODO (#1650): we want 040444 here.
                Assert.AreEqual("755", rootParentBBB.GetMode().ToOctalString());
                // TODO (#1650): we want Instant.ofEpochSecond(40) here.
                Assert.AreEqual(Instant.FromUnixTimeSeconds(1), root.GetModTime().ToInstant());

                // skip over fileB
                @in.GetNextTarEntry();

                // parentCCC (default permissions - no entry provided)
                TarEntry rootParentCCC = @in.GetNextTarEntry();
                Assert.AreEqual("755", rootParentCCC.GetMode().ToOctalString());
                Assert.AreEqual(Instant.FromUnixTimeSeconds(1), root.GetModTime().ToInstant());

                // we don't care about fileC
            }
        }

        [Test]
        public async System.Threading.Tasks.Task TestBuild_timestampDefaultAsync()
        {
            SystemPath file = CreateFile(temporaryFolder.GetRoot().ToPath(), "fileA", "some content", 54321);

            IBlob blob =
                new ReproducibleLayerBuilder(
                        ImmutableArray.Create(DefaultLayerEntry(file, AbsoluteUnixPath.Get("/fileA"))))
                    .Build();

            SystemPath tarFile = temporaryFolder.NewFile().ToPath();
            using (Stream @out = new BufferedStream(Files.NewOutputStream(tarFile)))
            {
                await blob.WriteToAsync(@out).ConfigureAwait(false);
            }

            // Reads the file back.
            using (TarInputStream @in = new TarInputStream(Files.NewInputStream(tarFile)))
            {
                Assert.AreEqual(
                    Instant.FromUnixTimeSeconds(0).PlusSeconds(1).ToDateTimeUtc(), JavaExtensions.GetNextEntry(@in).GetLastModifiedDate());
            }
        }

        [Test]
        public async System.Threading.Tasks.Task TestBuild_timestampNonDefaultAsync()
        {
            SystemPath file = CreateFile(temporaryFolder.GetRoot().ToPath(), "fileA", "some content", 54321);

            IBlob blob =
                new ReproducibleLayerBuilder(
                        ImmutableArray.Create(
                            new LayerEntry(
                                file,
                                AbsoluteUnixPath.Get("/fileA"),
                                FilePermissions.DefaultFilePermissions,
                                Instant.FromUnixTimeSeconds(123))))
                    .Build();

            SystemPath tarFile = temporaryFolder.NewFile().ToPath();
            using (Stream @out = new BufferedStream(Files.NewOutputStream(tarFile)))
            {
                await blob.WriteToAsync(@out).ConfigureAwait(false);
            }

            // Reads the file back.
            using (TarInputStream @in = new TarInputStream(Files.NewInputStream(tarFile)))
            {
                Assert.AreEqual(
                    Instant.FromUnixTimeSeconds(0).PlusSeconds(123).ToDateTimeUtc(),
                    JavaExtensions.GetNextEntry(@in).GetLastModifiedDate());
            }
        }

        [Test]
        public async System.Threading.Tasks.Task TestBuild_permissionsAsync()
        {
            SystemPath testRoot = temporaryFolder.GetRoot().ToPath();
            SystemPath folder = Files.CreateDirectories(testRoot.Resolve("files1"));
            SystemPath fileA = CreateFile(testRoot, "fileA", "abc", 54321);
            SystemPath fileB = CreateFile(testRoot, "fileB", "def", 54321);

            IBlob blob =
                new ReproducibleLayerBuilder(
                        ImmutableArray.Create(
                            DefaultLayerEntry(fileA, AbsoluteUnixPath.Get("/somewhere/fileA")),
                            new LayerEntry(
                                fileB,
                                AbsoluteUnixPath.Get("/somewhere/fileB"),
                                FilePermissions.FromOctalString("123"),
                                LayerConfiguration.DefaultModifiedTime),
                            new LayerEntry(
                                folder,
                                AbsoluteUnixPath.Get("/somewhere/folder"),
                                FilePermissions.FromOctalString("456"),
                                LayerConfiguration.DefaultModifiedTime)))
                    .Build();

            SystemPath tarFile = temporaryFolder.NewFile().ToPath();
            using (Stream @out = new BufferedStream(Files.NewOutputStream(tarFile)))
            {
                await blob.WriteToAsync(@out).ConfigureAwait(false);
            }

            using (TarInputStream @in = new TarInputStream(Files.NewInputStream(tarFile)))
            {
                // Root folder (default folder permissions)
                TarEntry rootEntry = @in.GetNextTarEntry();
                // fileA (default file permissions)
                TarEntry fileAEntry = @in.GetNextTarEntry();
                // fileB (custom file permissions)
                TarEntry fileBEntry = @in.GetNextTarEntry();
                // folder (custom folder permissions)
                TarEntry folderEntry = @in.GetNextTarEntry();
                Assert.AreEqual("755", rootEntry.GetMode().ToOctalString());
                Assert.AreEqual("644", fileAEntry.GetMode().ToOctalString());
                Assert.AreEqual("123", fileBEntry.GetMode().ToOctalString());
                Assert.AreEqual("456", folderEntry.GetMode().ToOctalString());
            }
        }

        private SystemPath CreateFile(SystemPath root, string filename, string content, long lastModifiedTime)
        {
            SystemPath newFile =
                Files.Write(
                    root.Resolve(filename),
                    content.GetBytes(Encoding.UTF8));
            Files.SetLastModifiedTime(newFile, FileTime.FromMillis(lastModifiedTime));
            return newFile;
        }
    }
}
