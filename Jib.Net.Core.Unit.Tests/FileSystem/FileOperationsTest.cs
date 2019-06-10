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

namespace com.google.cloud.tools.jib.filesystem {






















/** Tests for {@link FileOperations}. */
public class FileOperationsTest {

  private static void verifyWriteWithLock(SystemPath file) {
    Stream fileOutputStream = FileOperations.newLockingOutputStream(file);

            try {
                // Checks that the file was locked.
                using (FileChannel channel = FileChannel.open(file, StandardOpenOption.READ))
                {
                    // locking should either fail and return null or throw an OverlappingFileLockException
                    FileLock @lock = channel.tryLock(0, long.MAX_VALUE, true);
                    Assert.assertNull("Lock attempt should have failed", @lock);
                }
    } catch (OverlappingFileLockException ex) {
      // pass
    }

    fileOutputStream.write("jib".getBytes(StandardCharsets.UTF_8));
    fileOutputStream.close();

    FileChannel channel = FileChannel.open(file, StandardOpenOption.READ);
    channel.@lock(0, long.MAX_VALUE, true);
    using(Stream inputStream = Channels.newInputStream(channel))
    using(StreamReader inputStreamReader =
            new StreamReader(inputStream, StandardCharsets.UTF_8))
    {

      Assert.assertEquals("jib", CharStreams.toString(inputStreamReader));
    }
  }

  [Rule] public TemporaryFolder temporaryFolder = new TemporaryFolder();

  [TestMethod]
  public void testCopy() {
    SystemPath destDir = temporaryFolder.newFolder().toPath();
    SystemPath libraryA =
        Paths.get(Resources.getResource("core/application/dependencies/libraryA.jar").toURI());
    SystemPath libraryB =
        Paths.get(Resources.getResource("core/application/dependencies/libraryB.jar").toURI());
    SystemPath dirLayer = Paths.get(Resources.getResource("core/layer").toURI());

    FileOperations.copy(ImmutableArray.Create(libraryA, libraryB, dirLayer), destDir);

    assertFilesEqual(libraryA, destDir.resolve("libraryA.jar"));
    assertFilesEqual(libraryB, destDir.resolve("libraryB.jar"));
    Assert.assertTrue(Files.exists(destDir.resolve("layer").resolve("a").resolve("b")));
    Assert.assertTrue(Files.exists(destDir.resolve("layer").resolve("c")));
    assertFilesEqual(
        dirLayer.resolve("a").resolve("b").resolve("bar"),
        destDir.resolve("layer").resolve("a").resolve("b").resolve("bar"));
    assertFilesEqual(
        dirLayer.resolve("c").resolve("cat"), destDir.resolve("layer").resolve("c").resolve("cat"));
    assertFilesEqual(dirLayer.resolve("foo"), destDir.resolve("layer").resolve("foo"));
  }

  [TestMethod]
  public void testNewLockingOutputStream_newFile() {
    SystemPath file = Files.createTempFile("", "");
    // Ensures file doesn't exist.
    Assert.assertTrue(Files.deleteIfExists(file));

    verifyWriteWithLock(file);
  }

  [TestMethod]
  public void testNewLockingOutputStream_existingFile() {
    SystemPath file = Files.createTempFile("", "");
    // Writes out more bytes to ensure proper truncated.
    byte[] dataBytes = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
    Files.write(file, dataBytes, StandardOpenOption.WRITE);
    Assert.assertTrue(Files.exists(file));
    Assert.assertEquals(10, Files.size(file));

    verifyWriteWithLock(file);
  }

  private void assertFilesEqual(SystemPath file1, SystemPath file2) {
    Assert.assertArrayEquals(Files.readAllBytes(file1), Files.readAllBytes(file2));
  }
}
}
