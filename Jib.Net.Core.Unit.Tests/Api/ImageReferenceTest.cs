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

namespace com.google.cloud.tools.jib.api {







/** Tests for {@link ImageReference}. */
public class ImageReferenceTest {

  private static readonly IList<string> goodRegistries =
      Arrays.asList("some.domain---name.123.com:8080", "gcr.io", "localhost", null, "");
  private static readonly IList<string> goodRepositories =
      Arrays.asList("some123_abc/repository__123-456/name---here", "distroless/java", "repository");
  private static readonly IList<string> goodTags = Arrays.asList("some-.-.Tag", "", "latest", null);
  private static readonly IList<string> goodDigests =
      Arrays.asList(
          "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", null);

  private static readonly IList<string> badImageReferences =
      Arrays.asList(
          "",
          ":justsometag",
          "@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "repository@sha256:a",
          "repository@notadigest",
          "Repositorywithuppercase",
          "registry:8080/Repositorywithuppercase/repository:sometag",
          "domain.name:nonnumberport/repository",
          "domain.name:nonnumberport//:no-repository");

  [TestMethod]
  public void testParse_pass() {
    foreach (string goodRegistry in goodRegistries)
    {
      foreach (string goodRepository in goodRepositories)
      {
        foreach (string goodTag in goodTags)
        {
          verifyParse(goodRegistry, goodRepository, ":", goodTag);
        }
        foreach (string goodDigest in goodDigests)
        {
          verifyParse(goodRegistry, goodRepository, "@", goodDigest);
        }
      }
    }
  }

  [TestMethod]
  public void testParse_dockerHub_official() {
    string imageReferenceString = "busybox";
    ImageReference imageReference = ImageReference.parse(imageReferenceString);

    Assert.assertEquals("registry-1.docker.io", imageReference.getRegistry());
    Assert.assertEquals("library/busybox", imageReference.getRepository());
    Assert.assertEquals("latest", imageReference.getTag());
  }

  [TestMethod]
  public void testParse_dockerHub_user() {
    string imageReferenceString = "someuser/someimage";
    ImageReference imageReference = ImageReference.parse(imageReferenceString);

    Assert.assertEquals("registry-1.docker.io", imageReference.getRegistry());
    Assert.assertEquals("someuser/someimage", imageReference.getRepository());
    Assert.assertEquals("latest", imageReference.getTag());
  }

  [TestMethod]
  public void testParse_invalid() {
    foreach (string badImageReference in badImageReferences)
    {
      try {
        ImageReference.parse(badImageReference);
        Assert.fail(badImageReference + " should not be a valid image reference");

      } catch (InvalidImageReferenceException ex) {
        Assert.assertThat(ex.getMessage(), CoreMatchers.containsString(badImageReference));
      }
    }
  }

  [TestMethod]
  public void testOf_smoke() {
    string expectedRegistry = "someregistry";
    string expectedRepository = "somerepository";
    string expectedTag = "sometag";

    Assert.assertEquals(
        expectedRegistry,
        ImageReference.of(expectedRegistry, expectedRepository, expectedTag).getRegistry());
    Assert.assertEquals(
        expectedRepository,
        ImageReference.of(expectedRegistry, expectedRepository, expectedTag).getRepository());
    Assert.assertEquals(
        expectedTag, ImageReference.of(expectedRegistry, expectedRepository, expectedTag).getTag());
    Assert.assertEquals(
        "registry-1.docker.io",
        ImageReference.of(null, expectedRepository, expectedTag).getRegistry());
    Assert.assertEquals(
        "registry-1.docker.io", ImageReference.of(null, expectedRepository, null).getRegistry());
    Assert.assertEquals(
        "latest", ImageReference.of(expectedRegistry, expectedRepository, null).getTag());
    Assert.assertEquals("latest", ImageReference.of(null, expectedRepository, null).getTag());
    Assert.assertEquals(
        expectedRepository, ImageReference.of(null, expectedRepository, null).getRepository());
  }

