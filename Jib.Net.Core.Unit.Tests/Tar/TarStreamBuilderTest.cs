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

namespace com.google.cloud.tools.jib.tar {





















/** Tests for {@link TarStreamBuilder}. */
public class TarStreamBuilderTest {

  private SystemPath fileA;
  private SystemPath fileB;
  private SystemPath directoryA;
  private byte[] fileAContents;
  private byte[] fileBContents;
  private TarStreamBuilder testTarStreamBuilder = new TarStreamBuilder();

  [TestInitialize]
  public void setup() {
    // Gets the test resource files.
    fileA = Paths.get(Resources.getResource("core/fileA").toURI());
    fileB = Paths.get(Resources.getResource("core/fileB").toURI());
    directoryA = Paths.get(Resources.getResource("core/directoryA").toURI());

    fileAContents = Files.readAllBytes(fileA);
    fileBContents = Files.readAllBytes(fileB);
  }

  [TestMethod]
  public void testToBlob_tarArchiveEntries() {
    setUpWithTarEntries();
    verifyBlobWithoutCompression();
  }

  [TestMethod]
  public void testToBlob_strings() {
    setUpWithStrings();
    verifyBlobWithoutCompression();
  }

  [TestMethod]
  public void testToBlob_stringsAndTarArchiveEntries() {
    setUpWithStringsAndTarEntries();
    verifyBlobWithoutCompression();
  }

  [TestMethod]
  public void testToBlob_tarArchiveEntriesWithCompression() {
    setUpWithTarEntries();
    verifyBlobWithCompression();
  }

  [TestMethod]
  public void testToBlob_stringsWithCompression() {
    setUpWithStrings();
    verifyBlobWithCompression();
  }

  [TestMethod]
  public void testToBlob_stringsAndTarArchiveEntriesWithCompression() {
    setUpWithStringsAndTarEntries();
    verifyBlobWithCompression();
  }

  [TestMethod]
  public void testToBlob_multiByte() {
    testTarStreamBuilder.addByteEntry("日本語".getBytes(StandardCharsets.UTF_8), "test");
    testTarStreamBuilder.addByteEntry("asdf".getBytes(StandardCharsets.UTF_8), "crepecake");
    testTarStreamBuilder.addBlobEntry(
        Blobs.from("jib"), "jib".getBytes(StandardCharsets.UTF_8).length, "jib");

    // Writes the BLOB and captures the output.
    MemoryStream tarByteOutputStream = new MemoryStream();
    Stream compressorStream = new GZipOutputStream(tarByteOutputStream);
    testTarStreamBuilder.writeAsTarArchiveTo(compressorStream);

    // Rearrange the output into input for verification.
    ByteArrayInputStream byteArrayInputStream =
        new MemoryStream(tarByteOutputStream.toByteArray());
    Stream tarByteInputStream = new GZIPInputStream(byteArrayInputStream);
    TarArchiveInputStream tarArchiveInputStream = new TarArchiveInputStream(tarByteInputStream);

    // Verify multi-byte characters are written/read correctly
    TarEntry headerFile = tarArchiveInputStream.getNextTarEntry();
    Assert.assertEquals("test", headerFile.getName());
    Assert.assertEquals(
        "日本語", new string(ByteStreams.toByteArray(tarArchiveInputStream), StandardCharsets.UTF_8));

    headerFile = tarArchiveInputStream.getNextTarEntry();
    Assert.assertEquals("crepecake", headerFile.getName());
    Assert.assertEquals(
        "asdf", new string(ByteStreams.toByteArray(tarArchiveInputStream), StandardCharsets.UTF_8));

    headerFile = tarArchiveInputStream.getNextTarEntry();
    Assert.assertEquals("jib", headerFile.getName());
    Assert.assertEquals(
        "jib", new string(ByteStreams.toByteArray(tarArchiveInputStream), StandardCharsets.UTF_8));

    Assert.assertNull(tarArchiveInputStream.getNextTarEntry());
  }

  /** Creates a TarStreamBuilder using TarArchiveEntries. */
  private void setUpWithTarEntries() {
    // Prepares a test TarStreamBuilder.
    testTarStreamBuilder.addTarArchiveEntry(
        new TarEntry(fileA.toFile(), "some/path/to/resourceFileA"));
    testTarStreamBuilder.addTarArchiveEntry(new TarEntry(fileB.toFile(), "crepecake"));
    testTarStreamBuilder.addTarArchiveEntry(
        new TarEntry(directoryA.toFile(), "some/path/to"));
    testTarStreamBuilder.addTarArchiveEntry(
        new TarEntry(
            fileA.toFile(),
            "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890"));
  }

