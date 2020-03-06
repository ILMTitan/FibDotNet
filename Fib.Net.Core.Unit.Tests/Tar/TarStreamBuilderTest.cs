/*
 * Copyright 2017 Google LLC.
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

using ICSharpCode.SharpZipLib.Tar;
using Fib.Net.Core.Api;
using Fib.Net.Core.Blob;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Hash;
using Fib.Net.Core.Tar;
using Fib.Net.Test.Common;
using NUnit.Framework;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Fib.Net.Core.Unit.Tests.Tar
{
    /** Tests for {@link TarStreamBuilder}. */
    public class TarStreamBuilderTest
    {
        private SystemPath fileA;
        private SystemPath fileB;
        private SystemPath directoryA;
        private byte[] fileAContents;
        private byte[] fileBContents;
        private TarStreamBuilder testTarStreamBuilder;

        [SetUp]
        public void Setup()
        {
            testTarStreamBuilder = new TarStreamBuilder();
            // Gets the test resource files.
            fileA = Paths.Get(TestResources.GetResource("core/fileA").ToURI());
            fileB = Paths.Get(TestResources.GetResource("core/fileB").ToURI());
            directoryA = Paths.Get(TestResources.GetResource("core/directoryA").ToURI());

            fileAContents = Files.ReadAllBytes(fileA);
            fileBContents = Files.ReadAllBytes(fileB);
        }

        [Test]
        public async Task TestToBlob_tarArchiveEntriesAsync()
        {
            SetUpWithTarEntries();
            await VerifyBlobWithoutCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task TestToBlob_stringsAsync()
        {
            SetUpWithStrings();
            await VerifyBlobWithoutCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task TestToBlob_stringsAndTarArchiveEntriesAsync()
        {
            SetUpWithStringsAndTarEntries();
            await VerifyBlobWithoutCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task TestToBlob_tarArchiveEntriesWithCompressionAsync()
        {
            SetUpWithTarEntries();
            await VerifyBlobWithCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task TestToBlob_stringsWithCompressionAsync()
        {
            SetUpWithStrings();
            await VerifyBlobWithCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task TestToBlob_stringsAndTarArchiveEntriesWithCompressionAsync()
        {
            SetUpWithStringsAndTarEntries();
            await VerifyBlobWithCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task TestToBlob_multiByteAsync()
        {
            testTarStreamBuilder.AddByteEntry(Encoding.UTF8.GetBytes("日本語"), "test");
            testTarStreamBuilder.AddByteEntry(Encoding.UTF8.GetBytes("asdf"), "crepecake");
            testTarStreamBuilder.AddBlobEntry(
                Blobs.From("fib"), Encoding.UTF8.GetBytes("fib").Length, "fib");

            // Writes the BLOB and captures the output.
            MemoryStream tarByteOutputStream = new MemoryStream();
            using (Stream compressorStream = new GZipStream(tarByteOutputStream, CompressionMode.Compress))
            {
                await testTarStreamBuilder.WriteAsTarArchiveToAsync(compressorStream).ConfigureAwait(false);
            }

            // Rearrange the output into input for verification.
            MemoryStream byteArrayInputStream =
                new MemoryStream(tarByteOutputStream.ToArray());
            Stream tarByteInputStream = new GZipStream(byteArrayInputStream, CompressionMode.Decompress);
            using (TarInputStream tarArchiveInputStream = new TarInputStream(tarByteInputStream))
            {
                // Verify multi-byte characters are written/read correctly
                TarEntry headerFile = tarArchiveInputStream.GetNextEntry();
                Assert.AreEqual("test", headerFile.Name);
                Assert.AreEqual(
                    "日本語", Encoding.UTF8.GetString(ByteStreams.ToByteArray(tarArchiveInputStream)));

                headerFile = tarArchiveInputStream.GetNextEntry();
                Assert.AreEqual("crepecake", headerFile.Name);
                Assert.AreEqual(
                    "asdf", Encoding.UTF8.GetString(ByteStreams.ToByteArray(tarArchiveInputStream)));

                headerFile = tarArchiveInputStream.GetNextEntry();
                Assert.AreEqual("fib", headerFile.Name);
                Assert.AreEqual(
                    "fib", Encoding.UTF8.GetString(ByteStreams.ToByteArray(tarArchiveInputStream)));

                Assert.IsNull(tarArchiveInputStream.GetNextEntry());
            }
        }

        /** Creates a TarStreamBuilder using TarArchiveEntries. */
        private void SetUpWithTarEntries()
        {
            // Prepares a test TarStreamBuilder.
            testTarStreamBuilder.AddTarArchiveEntry(TarStreamBuilder.CreateEntryFromFile(fileA.ToFile(), "some/path/to/resourceFileA"));
            testTarStreamBuilder.AddTarArchiveEntry(TarStreamBuilder.CreateEntryFromFile(fileB.ToFile(), "crepecake"));
            testTarStreamBuilder.AddTarArchiveEntry(
                TarStreamBuilder.CreateEntryFromFile(directoryA.ToFile(), "some/path/to/"));
            testTarStreamBuilder.AddTarArchiveEntry(
                TarStreamBuilder.CreateEntryFromFile(
                    fileA.ToFile(),
                    "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890"));
        }

        /** Creates a TarStreamBuilder using Strings. */
        private void SetUpWithStrings()
        {
            // Prepares a test TarStreamBuilder.
            testTarStreamBuilder.AddByteEntry(fileAContents, "some/path/to/resourceFileA");
            testTarStreamBuilder.AddByteEntry(fileBContents, "crepecake");
            testTarStreamBuilder.AddTarArchiveEntry(
                TarStreamBuilder.CreateEntryFromFile(directoryA.ToFile(), "some/path/to/"));
            testTarStreamBuilder.AddByteEntry(
                fileAContents,
                "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890");
        }

        /** Creates a TarStreamBuilder using Strings and TarArchiveEntries. */
        private void SetUpWithStringsAndTarEntries()
        {
            // Prepares a test TarStreamBuilder.
            testTarStreamBuilder.AddByteEntry(fileAContents, "some/path/to/resourceFileA");
            testTarStreamBuilder.AddTarArchiveEntry(TarStreamBuilder.CreateEntryFromFile(fileB.ToFile(), "crepecake"));
            testTarStreamBuilder.AddTarArchiveEntry(
                TarStreamBuilder.CreateEntryFromFile(directoryA.ToFile(), "some/path/to/"));
            testTarStreamBuilder.AddByteEntry(
                fileAContents,
                "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890");
        }

        /** Creates a compressed blob from the TarStreamBuilder and verifies it. */
        private async Task VerifyBlobWithCompressionAsync()
        {
            // Writes the BLOB and captures the output.
            MemoryStream tarByteOutputStream = new MemoryStream();
            using (Stream compressorStream = new GZipStream(tarByteOutputStream, CompressionMode.Compress))
            {
                await testTarStreamBuilder.WriteAsTarArchiveToAsync(compressorStream).ConfigureAwait(false);
            }

            // Rearrange the output into input for verification.
            MemoryStream byteArrayInputStream =
                new MemoryStream(tarByteOutputStream.ToArray());
            Stream tarByteInputStream = new GZipStream(byteArrayInputStream, CompressionMode.Decompress);
            using (TarInputStream tarArchiveInputStream = new TarInputStream(tarByteInputStream))
            {
                VerifyTarArchive(tarArchiveInputStream);
            }
        }

        /** Creates an uncompressed blob from the TarStreamBuilder and verifies it. */
        private async Task VerifyBlobWithoutCompressionAsync()
        {
            // Writes the BLOB and captures the output.
            using (MemoryStream tarByteOutputStream = new MemoryStream())
            {
                await testTarStreamBuilder.WriteAsTarArchiveToAsync(tarByteOutputStream).ConfigureAwait(false);

                // Rearrange the output into input for verification.
                MemoryStream byteArrayInputStream =
                    new MemoryStream(tarByteOutputStream.ToArray());
                using (TarInputStream tarArchiveInputStream = new TarInputStream(byteArrayInputStream))
                {
                    VerifyTarArchive(tarArchiveInputStream);
                }
            }
        }

        /**
         * Helper method to verify that the files were archived correctly by reading {@code
         * tarArchiveInputStream}.
         */
        private void VerifyTarArchive(TarInputStream tarArchiveInputStream)
        {
            // Verifies fileA was archived correctly.
            TarEntry headerFileA = tarArchiveInputStream.GetNextEntry();
            Assert.AreEqual("some/path/to/resourceFileA", headerFileA.Name);
            byte[] fileAString = ByteStreams.ToByteArray(tarArchiveInputStream);
            CollectionAssert.AreEqual(fileAContents, fileAString);

            // Verifies fileB was archived correctly.
            TarEntry headerFileB = tarArchiveInputStream.GetNextEntry();
            Assert.AreEqual("crepecake", headerFileB.Name);
            byte[] fileBString = ByteStreams.ToByteArray(tarArchiveInputStream);
            CollectionAssert.AreEqual(fileBContents, fileBString);

            // Verifies directoryA was archived correctly.
            TarEntry headerDirectoryA = tarArchiveInputStream.GetNextEntry();
            Assert.AreEqual("some/path/to/", headerDirectoryA.Name);

            // Verifies the long file was archived correctly.
            TarEntry headerFileALong = tarArchiveInputStream.GetNextEntry();
            Assert.AreEqual(
                "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890",
                headerFileALong.Name);
            byte[] fileALongString = ByteStreams.ToByteArray(tarArchiveInputStream);
            CollectionAssert.AreEqual(fileAContents, fileALongString);

            Assert.IsNull(tarArchiveInputStream.GetNextEntry());
        }
    }
}
