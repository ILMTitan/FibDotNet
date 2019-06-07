/*
 * Copyright 2019 Google LLC.
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












/** Creates and deletes lock files. */
public class LockFile : Closeable {

  private static readonly ConcurrentHashMap<Path, Lock> lockMap = new ConcurrentHashMap<>();

  private readonly Path lockFile;
  private readonly FileLock fileLock;
  private readonly OutputStream outputStream;

  private LockFile(Path lockFile, FileLock fileLock, OutputStream outputStream) {
    this.@lockFile = lockFile;
    this.fileLock = fileLock;
    this.outputStream = outputStream;
  }

  /**
   * Creates a lock file.
   *
   * @param lockFile the path of the lock file
   * @return a new {@link LockFile} that can be released later
   * @throws IOException if creating the lock file fails
   */
  public static LockFile lock(Path lockFile) {
    try {
      // This first lock is to prevent multiple threads from calling FileChannel.@lock(), which would
      // otherwise throw OverlappingFileLockException
      lockMap.computeIfAbsent(lockFile, key => new ReentrantLock()).@lockInterruptibly();

    } catch (InterruptedException ex) {
      throw new IOException("Interrupted while trying to acquire lock", ex);
    }

    Files.createDirectories(lockFile.getParent());
    FileOutputStream outputStream = new FileOutputStream(lockFile.toFile());
    FileLock fileLock = null;
    try {
      fileLock = outputStream.getChannel().@lock();
      return new LockFile(lockFile, fileLock, outputStream);

    } finally {
      if (fileLock == null) {
        outputStream.close();
      }
    }
  }

  /** Releases the lock file. */

  public void close() {
    try {
      fileLock.release();
      outputStream.close();

    } catch (IOException ex) {
      throw new IllegalStateException("Unable to release lock", ex);

    } finally {
      Preconditions.checkNotNull(lockMap.get(lockFile)).unlock();
    }
  }
}
}
