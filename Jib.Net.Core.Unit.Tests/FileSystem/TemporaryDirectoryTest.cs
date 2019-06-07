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











/** Tests for {@link TemporaryDirectory}. */
public class TemporaryDirectoryTest {

  private static void createFilesInDirectory(Path directory)
      {
    Path testFilesDirectory = Paths.get(Resources.getResource("core/layer").toURI());
    new DirectoryWalker(testFilesDirectory)
        .filterRoot()
        .walk(path => Files.copy(path, directory.resolve(testFilesDirectory.relativize(path))));
  }

  [Rule] public final TemporaryFolder temporaryFolder = new TemporaryFolder();

  [TestMethod]
  public void testClose_directoryDeleted() {
    using (TemporaryDirectory temporaryDirectory =
        new TemporaryDirectory(temporaryFolder.newFolder().toPath())) {
      createFilesInDirectory(temporaryDirectory.getDirectory());

      temporaryDirectory.close();
      Assert.assertFalse(Files.exists(temporaryDirectory.getDirectory()));
    }
  }

  [TestMethod]
  public void testClose_directoryNotDeletedIfMoved() {
    Path destinationParent = temporaryFolder.newFolder().toPath();

    using (TemporaryDirectory temporaryDirectory =
        new TemporaryDirectory(temporaryFolder.newFolder().toPath())) {
      createFilesInDirectory(temporaryDirectory.getDirectory());

      Assert.assertFalse(Files.exists(destinationParent.resolve("destination")));
      Files.move(temporaryDirectory.getDirectory(), destinationParent.resolve("destination"));

      temporaryDirectory.close();
      Assert.assertFalse(Files.exists(temporaryDirectory.getDirectory()));
      Assert.assertTrue(Files.exists(destinationParent.resolve("destination")));
    }
  }
}
}
