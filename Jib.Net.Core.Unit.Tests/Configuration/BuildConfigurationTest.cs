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

namespace com.google.cloud.tools.jib.configuration {



























/** Tests for {@link BuildConfiguration}. */
public class BuildConfigurationTest {

  [TestMethod]
  public void testBuilder() {
    string expectedBaseImageServerUrl = "someserver";
    string expectedBaseImageName = "baseimage";
    string expectedBaseImageTag = "baseimagetag";
    string expectedTargetServerUrl = "someotherserver";
    string expectedTargetImageName = "targetimage";
    string expectedTargetTag = "targettag";
    ISet<string> additionalTargetImageTags = ImmutableHashSet.of("tag1", "tag2", "tag3");
    ISet<string> expectedTargetImageTags = ImmutableHashSet.of("targettag", "tag1", "tag2", "tag3");
    IList<CredentialRetriever> credentialRetrievers =
        Collections.singletonList(() => Optional.of(Credential.from("username", "password")));
    Instant expectedCreationTime = Instant.FromUnixTimeSeconds(10000);
    IList<string> expectedEntrypoint = Arrays.asList("some", "entrypoint");
    IList<string> expectedProgramArguments = Arrays.asList("arg1", "arg2");
    IDictionary<string, string> expectedEnvironment = ImmutableDictionary.of("key", "value");
    ImmutableHashSet<Port> expectedExposedPorts = ImmutableHashSet.of(Port.tcp(1000), Port.tcp(2000));
    IDictionary<string, string> expectedLabels = ImmutableDictionary.of("key1", "value1", "key2", "value2");
    Class<? extends BuildableManifestTemplate> expectedTargetFormat = typeof(OCIManifestTemplate);
    SystemPath expectedApplicationLayersCacheDirectory = Paths.get("application/layers");
    SystemPath expectedBaseImageLayersCacheDirectory = Paths.get("base/image/layers");
    IList<LayerConfiguration> expectedLayerConfigurations =
        Collections.singletonList(
            LayerConfiguration.builder()
                .addEntry(Paths.get("sourceFile"), AbsoluteUnixPath.get("/path/in/container"))
                .build());
    string expectedCreatedBy = "createdBy";

    ImageConfiguration baseImageConfiguration =
        ImageConfiguration.builder(
                ImageReference.of(
                    expectedBaseImageServerUrl, expectedBaseImageName, expectedBaseImageTag))
            .build();
    ImageConfiguration targetImageConfiguration =
        ImageConfiguration.builder(
                ImageReference.of(
                    expectedTargetServerUrl, expectedTargetImageName, expectedTargetTag))
            .setCredentialRetrievers(credentialRetrievers)
            .build();
    ContainerConfiguration containerConfiguration =
        ContainerConfiguration.builder()
            .setCreationTime(expectedCreationTime)
            .setEntrypoint(expectedEntrypoint)
            .setProgramArguments(expectedProgramArguments)
            .setEnvironment(expectedEnvironment)
            .setExposedPorts(expectedExposedPorts)
            .setLabels(expectedLabels)
            .build();
    BuildConfiguration.Builder buildConfigurationBuilder =
        BuildConfiguration.builder()
            .setBaseImageConfiguration(baseImageConfiguration)
            .setTargetImageConfiguration(targetImageConfiguration)
            .setAdditionalTargetImageTags(additionalTargetImageTags)
            .setContainerConfiguration(containerConfiguration)
            .setApplicationLayersCacheDirectory(expectedApplicationLayersCacheDirectory)
            .setBaseImageLayersCacheDirectory(expectedBaseImageLayersCacheDirectory)
            .setTargetFormat(ImageFormat.OCI)
            .setAllowInsecureRegistries(true)
            .setLayerConfigurations(expectedLayerConfigurations)
            .setToolName(expectedCreatedBy)
            .setExecutorService(MoreExecutors.newDirectExecutorService());
    BuildConfiguration buildConfiguration = buildConfigurationBuilder.build();

    Assert.assertNotNull(buildConfiguration.getContainerConfiguration());
    Assert.assertEquals(
        expectedCreationTime, buildConfiguration.getContainerConfiguration().getCreationTime());
    Assert.assertEquals(
        expectedBaseImageServerUrl,
        buildConfiguration.getBaseImageConfiguration().getImageRegistry());
    Assert.assertEquals(
        expectedBaseImageName, buildConfiguration.getBaseImageConfiguration().getImageRepository());
    Assert.assertEquals(
        expectedBaseImageTag, buildConfiguration.getBaseImageConfiguration().getImageTag());
    Assert.assertEquals(
        expectedTargetServerUrl,
        buildConfiguration.getTargetImageConfiguration().getImageRegistry());
    Assert.assertEquals(
        expectedTargetImageName,
        buildConfiguration.getTargetImageConfiguration().getImageRepository());
    Assert.assertEquals(
        expectedTargetTag, buildConfiguration.getTargetImageConfiguration().getImageTag());
    Assert.assertEquals(expectedTargetImageTags, buildConfiguration.getAllTargetImageTags());
    Assert.assertEquals(
        Credential.from("username", "password"),
        buildConfiguration
            .getTargetImageConfiguration()
            .getCredentialRetrievers()
            .get(0)
            .retrieve()
            .orElseThrow(AssertionError.new));
    Assert.assertEquals(
        expectedProgramArguments,
        buildConfiguration.getContainerConfiguration().getProgramArguments());
    Assert.assertEquals(
        expectedEnvironment, buildConfiguration.getContainerConfiguration().getEnvironmentMap());
    Assert.assertEquals(
        expectedExposedPorts, buildConfiguration.getContainerConfiguration().getExposedPorts());
    Assert.assertEquals(expectedLabels, buildConfiguration.getContainerConfiguration().getLabels());
    Assert.assertEquals(expectedTargetFormat, buildConfiguration.getTargetFormat());
    Assert.assertEquals(
        expectedApplicationLayersCacheDirectory,
        buildConfigurationBuilder.getApplicationLayersCacheDirectory());
    Assert.assertEquals(
        expectedBaseImageLayersCacheDirectory,
        buildConfigurationBuilder.getBaseImageLayersCacheDirectory());
    Assert.assertTrue(buildConfiguration.getAllowInsecureRegistries());
    Assert.assertEquals(expectedLayerConfigurations, buildConfiguration.getLayerConfigurations());
    Assert.assertEquals(
        expectedEntrypoint, buildConfiguration.getContainerConfiguration().getEntrypoint());
    Assert.assertEquals(expectedCreatedBy, buildConfiguration.getToolName());
    Assert.assertNotNull(buildConfiguration.getExecutorService());
  }

