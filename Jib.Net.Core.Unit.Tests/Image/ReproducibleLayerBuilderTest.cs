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
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.IO;

namespace com.google.cloud.tools.jib.image {
































/** Tests for {@link ReproducibleLayerBuilder}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class ReproducibleLayerBuilderTest {

  /**
   * Verifies the correctness of the next {@link TarArchiveEntry} in the {@link
   * TarArchiveInputStream}.
   *
   * @param tarArchiveInputStream the {@link TarArchiveInputStream} to read from
   * @param expectedExtractionPath the expected extraction path of the next entry
   * @param expectedFile the file to match against the contents of the next entry
   * @throws IOException if an I/O exception occurs
   */
  private static void verifyNextTarArchiveEntry(
      TarInputStream tarArchiveInputStream, string expectedExtractionPath, SystemPath expectedFile)
      {
    TarEntry header = tarArchiveInputStream.getNextTarEntry();
    Assert.AreEqual(expectedExtractionPath, header.getName());

            string expectedString = StandardCharsets.UTF_8.GetString(Files.readAllBytes(expectedFile));

    string extractedString =
        CharStreams.toString(new StreamReader(tarArchiveInputStream, StandardCharsets.UTF_8));

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
  private static void verifyNextTarArchiveEntryIsDirectory(
      TarInputStream tarArchiveInputStream, string expectedExtractionPath)
      {
    TarEntry extractionPathEntry = tarArchiveInputStream.getNextTarEntry();
    Assert.AreEqual(expectedExtractionPath, extractionPathEntry.getName());
    Assert.IsTrue(extractionPathEntry.isDirectory());
  }

  private static LayerEntry defaultLayerEntry(SystemPath source, AbsoluteUnixPath destination) {
    return new LayerEntry(
        source,
        destination,
        LayerConfiguration.DEFAULT_FILE_PERMISSIONS_PROVIDER.apply(source, destination),
        LayerConfiguration.DEFAULT_MODIFIED_TIME);
  }

  [Rule] public TemporaryFolder temporaryFolder = new TemporaryFolder();

  [Test]
  public void testBuild() {
    SystemPath layerDirectory = Paths.get(Resources.getResource("core/layer").toURI());
    SystemPath blobA = Paths.get(Resources.getResource("core/blobA").toURI());

    ReproducibleLayerBuilder layerBuilder =
        new ReproducibleLayerBuilder(
            LayerConfiguration.builder()
                .addEntryRecursive(
                    layerDirectory, AbsoluteUnixPath.get("/extract/here/apple/layer"))
                .addEntry(blobA, AbsoluteUnixPath.get("/extract/here/apple/blobA"))
                .addEntry(blobA, AbsoluteUnixPath.get("/extract/here/banana/blobA"))
                .build()
                .getLayerEntries());

    // Writes the layer tar to a temporary file.
    Blob unwrittenBlob = layerBuilder.build();
    SystemPath temporaryFile = temporaryFolder.newFile().toPath();
    using (Stream temporaryFileOutputStream =
        new BufferedStream(Files.newOutputStream(temporaryFile))) {
      unwrittenBlob.writeTo(temporaryFileOutputStream);
    }

    // Reads the file back.
    using (TarInputStream tarArchiveInputStream =
        new TarInputStream(Files.newInputStream(temporaryFile))) {
      verifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/");
      verifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/");
      verifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/apple/");
      verifyNextTarArchiveEntry(tarArchiveInputStream, "extract/here/apple/blobA", blobA);
      verifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/apple/layer/");
      verifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/apple/layer/a/");
      verifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/apple/layer/a/b/");
      verifyNextTarArchiveEntry(
          tarArchiveInputStream,
          "extract/here/apple/layer/a/b/bar",
          Paths.get(Resources.getResource("core/layer/a/b/bar").toURI()));
      verifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/apple/layer/c/");
      verifyNextTarArchiveEntry(
          tarArchiveInputStream,
          "extract/here/apple/layer/c/cat",
          Paths.get(Resources.getResource("core/layer/c/cat").toURI()));
      verifyNextTarArchiveEntry(
          tarArchiveInputStream,
          "extract/here/apple/layer/foo",
          Paths.get(Resources.getResource("core/layer/foo").toURI()));
      verifyNextTarArchiveEntryIsDirectory(tarArchiveInputStream, "extract/here/banana/");
      verifyNextTarArchiveEntry(tarArchiveInputStream, "extract/here/banana/blobA", blobA);
    }
  }

  [Test]
  public void testToBlob_reproducibility() {
    SystemPath testRoot = temporaryFolder.getRoot().toPath();
    SystemPath root1 = Files.createDirectories(testRoot.resolve("files1"));
    SystemPath root2 = Files.createDirectories(testRoot.resolve("files2"));

    // TODO: Currently this test only covers variation in order and modified time, even though
    // TODO: the code is designed to clean up userid/groupid, this test does not check that yet.
    string contentA = "abcabc";
    SystemPath fileA1 = createFile(root1, "fileA", contentA, 10000);
    SystemPath fileA2 = createFile(root2, "fileA", contentA, 20000);
    string contentB = "yumyum";
    SystemPath fileB1 = createFile(root1, "fileB", contentB, 10000);
    SystemPath fileB2 = createFile(root2, "fileB", contentB, 20000);

    // check if modified times are off
    Assert.AreNotEqual(Files.getLastModifiedTime(fileA1), Files.getLastModifiedTime(fileA2));
    Assert.AreNotEqual(Files.getLastModifiedTime(fileB1), Files.getLastModifiedTime(fileB2));

    // create layers of exact same content but ordered differently and with different timestamps
    Blob layer =
        new ReproducibleLayerBuilder(
                ImmutableArray.Create(
                    defaultLayerEntry(fileA1, AbsoluteUnixPath.get("/somewhere/fileA")),
                    defaultLayerEntry(fileB1, AbsoluteUnixPath.get("/somewhere/fileB"))))
            .build();
    Blob reproduced =
        new ReproducibleLayerBuilder(
                ImmutableArray.Create(
                    defaultLayerEntry(fileB2, AbsoluteUnixPath.get("/somewhere/fileB")),
                    defaultLayerEntry(fileA2, AbsoluteUnixPath.get("/somewhere/fileA"))))
            .build();

    byte[] layerContent = Blobs.writeToByteArray(layer);
    byte[] reproducedLayerContent = Blobs.writeToByteArray(reproduced);

    Assert.AreEqual(layerContent, (reproducedLayerContent));
  }

  [Test]
  public void testBuild_parentDirBehavior() {
    SystemPath testRoot = temporaryFolder.getRoot().toPath();

    // the path doesn't really matter on source files, but these are structured
    SystemPath parent = Files.createDirectories(testRoot.resolve("aaa"));
    SystemPath fileA = Files.createFile(parent.resolve("fileA"));
    SystemPath ignoredParent = Files.createDirectories(testRoot.resolve("bbb-ignored"));
    SystemPath fileB = Files.createFile(ignoredParent.resolve("fileB"));
    SystemPath fileC =
        Files.createFile(Files.createDirectories(testRoot.resolve("ccc-absent")).resolve("fileC"));

    Blob layer =
        new ReproducibleLayerBuilder(
                ImmutableArray.Create(
                    new LayerEntry(
                        parent,
                        AbsoluteUnixPath.get("/root/aaa"),
                        FilePermissions.fromOctalString("111"),
                        Instant.FromUnixTimeSeconds(10)),
                    new LayerEntry(
                        fileA,
                        AbsoluteUnixPath.get("/root/aaa/fileA"),
                        FilePermissions.fromOctalString("222"),
                        Instant.FromUnixTimeSeconds(20)),
                    new LayerEntry(
                        fileB,
                        AbsoluteUnixPath.get("/root/bbb-ignored/fileB"),
                        FilePermissions.fromOctalString("333"),
                        Instant.FromUnixTimeSeconds(30)),
                    new LayerEntry(
                        ignoredParent,
                        AbsoluteUnixPath.get("/root/bbb-ignored"),
                        FilePermissions.fromOctalString("444"),
                        Instant.FromUnixTimeSeconds(40)),
                    new LayerEntry(
                        fileC,
                        AbsoluteUnixPath.get("/root/ccc-absent/file3"),
                        FilePermissions.fromOctalString("555"),
                        Instant.FromUnixTimeSeconds(50))))
            .build();

    SystemPath tarFile = temporaryFolder.newFile().toPath();
    using (Stream @out = new BufferedStream(Files.newOutputStream(tarFile))) {
      layer.writeTo(@out);
    }

    using (TarInputStream @in = new TarInputStream(Files.newInputStream(tarFile))) {
      // root (default folder permissions)
      TarEntry root = @in.getNextTarEntry();
      Assert.AreEqual(040755, root.getMode());
      Assert.AreEqual(Instant.FromUnixTimeSeconds(1), root.getModTime().toInstant());

      // parentAAA (custom permissions, custom timestamp)
      TarEntry rootParentAAA = @in.getNextTarEntry();
      Assert.AreEqual(040111, rootParentAAA.getMode());
      Assert.AreEqual(Instant.FromUnixTimeSeconds(10), rootParentAAA.getModTime().toInstant());

      // skip over fileA
      @in.getNextTarEntry();

      // parentBBB (default permissions - ignored custom permissions, since fileB added first)
      TarEntry rootParentBBB = @in.getNextTarEntry();
      // TODO (#1650): we want 040444 here.
      Assert.AreEqual(040755, rootParentBBB.getMode());
      // TODO (#1650): we want Instant.ofEpochSecond(40) here.
      Assert.AreEqual(Instant.FromUnixTimeSeconds(1), root.getModTime().toInstant());

      // skip over fileB
      @in.getNextTarEntry();

      // parentCCC (default permissions - no entry provided)
      TarEntry rootParentCCC = @in.getNextTarEntry();
      Assert.AreEqual(040755, rootParentCCC.getMode());
      Assert.AreEqual(Instant.FromUnixTimeSeconds(1), root.getModTime().toInstant());

      // we don't care about fileC
    }
  }

  [Test]
  public void testBuild_timestampDefault() {
    SystemPath file = createFile(temporaryFolder.getRoot().toPath(), "fileA", "some content", 54321);

    Blob blob =
        new ReproducibleLayerBuilder(
                ImmutableArray.Create(defaultLayerEntry(file, AbsoluteUnixPath.get("/fileA"))))
            .build();

    SystemPath tarFile = temporaryFolder.newFile().toPath();
    using (Stream @out = new BufferedStream(Files.newOutputStream(tarFile))) {
      blob.writeTo(@out);
    }

    // Reads the file back.
    using (TarInputStream @in = new TarInputStream(Files.newInputStream(tarFile))) {
      Assert.AreEqual(
          Instant.FromUnixTimeSeconds(0).plusSeconds(1).ToDateTimeUtc(), @in.getNextEntry().getLastModifiedDate());
    }
  }

  [Test]
  public void testBuild_timestampNonDefault() {
    SystemPath file = createFile(temporaryFolder.getRoot().toPath(), "fileA", "some content", 54321);

    Blob blob =
        new ReproducibleLayerBuilder(
                ImmutableArray.Create(
                    new LayerEntry(
                        file,
                        AbsoluteUnixPath.get("/fileA"),
                        FilePermissions.DEFAULT_FILE_PERMISSIONS,
                        Instant.FromUnixTimeSeconds(123))))
            .build();

    SystemPath tarFile = temporaryFolder.newFile().toPath();
    using (Stream @out = new BufferedStream(Files.newOutputStream(tarFile))) {
      blob.writeTo(@out);
    }

    // Reads the file back.
    using (TarInputStream @in = new TarInputStream(Files.newInputStream(tarFile))) {
      Assert.AreEqual(
          Date.from(Instant.FromUnixTimeSeconds(0).plusSeconds(123)), @in.getNextEntry().getLastModifiedDate());
    }
  }

  [Test]
  public void testBuild_permissions() {
    SystemPath testRoot = temporaryFolder.getRoot().toPath();
    SystemPath folder = Files.createDirectories(testRoot.resolve("files1"));
    SystemPath fileA = createFile(testRoot, "fileA", "abc", 54321);
    SystemPath fileB = createFile(testRoot, "fileB", "def", 54321);

    Blob blob =
        new ReproducibleLayerBuilder(
                ImmutableArray.Create(
                    defaultLayerEntry(fileA, AbsoluteUnixPath.get("/somewhere/fileA")),
                    new LayerEntry(
                        fileB,
                        AbsoluteUnixPath.get("/somewhere/fileB"),
                        FilePermissions.fromOctalString("123"),
                        LayerConfiguration.DEFAULT_MODIFIED_TIME),
                    new LayerEntry(
                        folder,
                        AbsoluteUnixPath.get("/somewhere/folder"),
                        FilePermissions.fromOctalString("456"),
                        LayerConfiguration.DEFAULT_MODIFIED_TIME)))
            .build();

    SystemPath tarFile = temporaryFolder.newFile().toPath();
    using (Stream @out = new BufferedStream(Files.newOutputStream(tarFile))) {
      blob.writeTo(@out);
    }

    using (TarInputStream @in = new TarInputStream(Files.newInputStream(tarFile))) {
      // Root folder (default folder permissions)
      Assert.AreEqual(040755, @in.getNextTarEntry().getMode());
      // fileA (default file permissions)
      Assert.AreEqual(0100644, @in.getNextTarEntry().getMode());
      // fileB (custom file permissions)
      Assert.AreEqual(0100123, @in.getNextTarEntry().getMode());
      // folder (custom folder permissions)
      Assert.AreEqual(040456, @in.getNextTarEntry().getMode());
    }
  }

  private SystemPath createFile(SystemPath root, string filename, string content, long lastModifiedTime)
      {
    SystemPath newFile =
        Files.write(
            root.resolve(filename),
            content.getBytes(StandardCharsets.UTF_8));
    Files.setLastModifiedTime(newFile, FileTime.fromMillis(lastModifiedTime));
    return newFile;
  }
}
}