  [TestMethod]
  public void testToString() {
    Assert.assertEquals("someimage", ImageReference.of(null, "someimage", null).toString());
    Assert.assertEquals("someimage", ImageReference.of("", "someimage", "").toString());
    Assert.assertEquals(
        "someotherimage", ImageReference.of(null, "library/someotherimage", null).toString());
    Assert.assertEquals(
        "someregistry/someotherimage",
        ImageReference.of("someregistry", "someotherimage", null).toString());
    Assert.assertEquals(
        "anotherregistry/anotherimage:sometag",
        ImageReference.of("anotherregistry", "anotherimage", "sometag").toString());

    Assert.assertEquals(
        "someimage@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        ImageReference.of(
                null,
                "someimage",
                "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
            .toString());
    Assert.assertEquals(
        "gcr.io/distroless/java@sha256:b430543bea1d8326e767058bdab3a2482ea45f59d7af5c5c61334cd29ede88a1",
        ImageReference.parse(
                "gcr.io/distroless/java@sha256:b430543bea1d8326e767058bdab3a2482ea45f59d7af5c5c61334cd29ede88a1")
            .toString());
  }

  [TestMethod]
  public void testToStringWithTag() {
    Assert.assertEquals(
        "someimage:latest", ImageReference.of(null, "someimage", null).toStringWithTag());
    Assert.assertEquals(
        "someimage:latest", ImageReference.of("", "someimage", "").toStringWithTag());
    Assert.assertEquals(
        "someotherimage:latest",
        ImageReference.of(null, "library/someotherimage", null).toStringWithTag());
    Assert.assertEquals(
        "someregistry/someotherimage:latest",
        ImageReference.of("someregistry", "someotherimage", null).toStringWithTag());
    Assert.assertEquals(
        "anotherregistry/anotherimage:sometag",
        ImageReference.of("anotherregistry", "anotherimage", "sometag").toStringWithTag());
  }

  [TestMethod]
  public void testIsTagDigest() {
    Assert.assertFalse(ImageReference.of(null, "someimage", null).isTagDigest());
    Assert.assertFalse(ImageReference.of(null, "someimage", "latest").isTagDigest());
    Assert.assertTrue(
        ImageReference.of(
                null,
                "someimage",
                "sha256:b430543bea1d8326e767058bdab3a2482ea45f59d7af5c5c61334cd29ede88a1")
            .isTagDigest());
    Assert.assertTrue(
        ImageReference.parse(
                "someimage@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
            .isTagDigest());
  }

  [TestMethod]
  public void testIsScratch() {
    Assert.assertTrue(ImageReference.scratch().isScratch());
    Assert.assertFalse(ImageReference.of("", "scratch", "").isScratch());
    Assert.assertFalse(ImageReference.of(null, "scratch", null).isScratch());
  }

  [TestMethod]
  public void testGetRegistry() {
    Assert.assertEquals(
        "registry-1.docker.io", ImageReference.of(null, "someimage", null).getRegistry());
    Assert.assertEquals(
        "registry-1.docker.io", ImageReference.of("docker.io", "someimage", null).getRegistry());
    Assert.assertEquals(
        "index.docker.io", ImageReference.of("index.docker.io", "someimage", null).getRegistry());
    Assert.assertEquals(
        "registry.hub.docker.com",
        ImageReference.of("registry.hub.docker.com", "someimage", null).getRegistry());
    Assert.assertEquals("gcr.io", ImageReference.of("gcr.io", "someimage", null).getRegistry());
  }

  private void verifyParse(string registry, string repository, string tagSeparator, string tag)
      {
    // Gets the expected parsed components.
    string expectedRegistry = registry;
    if (Strings.isNullOrEmpty(expectedRegistry)) {
      expectedRegistry = "registry-1.docker.io";
    }
    string expectedRepository = repository;
    if ("registry-1.docker.io".Equals(expectedRegistry) && repository.indexOf('/') < 0) {
      expectedRepository = "library/" + expectedRepository;
    }
    string expectedTag = tag;
    if (Strings.isNullOrEmpty(expectedTag)) {
      expectedTag = "latest";
    }

    // Builds the image reference to parse.
    StringBuilder imageReferenceBuilder = new StringBuilder();
    if (!Strings.isNullOrEmpty(registry)) {
      imageReferenceBuilder.append(registry).append('/');
    }
    imageReferenceBuilder.append(repository);
    if (!Strings.isNullOrEmpty(tag)) {
      imageReferenceBuilder.append(tagSeparator).append(tag);
    }

    ImageReference imageReference = ImageReference.parse(imageReferenceBuilder.toString());

    Assert.assertEquals(expectedRegistry, imageReference.getRegistry());
    Assert.assertEquals(expectedRepository, imageReference.getRepository());
    Assert.assertEquals(expectedTag, imageReference.getTag());
  }
}
}
