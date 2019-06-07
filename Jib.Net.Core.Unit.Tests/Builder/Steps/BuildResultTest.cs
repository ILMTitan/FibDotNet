/*
 * Copyright 2018 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace com.google.cloud.tools.jib.builder.steps {









/** Tests for {@link BuildResult}. */
public class BuildResultTest {

  private DescriptorDigest digest1;
  private DescriptorDigest digest2;
  private DescriptorDigest id;

  [TestInitialize]
  public void setUp() {
    digest1 =
        DescriptorDigest.fromDigest(
            "sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789");
    digest2 =
        DescriptorDigest.fromDigest(
            "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
    id =
        DescriptorDigest.fromDigest(
            "sha256:9876543210fedcba9876543210fedcba9876543210fedcba9876543210fedcba");
  }

  [TestMethod]
  public void testCreated() {
    BuildResult container = new BuildResult(digest1, id);
    Assert.assertEquals(digest1, container.getImageDigest());
    Assert.assertEquals(id, container.getImageId());
  }

  [TestMethod]
  public void testEquality() {
    BuildResult container1 = new BuildResult(digest1, id);
    BuildResult container2 = new BuildResult(digest1, id);
    BuildResult container3 = new BuildResult(digest2, id);

    Assert.assertEquals(container1, container2);
    Assert.assertEquals(container1.hashCode(), container2.hashCode());
    Assert.assertNotEquals(container1, container3);
  }

  [TestMethod]
  public void testFromImage() {
    Image image1 = Image.builder(typeof(V22ManifestTemplate)).setUser("user").build();
    Image image2 = Image.builder(typeof(V22ManifestTemplate)).setUser("user").build();
    Image image3 = Image.builder(typeof(V22ManifestTemplate)).setUser("anotherUser").build();
    Assert.assertEquals(
        BuildResult.fromImage(image1, typeof(V22ManifestTemplate)),
        BuildResult.fromImage(image2, typeof(V22ManifestTemplate)));
    Assert.assertNotEquals(
        BuildResult.fromImage(image1, typeof(V22ManifestTemplate)),
        BuildResult.fromImage(image3, typeof(V22ManifestTemplate)));
  }
}
}
