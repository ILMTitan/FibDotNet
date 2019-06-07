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

namespace com.google.cloud.tools.jib.hash {















/** Tests for {@link CountingDigestOutputStream}. */
public class CountingDigestOutputStreamTest {

  private readonly Map<string, string> KNOWN_SHA256_HASHES =
      ImmutableMap.of(
          "crepecake",
          "52a9e4d4ba4333ce593707f98564fee1e6d898db0d3602408c0b2a6a424d357c",
          "12345",
          "5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5",
          "",
          "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");

  [TestMethod]
  public void test_smokeTest() {
    for (Map.Entry<string, string> knownHash : KNOWN_SHA256_HASHES.entrySet()) {
      string toHash = knownHash.getKey();
      string expectedHash = knownHash.getValue();

      OutputStream underlyingOutputStream = new ByteArrayOutputStream();
      CountingDigestOutputStream countingDigestOutputStream =
          new CountingDigestOutputStream(underlyingOutputStream);

      byte[] bytesToHash = toHash.getBytes(StandardCharsets.UTF_8);
      InputStream toHashInputStream = new ByteArrayInputStream(bytesToHash);
      ByteStreams.copy(toHashInputStream, countingDigestOutputStream);

      BlobDescriptor blobDescriptor = countingDigestOutputStream.computeDigest();
      Assert.assertEquals(DescriptorDigest.fromHash(expectedHash), blobDescriptor.getDigest());
      Assert.assertEquals(bytesToHash.length, blobDescriptor.getSize());
    }
  }
}
}
