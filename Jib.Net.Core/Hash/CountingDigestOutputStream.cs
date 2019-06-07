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









/** A {@link DigestOutputStream} that also keeps track of the total number of bytes written. */
public class CountingDigestOutputStream : DigestOutputStream {

  private static readonly string SHA_256_ALGORITHM = "SHA-256";

  private long bytesSoFar = 0;

  /**
   * Wraps the {@code outputStream}.
   *
   * @param outputStream the {@link OutputStream} to wrap.
   */
  public CountingDigestOutputStream(OutputStream outputStream) : base(outputStream, null) {
    
    try {
      setMessageDigest(MessageDigest.getInstance(SHA_256_ALGORITHM));
    } catch (NoSuchAlgorithmException ex) {
      throw new RuntimeException(
          "SHA-256 algorithm implementation not found - might be a broken JVM");
    }
  }

  /**
   * Computes the hash and returns it along with the size of the bytes written to compute the hash.
   * The buffer resets after this method is called, so this method should only be called once per
   * computation.
   *
   * @return the computed hash and the size of the bytes consumed
   */
  public BlobDescriptor computeDigest() {
    try {
      byte[] hashedBytes = digest.digest();

      // Encodes each hashed byte into 2-character hexadecimal representation.
      StringBuilder stringBuilder = new StringBuilder(2 * hashedBytes.length);
      foreach (byte b in hashedBytes)
      {
        stringBuilder.append(string.format("%02x", b));
      }
      string hash = stringBuilder.toString();

      BlobDescriptor blobDescriptor =
          new BlobDescriptor(bytesSoFar, DescriptorDigest.fromHash(hash));
      bytesSoFar = 0;
      return blobDescriptor;

    } catch (DigestException ex) {
      throw new RuntimeException("SHA-256 algorithm produced invalid hash: " + ex.getMessage(), ex);
    }
  }

  public void write(byte[] data, int offset, int length) {
    super.write(data, offset, length);
    bytesSoFar += length;
  }

  public void write(int singleByte) {
    super.write(singleByte);
    bytesSoFar++;
  }
}
}