  /** Creates a TarStreamBuilder using Strings. */
  private void setUpWithStrings() {
    // Prepares a test TarStreamBuilder.
    testTarStreamBuilder.addByteEntry(fileAContents, "some/path/to/resourceFileA");
    testTarStreamBuilder.addByteEntry(fileBContents, "crepecake");
    testTarStreamBuilder.addTarArchiveEntry(
        new TarEntry(directoryA.toFile(), "some/path/to"));
    testTarStreamBuilder.addByteEntry(
        fileAContents,
        "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890");
  }

  /** Creates a TarStreamBuilder using Strings and TarArchiveEntries. */
  private void setUpWithStringsAndTarEntries() {
    // Prepares a test TarStreamBuilder.
    testTarStreamBuilder.addByteEntry(fileAContents, "some/path/to/resourceFileA");
    testTarStreamBuilder.addTarArchiveEntry(new TarEntry(fileB.toFile(), "crepecake"));
    testTarStreamBuilder.addTarArchiveEntry(
        new TarEntry(directoryA.toFile(), "some/path/to"));
    testTarStreamBuilder.addByteEntry(
        fileAContents,
        "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890");
  }

  /** Creates a compressed blob from the TarStreamBuilder and verifies it. */
  private void verifyBlobWithCompression() {
    // Writes the BLOB and captures the output.
    MemoryStream tarByteOutputStream = new MemoryStream();
    Stream compressorStream = new GZipOutputStream(tarByteOutputStream);
    testTarStreamBuilder.writeAsTarArchiveTo(compressorStream);

    // Rearrange the output into input for verification.
    ByteArrayInputStream byteArrayInputStream =
        new MemoryStream(tarByteOutputStream.toByteArray());
    Stream tarByteInputStream = new GZIPInputStream(byteArrayInputStream);
    TarArchiveInputStream tarArchiveInputStream = new TarArchiveInputStream(tarByteInputStream);
    verifyTarArchive(tarArchiveInputStream);
  }

  /** Creates an uncompressed blob from the TarStreamBuilder and verifies it. */
  private void verifyBlobWithoutCompression() {
    // Writes the BLOB and captures the output.
    MemoryStream tarByteOutputStream = new MemoryStream();
    testTarStreamBuilder.writeAsTarArchiveTo(tarByteOutputStream);

    // Rearrange the output into input for verification.
    ByteArrayInputStream byteArrayInputStream =
        new MemoryStream(tarByteOutputStream.toByteArray());
    TarArchiveInputStream tarArchiveInputStream = new TarArchiveInputStream(byteArrayInputStream);
    verifyTarArchive(tarArchiveInputStream);
  }

  /**
   * Helper method to verify that the files were archived correctly by reading {@code
   * tarArchiveInputStream}.
   */
  private void verifyTarArchive(TarArchiveInputStream tarArchiveInputStream) {
    // Verifies fileA was archived correctly.
    TarEntry headerFileA = tarArchiveInputStream.getNextTarEntry();
    Assert.assertEquals("some/path/to/resourceFileA", headerFileA.getName());
    byte[] fileAString = ByteStreams.toByteArray(tarArchiveInputStream);
    Assert.assertArrayEquals(fileAContents, fileAString);

    // Verifies fileB was archived correctly.
    TarEntry headerFileB = tarArchiveInputStream.getNextTarEntry();
    Assert.assertEquals("crepecake", headerFileB.getName());
    byte[] fileBString = ByteStreams.toByteArray(tarArchiveInputStream);
    Assert.assertArrayEquals(fileBContents, fileBString);

    // Verifies directoryA was archived correctly.
    TarEntry headerDirectoryA = tarArchiveInputStream.getNextTarEntry();
    Assert.assertEquals("some/path/to/", headerDirectoryA.getName());

    // Verifies the long file was archived correctly.
    TarEntry headerFileALong = tarArchiveInputStream.getNextTarEntry();
    Assert.assertEquals(
        "some/really/long/path/that/exceeds/100/characters/abcdefghijklmnopqrstuvwxyz0123456789012345678901234567890",
        headerFileALong.getName());
    byte[] fileALongString = ByteStreams.toByteArray(tarArchiveInputStream);
    Assert.assertArrayEquals(fileAContents, fileALongString);

    Assert.assertNull(tarArchiveInputStream.getNextTarEntry());
  }
}
}
