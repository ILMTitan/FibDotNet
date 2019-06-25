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

using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.hash;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Test.Common;
using NUnit.Framework;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.tar
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
        public void setup()
        {
            testTarStreamBuilder = new TarStreamBuilder();
            // Gets the test resource files.
            fileA = Paths.get(TestResources.getResource("core/fileA").toURI());
            fileB = Paths.get(TestResources.getResource("core/fileB").toURI());
            directoryA = Paths.get(TestResources.getResource("core/directoryA").toURI());

            fileAContents = Files.readAllBytes(fileA);
            fileBContents = Files.readAllBytes(fileB);
        }

        [Test]
        public async Task testToBlob_tarArchiveEntriesAsync()
        {
            setUpWithTarEntries();
            await verifyBlobWithoutCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task testToBlob_stringsAsync()
        {
            setUpWithStrings();
            await verifyBlobWithoutCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task testToBlob_stringsAndTarArchiveEntriesAsync()
        {
            setUpWithStringsAndTarEntries();
            await verifyBlobWithoutCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task testToBlob_tarArchiveEntriesWithCompressionAsync()
        {
            setUpWithTarEntries();
            await verifyBlobWithCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task testToBlob_stringsWithCompressionAsync()
        {
            setUpWithStrings();
            await verifyBlobWithCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task testToBlob_stringsAndTarArchiveEntriesWithCompressionAsync()
        {
            setUpWithStringsAndTarEntries();
            await verifyBlobWithCompressionAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task testToBlob_multiByteAsync()
        {
            testTarStreamBuilder.addByteEntry("日本語".getBytes(StandardCharsets.UTF_8), "test");
            testTarStreamBuilder.addByteEntry("asdf".getBytes(StandardCharsets.UTF_8), "crepecake");
            testTarStreamBuilder.addBlobEntry(
                Blobs.from("jib"), "jib".getBytes(StandardCharsets.UTF_8).Length, "jib");

            // Writes the BLOB and captures the output.
            MemoryStream tarByteOutputStream = new MemoryStream();
            Stream compressorStream = new GZipStream(tarByteOutputStream, CompressionMode.Compress);
            await testTarStreamBuilder.writeAsTarArchiveToAsync(compressorStream).ConfigureAwait(false);

            // Rearrange the output into input for verification.
            MemoryStream byteArrayInputStream =
                new MemoryStream(tarByteOutputStream.toByteArray());
            Stream tarByteInputStream = new GZipStream(byteArrayInputStream, CompressionMode.Decompress);
            TarInputStream tarArchiveInputStream = new TarInputStream(tarByteInputStream);

            // Verify multi-byte characters are written/read correctly
            TarEntry headerFile = tarArchiveInputStream.getNextTarEntry();
            Assert.AreEqual("test", headerFile.getName());
            Assert.AreEqual(
                "日本語", StandardCharsets.UTF_8.GetString(ByteStreams.toByteArray(tarArchiveInputStream)));

            headerFile = tarArchiveInputStream.getNextTarEntry();
            Assert.AreEqual("crepecake", headerFile.getName());
            Assert.AreEqual(
                "asdf", StandardCharsets.UTF_8.GetString(ByteStreams.toByteArray(tarArchiveInputStream)));

            headerFile = tarArchiveInputStream.getNextTarEntry();
            Assert.AreEqual("jib", headerFile.getName());
            Assert.AreEqual(
                "jib", StandardCharsets.UTF_8.GetString(ByteStreams.toByteArray(tarArchiveInputStream)));

            Assert.IsNull(tarArchiveInputStream.getNextTarEntry());
        }

        /** Creates a TarStreamBuilder using TarArchiveEntries. */
        private void setUpWithTarEntries()
        {
            // Prepares a test TarStreamBuilder.
            testTarStreamBuilder.addTarArchiveEntry(TarStreamBuilder.CreateEntryFromFile(fileA.toFile(), "some/path/to/resourceFileA"));
            testTarStreamBuilder.addTarArchiveEntry(TarStreamBuilder.CreateEntryFromFile(fileB.toFile(), "crepecake"));
            testTarStreamBuilder.addTarArchiveEntry(
                TarStreamBuilder.CreateEntryFromFile(directoryA.toFile(), "some/path/to/"));
            testTarStreamBuilder.addTarArchiveEntry(
                TarStreamBuilder.CreateEntryFromFile(
                    fileA.toFile(),
                    "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890"));
        }

        /** Creates a TarStreamBuilder using Strings. */
        private void setUpWithStrings()
        {
            // Prepares a test TarStreamBuilder.
            testTarStreamBuilder.addByteEntry(fileAContents, "some/path/to/resourceFileA");
            testTarStreamBuilder.addByteEntry(fileBContents, "crepecake");
            testTarStreamBuilder.addTarArchiveEntry(
                TarStreamBuilder.CreateEntryFromFile(directoryA.toFile(), "some/path/to/"));
            testTarStreamBuilder.addByteEntry(
                fileAContents,
                "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890");
        }

        /** Creates a TarStreamBuilder using Strings and TarArchiveEntries. */
        private void setUpWithStringsAndTarEntries()
        {
            // Prepares a test TarStreamBuilder.
            testTarStreamBuilder.addByteEntry(fileAContents, "some/path/to/resourceFileA");
            testTarStreamBuilder.addTarArchiveEntry(TarStreamBuilder.CreateEntryFromFile(fileB.toFile(), "crepecake"));
            testTarStreamBuilder.addTarArchiveEntry(
                TarStreamBuilder.CreateEntryFromFile(directoryA.toFile(), "some/path/to/"));
            testTarStreamBuilder.addByteEntry(
                fileAContents,
                "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890");
        }

        /** Creates a compressed blob from the TarStreamBuilder and verifies it. */
        private async Task verifyBlobWithCompressionAsync()
        {
            // Writes the BLOB and captures the output.
            MemoryStream tarByteOutputStream = new MemoryStream();
            Stream compressorStream = new GZipStream(tarByteOutputStream, CompressionMode.Compress);
            await testTarStreamBuilder.writeAsTarArchiveToAsync(compressorStream).ConfigureAwait(false);

            // Rearrange the output into input for verification.
            MemoryStream byteArrayInputStream =
                new MemoryStream(tarByteOutputStream.toByteArray());
            Stream tarByteInputStream = new GZipStream(byteArrayInputStream, CompressionMode.Decompress);
            TarInputStream tarArchiveInputStream = new TarInputStream(tarByteInputStream);
            verifyTarArchive(tarArchiveInputStream);
        }

        /** Creates an uncompressed blob from the TarStreamBuilder and verifies it. */
        private async Task verifyBlobWithoutCompressionAsync()
        {
            // Writes the BLOB and captures the output.
            MemoryStream tarByteOutputStream = new MemoryStream();
            await testTarStreamBuilder.writeAsTarArchiveToAsync(tarByteOutputStream).ConfigureAwait(false);

            // Rearrange the output into input for verification.
            MemoryStream byteArrayInputStream =
                new MemoryStream(tarByteOutputStream.toByteArray());
            TarInputStream tarArchiveInputStream = new TarInputStream(byteArrayInputStream);
            verifyTarArchive(tarArchiveInputStream);
        }

        /**
         * Helper method to verify that the files were archived correctly by reading {@code
         * tarArchiveInputStream}.
         */
        private void verifyTarArchive(TarInputStream tarArchiveInputStream)
        {
            // Verifies fileA was archived correctly.
            TarEntry headerFileA = tarArchiveInputStream.getNextTarEntry();
            Assert.AreEqual("some/path/to/resourceFileA", headerFileA.getName());
            byte[] fileAString = ByteStreams.toByteArray(tarArchiveInputStream);
            CollectionAssert.AreEqual(fileAContents, fileAString);

            // Verifies fileB was archived correctly.
            TarEntry headerFileB = tarArchiveInputStream.getNextTarEntry();
            Assert.AreEqual("crepecake", headerFileB.getName());
            byte[] fileBString = ByteStreams.toByteArray(tarArchiveInputStream);
            CollectionAssert.AreEqual(fileBContents, fileBString);

            // Verifies directoryA was archived correctly.
            TarEntry headerDirectoryA = tarArchiveInputStream.getNextTarEntry();
            Assert.AreEqual("some/path/to/", headerDirectoryA.getName());

            // Verifies the long file was archived correctly.
            TarEntry headerFileALong = tarArchiveInputStream.getNextTarEntry();
            Assert.AreEqual(
                "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890",
                headerFileALong.getName());
            byte[] fileALongString = ByteStreams.toByteArray(tarArchiveInputStream);
            CollectionAssert.AreEqual(fileAContents, fileALongString);

            Assert.IsNull(tarArchiveInputStream.getNextTarEntry());
        }
    }
}