  [TestMethod]
  public void testBuilder_default() {
    // These are required and don't have defaults.
    string expectedBaseImageServerUrl = "someserver";
    string expectedBaseImageName = "baseimage";
    string expectedBaseImageTag = "baseimagetag";
    string expectedTargetServerUrl = "someotherserver";
    string expectedTargetImageName = "targetimage";
    string expectedTargetTag = "targettag";

    ImageConfiguration baseImageConfiguration =
        ImageConfiguration.builder(
                ImageReference.of(
                    expectedBaseImageServerUrl, expectedBaseImageName, expectedBaseImageTag))
            .build();
    ImageConfiguration targetImageConfiguration =
        ImageConfiguration.builder(
                ImageReference.of(
                    expectedTargetServerUrl, expectedTargetImageName, expectedTargetTag))
            .build();
    BuildConfiguration.Builder buildConfigurationBuilder =
        BuildConfiguration.builder()
            .setBaseImageConfiguration(baseImageConfiguration)
            .setTargetImageConfiguration(targetImageConfiguration)
            .setBaseImageLayersCacheDirectory(Paths.get("ignored"))
            .setApplicationLayersCacheDirectory(Paths.get("ignored"))
            .setExecutorService(MoreExecutors.newDirectExecutorService());
    BuildConfiguration buildConfiguration = buildConfigurationBuilder.build();

    Assert.assertEquals(ImmutableHashSet.of("targettag"), buildConfiguration.getAllTargetImageTags());
    Assert.assertEquals(typeof(V22ManifestTemplate), buildConfiguration.getTargetFormat());
    Assert.assertNotNull(buildConfigurationBuilder.getApplicationLayersCacheDirectory());
    Assert.assertEquals(
        Paths.get("ignored"), buildConfigurationBuilder.getApplicationLayersCacheDirectory());
    Assert.assertNotNull(buildConfigurationBuilder.getBaseImageLayersCacheDirectory());
    Assert.assertEquals(
        Paths.get("ignored"), buildConfigurationBuilder.getBaseImageLayersCacheDirectory());
    Assert.assertNull(buildConfiguration.getContainerConfiguration());
    Assert.assertFalse(buildConfiguration.getAllowInsecureRegistries());
    Assert.assertEquals(Collections.emptyList(), buildConfiguration.getLayerConfigurations());
    Assert.assertEquals("jib", buildConfiguration.getToolName());
  }

  [TestMethod]
  public void testBuilder_missingValues() {
    // Target image is missing
    try {
      BuildConfiguration.builder()
          .setBaseImageConfiguration(
              ImageConfiguration.builder(Mockito.mock(typeof(ImageReference))).build())
          .setBaseImageLayersCacheDirectory(Paths.get("ignored"))
          .setApplicationLayersCacheDirectory(Paths.get("ignored"))
          .setExecutorService(MoreExecutors.newDirectExecutorService())
          .build();
      Assert.fail("Build configuration should not be built with missing values");

    } catch (InvalidOperationException ex) {
      Assert.assertEquals("target image configuration is required but not set", ex.getMessage());
    }

    // Two required fields missing
    try {
      BuildConfiguration.builder()
          .setBaseImageLayersCacheDirectory(Paths.get("ignored"))
          .setApplicationLayersCacheDirectory(Paths.get("ignored"))
          .setExecutorService(MoreExecutors.newDirectExecutorService())
          .build();
      Assert.fail("Build configuration should not be built with missing values");

    } catch (InvalidOperationException ex) {
      Assert.assertEquals(
          "base image configuration and target image configuration are required but not set",
          ex.getMessage());
    }

    // All required fields missing
    try {
      BuildConfiguration.builder().build();
      Assert.fail("Build configuration should not be built with missing values");

    } catch (InvalidOperationException ex) {
      Assert.assertEquals(
          "base image configuration, target image configuration, base image layers cache directory, "
              + "application layers cache directory, and executor service are required but not set",
          ex.getMessage());
    }
  }
}
}
