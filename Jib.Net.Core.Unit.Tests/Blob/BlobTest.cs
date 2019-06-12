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

using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.hash;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.IO;

namespace com.google.cloud.tools.jib.blob {

















/** Tests for {@link Blob}. */
public class BlobTest {

  [Test]
  public void testFromInputStream() {
    string expected = "crepecake";
    Stream inputStream = new MemoryStream(expected.getBytes(StandardCharsets.UTF_8));
    verifyBlobWriteTo(expected, Blobs.from(inputStream));
  }

  [Test]
  public void testFromFile() {
    SystemPath fileA = Paths.get(Resources.getResource("core/fileA").toURI());
    string expected = StandardCharsets.UTF_8.GetString(Files.readAllBytes(fileA));
    verifyBlobWriteTo(expected, Blobs.from(fileA));
  }

  [Test]
  public void testFromString() {
    string expected = "crepecake";
    verifyBlobWriteTo(expected, Blobs.from(expected));
  }

  [Test]
  public void testFromWritableContents() {
    string expected = "crepecake";

    WritableContents writableContents =
        outputStream => outputStream.write(expected.getBytes(StandardCharsets.UTF_8));

    verifyBlobWriteTo(expected, Blobs.from(writableContents));
  }

  /** Checks that the {@link Blob} streams the expected string. */
  private void verifyBlobWriteTo(string expected, Blob blob) {
    Stream outputStream = new MemoryStream();
    BlobDescriptor blobDescriptor = blob.writeTo(outputStream);

    string output = outputStream.toString();
    Assert.AreEqual(expected, output);

    byte[] expectedBytes = expected.getBytes(StandardCharsets.UTF_8);
    Assert.AreEqual(expectedBytes.Length, blobDescriptor.getSize());

    DescriptorDigest expectedDigest =
        Digests.computeDigest(new MemoryStream(expectedBytes)).getDigest();
    Assert.AreEqual(expectedDigest, blobDescriptor.getDigest());
  }
}
}
