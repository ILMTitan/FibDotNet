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

namespace com.google.cloud.tools.jib.blob {








/** A {@link Blob} that holds a {@link Path}. */
class FileBlob : Blob {

  private readonly Path file;

  FileBlob(Path file) {
    this.file = file;
  }

  public BlobDescriptor writeTo(OutputStream outputStream) {
    using (InputStream fileIn = new BufferedInputStream(Files.newInputStream(file))) {
      return Digests.computeDigest(fileIn, outputStream);
    }
  }
}
}
