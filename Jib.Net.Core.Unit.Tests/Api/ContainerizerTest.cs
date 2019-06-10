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












/** Tests for {@link Containerizer}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class ContainerizerTest {

  [Mock] private ExecutorService mockExecutorService;

  [TestMethod]
  public void testTo() {
    RegistryImage registryImage = RegistryImage.named(ImageReference.of(null, "repository", null));
    DockerDaemonImage dockerDaemonImage =
        DockerDaemonImage.named(ImageReference.of(null, "repository", null));
    TarImage tarImage =
        TarImage.named(ImageReference.of(null, "repository", null)).saveTo(Paths.get("ignored"));

    verifyTo(Containerizer.to(registryImage));
    verifyTo(Containerizer.to(dockerDaemonImage));
    verifyTo(Containerizer.to(tarImage));
  }

  private void verifyTo(Containerizer containerizer) {
    Assert.assertTrue(containerizer.getAdditionalTags().isEmpty());
    Assert.assertFalse(containerizer.getExecutorService().isPresent());
    Assert.assertEquals(
        Containerizer.DEFAULT_BASE_CACHE_DIRECTORY,
        containerizer.getBaseImageLayersCacheDirectory());
    Assert.assertNotEquals(
        Containerizer.DEFAULT_BASE_CACHE_DIRECTORY,
        containerizer.getApplicationLayersCacheDirectory());
    Assert.assertFalse(containerizer.getAllowInsecureRegistries());
    Assert.assertEquals("jib-core", containerizer.getToolName());

    containerizer
        .withAdditionalTag("tag1")
        .withAdditionalTag("tag2")
        .setExecutorService(mockExecutorService)
        .setBaseImageLayersCache(Paths.get("base/image/layers"))
        .setApplicationLayersCache(Paths.get("application/layers"))
        .setAllowInsecureRegistries(true)
        .setToolName("tool");

    Assert.assertEquals(ImmutableHashSet.of("tag1", "tag2"), containerizer.getAdditionalTags());
    Assert.assertTrue(containerizer.getExecutorService().isPresent());
    Assert.assertEquals(mockExecutorService, containerizer.getExecutorService().get());
    Assert.assertEquals(
        Paths.get("base/image/layers"), containerizer.getBaseImageLayersCacheDirectory());
    Assert.assertEquals(
        Paths.get("application/layers"), containerizer.getApplicationLayersCacheDirectory());
    Assert.assertTrue(containerizer.getAllowInsecureRegistries());
    Assert.assertEquals("tool", containerizer.getToolName());
  }

  [TestMethod]
  public void testWithAdditionalTag() {
    DockerDaemonImage dockerDaemonImage =
        DockerDaemonImage.named(ImageReference.of(null, "repository", null));
    Containerizer containerizer = Containerizer.to(dockerDaemonImage);

    containerizer.withAdditionalTag("tag");
    try {
      containerizer.withAdditionalTag("+invalid+");
      Assert.fail();
    } catch (ArgumentException ex) {
      Assert.assertEquals("invalid tag '+invalid+'", ex.getMessage());
    }
  }

  [TestMethod]
  public void testGetImageConfiguration_registryImage() {
    CredentialRetriever credentialRetriever = Mockito.mock(typeof(CredentialRetriever));
    Containerizer containerizer =
        Containerizer.to(
            RegistryImage.named("registry/image").addCredentialRetriever(credentialRetriever));

    ImageConfiguration imageConfiguration = containerizer.getImageConfiguration();
    Assert.assertEquals("registry/image", imageConfiguration.getImage().toString());
    Assert.assertEquals(
        Arrays.asList(credentialRetriever), imageConfiguration.getCredentialRetrievers());
  }

  [TestMethod]
  public void testGetImageConfiguration_dockerDaemonImage() {
    Containerizer containerizer = Containerizer.to(DockerDaemonImage.named("docker/deamon/image"));

    ImageConfiguration imageConfiguration = containerizer.getImageConfiguration();
    Assert.assertEquals("docker/deamon/image", imageConfiguration.getImage().toString());
    Assert.assertEquals(0, imageConfiguration.getCredentialRetrievers().size());
  }

  [TestMethod]
  public void testGetImageConfiguration_tarImage() {
    Containerizer containerizer =
        Containerizer.to(TarImage.named("tar/image").saveTo(Paths.get("output/file")));

    ImageConfiguration imageConfiguration = containerizer.getImageConfiguration();
    Assert.assertEquals("tar/image", imageConfiguration.getImage().toString());
    Assert.assertEquals(0, imageConfiguration.getCredentialRetrievers().size());
  }
}
}
