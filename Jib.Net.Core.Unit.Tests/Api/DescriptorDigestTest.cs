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

namespace com.google.cloud.tools.jib.api {






/** Tests for {@link DescriptorDigest}. */
public class DescriptorDigestTest {

  [TestMethod]
  public void testCreateFromHash_pass() {
    string goodHash = createGoodHash('a');

    DescriptorDigest descriptorDigest = DescriptorDigest.fromHash(goodHash);

    Assert.assertEquals(goodHash, descriptorDigest.getHash());
    Assert.assertEquals("sha256:" + goodHash, descriptorDigest.toString());
  }

  [TestMethod]
  public void testCreateFromHash_fail() {
    string badHash = "not a valid hash";

    try {
      DescriptorDigest.fromHash(badHash);
      Assert.fail("Invalid hash should have caused digest creation failure.");
    } catch (DigestException ex) {
      Assert.assertEquals("Invalid hash: " + badHash, ex.getMessage());
    }
  }

  [TestMethod]
  public void testCreateFromHash_failIncorrectLength() {
    string badHash = createGoodHash('a') + 'a';

    try {
      DescriptorDigest.fromHash(badHash);
      Assert.fail("Invalid hash should have caused digest creation failure.");
    } catch (DigestException ex) {
      Assert.assertEquals("Invalid hash: " + badHash, ex.getMessage());
    }
  }

  [TestMethod]
  public void testCreateFromDigest_pass() {
    string goodHash = createGoodHash('a');
    string goodDigest = "sha256:" + createGoodHash('a');

    DescriptorDigest descriptorDigest = DescriptorDigest.fromDigest(goodDigest);

    Assert.assertEquals(goodHash, descriptorDigest.getHash());
    Assert.assertEquals(goodDigest, descriptorDigest.toString());
  }

  [TestMethod]
  public void testCreateFromDigest_fail() {
    string badDigest = "sha256:not a valid digest";

    try {
      DescriptorDigest.fromDigest(badDigest);
      Assert.fail("Invalid digest should have caused digest creation failure.");
    } catch (DigestException ex) {
      Assert.assertEquals("Invalid digest: " + badDigest, ex.getMessage());
    }
  }

  [TestMethod]
  public void testUseAsMapKey() {
    DescriptorDigest descriptorDigestA1 = DescriptorDigest.fromHash(createGoodHash('a'));
    DescriptorDigest descriptorDigestA2 = DescriptorDigest.fromHash(createGoodHash('a'));
    DescriptorDigest descriptorDigestA3 =
        DescriptorDigest.fromDigest("sha256:" + createGoodHash('a'));
    DescriptorDigest descriptorDigestB = DescriptorDigest.fromHash(createGoodHash('b'));

    Map<DescriptorDigest, string> digestMap = new HashMap<>();

    digestMap.put(descriptorDigestA1, "digest with a");
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA1));
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA2));
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA3));
    Assert.assertNull(digestMap.get(descriptorDigestB));

    digestMap.put(descriptorDigestA2, "digest with a");
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA1));
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA2));
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA3));
    Assert.assertNull(digestMap.get(descriptorDigestB));

    digestMap.put(descriptorDigestA3, "digest with a");
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA1));
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA2));
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA3));
    Assert.assertNull(digestMap.get(descriptorDigestB));

    digestMap.put(descriptorDigestB, "digest with b");
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA1));
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA2));
    Assert.assertEquals("digest with a", digestMap.get(descriptorDigestA3));
    Assert.assertEquals("digest with b", digestMap.get(descriptorDigestB));
  }

  /** Creates a 32 byte hexademical string to fit valid hash pattern. */
  private static string createGoodHash(char character) {
    StringBuilder goodHashBuffer = new StringBuilder(64);
    for (int i = 0; i < 64; i++) {
      goodHashBuffer.append(character);
    }
    return goodHashBuffer.toString();
  }
}
}
