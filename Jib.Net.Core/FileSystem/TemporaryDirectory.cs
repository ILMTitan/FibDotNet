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









/**
 * A temporary directory that tries to delete itself upon close. Note that deletion is <b>NOT</b>
 * guaranteed.
 */
public class TemporaryDirectory : Closeable {

  private readonly Path temporaryDirectory;

  /**
   * Creates a new temporary directory under an existing {@code parentDirectory}.
   *
   * @param parentDirectory the directory to create the temporary directory within
   * @throws IOException if an I/O exception occurs
   */
  public TemporaryDirectory(Path parentDirectory) {
    temporaryDirectory = Files.createTempDirectory(parentDirectory, null);
  }

  /**
   * Gets the temporary directory.
   *
   * @return the temporary directory.
   */
  public Path getDirectory() {
    return temporaryDirectory;
  }

  public void close() {
    if (Files.exists(temporaryDirectory)) {
      try {
        MoreFiles.deleteRecursively(temporaryDirectory, RecursiveDeleteOption.ALLOW_INSECURE);
      } catch (FileNotFoundException | FileSystemException ex) {
        // TODO log error; deletion is best-effort
      }
    }
  }
}
}
