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

















/** Tests for {@link Blob}. */
public class BlobTest {

  [TestMethod]
  public void testFromInputStream() {
    string expected = "crepecake";
    InputStream inputStream = new ByteArrayInputStream(expected.getBytes(StandardCharsets.UTF_8));
    verifyBlobWriteTo(expected, Blobs.from(inputStream));
  }

  [TestMethod]
  public void testFromFile() {
    Path fileA = Paths.get(Resources.getResource("core/fileA").toURI());
    string expected = new string(Files.readAllBytes(fileA), StandardCharsets.UTF_8);
    verifyBlobWriteTo(expected, Blobs.from(fileA));
  }

  [TestMethod]
  public void testFromString() {
    string expected = "crepecake";
    verifyBlobWriteTo(expected, Blobs.from(expected));
  }

  [TestMethod]
  public void testFromWritableContents() {
    string expected = "crepecake";

    WritableContents writableContents =
        outputStream => outputStream.write(expected.getBytes(StandardCharsets.UTF_8));

    verifyBlobWriteTo(expected, Blobs.from(writableContents));
  }

  /** Checks that the {@link Blob} streams the expected string. */
  private void verifyBlobWriteTo(string expected, Blob blob) {
    OutputStream outputStream = new ByteArrayOutputStream();
    BlobDescriptor blobDescriptor = blob.writeTo(outputStream);

    string output = outputStream.toString();
    Assert.assertEquals(expected, output);

    byte[] expectedBytes = expected.getBytes(StandardCharsets.UTF_8);
    Assert.assertEquals(expectedBytes.length, blobDescriptor.getSize());

    DescriptorDigest expectedDigest =
        Digests.computeDigest(new ByteArrayInputStream(expectedBytes)).getDigest();
    Assert.assertEquals(expectedDigest, blobDescriptor.getDigest());
  }
}
}
