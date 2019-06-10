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































/** Tests for {@link JibContainerBuilder}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class JibContainerBuilderTest {

  @Spy private BuildConfiguration.Builder spyBuildConfigurationBuilder;
  [Mock] private LayerConfiguration mockLayerConfiguration1;
  [Mock] private LayerConfiguration mockLayerConfiguration2;
  [Mock] private CredentialRetriever mockCredentialRetriever;
  [Mock] private ExecutorService mockExecutorService;
  [Mock] private Consumer<JibEvent> mockJibEventConsumer;
  [Mock] private JibEvent mockJibEvent;

  [TestMethod]
  public void testToBuildConfiguration_containerConfigurationSet()
      {
    JibContainerBuilder jibContainerBuilder =
        new JibContainerBuilder(RegistryImage.named("base/image"), spyBuildConfigurationBuilder)
            .setEntrypoint(Arrays.asList("entry", "point"))
            .setEnvironment(ImmutableDictionary.of("name", "value"))
            .setExposedPorts(ImmutableHashSet.of(Port.tcp(1234), Port.udp(5678)))
            .setLabels(ImmutableDictionary.of("key", "value"))
            .setProgramArguments(Arrays.asList("program", "arguments"))
            .setCreationTime(Instant.ofEpochMilli(1000))
            .setUser("user")
            .setWorkingDirectory(AbsoluteUnixPath.get("/working/directory"));

    BuildConfiguration buildConfiguration =
        jibContainerBuilder.toBuildConfiguration(
            Containerizer.to(RegistryImage.named("target/image")),
            MoreExecutors.newDirectExecutorService());
    ContainerConfiguration containerConfiguration = buildConfiguration.getContainerConfiguration();
    Assert.assertEquals(Arrays.asList("entry", "point"), containerConfiguration.getEntrypoint());
    Assert.assertEquals(
        ImmutableDictionary.of("name", "value"), containerConfiguration.getEnvironmentMap());
    Assert.assertEquals(
        ImmutableHashSet.of(Port.tcp(1234), Port.udp(5678)), containerConfiguration.getExposedPorts());
    Assert.assertEquals(ImmutableDictionary.of("key", "value"), containerConfiguration.getLabels());
    Assert.assertEquals(
        Arrays.asList("program", "arguments"), containerConfiguration.getProgramArguments());
    Assert.assertEquals(Instant.ofEpochMilli(1000), containerConfiguration.getCreationTime());
    Assert.assertEquals("user", containerConfiguration.getUser());
    Assert.assertEquals(
        AbsoluteUnixPath.get("/working/directory"), containerConfiguration.getWorkingDirectory());
  }

  [TestMethod]
  public void testToBuildConfiguration_containerConfigurationAdd()
      {
    JibContainerBuilder jibContainerBuilder =
        new JibContainerBuilder(RegistryImage.named("base/image"), spyBuildConfigurationBuilder)
            .setEntrypoint("entry", "point")
            .setEnvironment(ImmutableDictionary.of("name", "value"))
            .addEnvironmentVariable("environment", "variable")
            .setExposedPorts(Port.tcp(1234), Port.udp(5678))
            .addExposedPort(Port.tcp(1337))
            .setLabels(ImmutableDictionary.of("key", "value"))
            .addLabel("added", "label")
            .setProgramArguments("program", "arguments");

    BuildConfiguration buildConfiguration =
        jibContainerBuilder.toBuildConfiguration(
            Containerizer.to(RegistryImage.named("target/image")),
            MoreExecutors.newDirectExecutorService());
    ContainerConfiguration containerConfiguration = buildConfiguration.getContainerConfiguration();
    Assert.assertEquals(Arrays.asList("entry", "point"), containerConfiguration.getEntrypoint());
    Assert.assertEquals(
        ImmutableDictionary.of("name", "value", "environment", "variable"),
        containerConfiguration.getEnvironmentMap());
    Assert.assertEquals(
        ImmutableHashSet.of(Port.tcp(1234), Port.udp(5678), Port.tcp(1337)),
        containerConfiguration.getExposedPorts());
    Assert.assertEquals(
        ImmutableDictionary.of("key", "value", "added", "label"), containerConfiguration.getLabels());
    Assert.assertEquals(
        Arrays.asList("program", "arguments"), containerConfiguration.getProgramArguments());
    Assert.assertEquals(Instant.EPOCH, containerConfiguration.getCreationTime());
  }

  [TestMethod]
  public void testToBuildConfiguration()
      {
    RegistryImage targetImage =
        RegistryImage.named(ImageReference.of("gcr.io", "my-project/my-app", null))
            .addCredential("username", "password");
    Containerizer containerizer =
        Containerizer.to(targetImage)
            .setBaseImageLayersCache(Paths.get("base/image/layers"))
            .setApplicationLayersCache(Paths.get("application/layers"))
            .setExecutorService(mockExecutorService)
            .addEventHandler(mockJibEventConsumer);

    RegistryImage baseImage =
        RegistryImage.named("base/image").addCredentialRetriever(mockCredentialRetriever);
    JibContainerBuilder jibContainerBuilder =
        new JibContainerBuilder(baseImage, spyBuildConfigurationBuilder)
            .setLayers(Arrays.asList(mockLayerConfiguration1, mockLayerConfiguration2));
    BuildConfiguration buildConfiguration =
        jibContainerBuilder.toBuildConfiguration(
            containerizer, containerizer.getExecutorService().get());

    Assert.assertEquals(
        spyBuildConfigurationBuilder.build().getContainerConfiguration(),
        buildConfiguration.getContainerConfiguration());

    Assert.assertEquals(
        "base/image", buildConfiguration.getBaseImageConfiguration().getImage().toString());
    Assert.assertEquals(
        Arrays.asList(mockCredentialRetriever),
        buildConfiguration.getBaseImageConfiguration().getCredentialRetrievers());

    Assert.assertEquals(
        "gcr.io/my-project/my-app",
        buildConfiguration.getTargetImageConfiguration().getImage().toString());
    Assert.assertEquals(
        1, buildConfiguration.getTargetImageConfiguration().getCredentialRetrievers().size());
    Assert.assertEquals(
        Credential.from("username", "password"),
        buildConfiguration
            .getTargetImageConfiguration()
            .getCredentialRetrievers()
            .get(0)
            .retrieve()
            .orElseThrow(AssertionError.new));

    Assert.assertEquals(ImmutableHashSet.of("latest"), buildConfiguration.getAllTargetImageTags());

    Mockito.verify(spyBuildConfigurationBuilder)
        .setBaseImageLayersCacheDirectory(Paths.get("base/image/layers"));
    Mockito.verify(spyBuildConfigurationBuilder)
        .setApplicationLayersCacheDirectory(Paths.get("application/layers"));

    Assert.assertEquals(
        Arrays.asList(mockLayerConfiguration1, mockLayerConfiguration2),
        buildConfiguration.getLayerConfigurations());

    Assert.assertEquals(mockExecutorService, buildConfiguration.getExecutorService());

    buildConfiguration.getEventHandlers().dispatch(mockJibEvent);
    Mockito.verify(mockJibEventConsumer).accept(mockJibEvent);

    Assert.assertEquals("jib-core", buildConfiguration.getToolName());

    Assert.assertSame(typeof(V22ManifestTemplate), buildConfiguration.getTargetFormat());

    Assert.assertEquals("jib-core", buildConfiguration.getToolName());

    // Changes jibContainerBuilder.
    buildConfiguration =
        jibContainerBuilder
            .setFormat(ImageFormat.OCI)
            .toBuildConfiguration(
                containerizer
                    .withAdditionalTag("tag1")
                    .withAdditionalTag("tag2")
                    .setToolName("toolName"),
                MoreExecutors.newDirectExecutorService());
    Assert.assertSame(typeof(OCIManifestTemplate), buildConfiguration.getTargetFormat());
    Assert.assertEquals(
        ImmutableHashSet.of("latest", "tag1", "tag2"), buildConfiguration.getAllTargetImageTags());
    Assert.assertEquals("toolName", buildConfiguration.getToolName());
  }

  /** Verify that an internally-created ExecutorService is shutdown. */
  [TestMethod]
  public void testContainerize_executorCreated() {
    JibContainerBuilder jibContainerBuilder =
        new JibContainerBuilder(RegistryImage.named("base/image"), spyBuildConfigurationBuilder)
            .setEntrypoint(Arrays.asList("entry", "point"))
            .setEnvironment(ImmutableDictionary.of("name", "value"))
            .setExposedPorts(ImmutableHashSet.of(Port.tcp(1234), Port.udp(5678)))
            .setLabels(ImmutableDictionary.of("key", "value"))
            .setProgramArguments(Arrays.asList("program", "arguments"))
            .setCreationTime(Instant.ofEpochMilli(1000))
            .setUser("user")
            .setWorkingDirectory(AbsoluteUnixPath.get("/working/directory"));

    Containerizer mockContainerizer = createMockContainerizer();

    jibContainerBuilder.containerize(mockContainerizer, Suppliers.ofInstance(mockExecutorService));

    Mockito.verify(mockExecutorService).shutdown();
  }

  /** Verify that a provided ExecutorService is not shutdown. */
  [TestMethod]
  public void testContainerize_configuredExecutor() {
    JibContainerBuilder jibContainerBuilder =
        new JibContainerBuilder(RegistryImage.named("base/image"), spyBuildConfigurationBuilder)
            .setEntrypoint(Arrays.asList("entry", "point"))
            .setEnvironment(ImmutableDictionary.of("name", "value"))
            .setExposedPorts(ImmutableHashSet.of(Port.tcp(1234), Port.udp(5678)))
            .setLabels(ImmutableDictionary.of("key", "value"))
            .setProgramArguments(Arrays.asList("program", "arguments"))
            .setCreationTime(Instant.ofEpochMilli(1000))
            .setUser("user")
            .setWorkingDirectory(AbsoluteUnixPath.get("/working/directory"));
    Containerizer mockContainerizer = createMockContainerizer();
    Mockito.when(mockContainerizer.getExecutorService())
        .thenReturn(Optional.of(mockExecutorService));

    jibContainerBuilder.containerize(
        mockContainerizer,
        () => {
          throw new AssertionError();
        });

    Mockito.verify(mockExecutorService, Mockito.never()).shutdown();
  }

  private Containerizer createMockContainerizer()
      {
    ImageReference targetImage = ImageReference.parse("target-image");
    Containerizer mockContainerizer = Mockito.mock(typeof(Containerizer));
    StepsRunner stepsRunner = Mockito.mock(typeof(StepsRunner));
    BuildResult mockBuildResult = Mockito.mock(typeof(BuildResult));

    Mockito.when(mockContainerizer.getImageConfiguration())
        .thenReturn(ImageConfiguration.builder(targetImage).build());
    Mockito.when(mockContainerizer.createStepsRunner(Mockito.any(typeof(BuildConfiguration))))
        .thenReturn(stepsRunner);
    Mockito.when(stepsRunner.run()).thenReturn(mockBuildResult);
    Mockito.when(mockBuildResult.getImageDigest())
        .thenReturn(
            DescriptorDigest.fromHash(
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
    Mockito.when(mockBuildResult.getImageId())
        .thenReturn(
            DescriptorDigest.fromHash(
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));

    Mockito.when(mockContainerizer.getAdditionalTags()).thenReturn(Collections.emptySet());
    Mockito.when(mockContainerizer.getBaseImageLayersCacheDirectory()).thenReturn(Paths.get("/"));
    Mockito.when(mockContainerizer.getApplicationLayersCacheDirectory()).thenReturn(Paths.get("/"));
    Mockito.when(mockContainerizer.getAllowInsecureRegistries()).thenReturn(false);
    Mockito.when(mockContainerizer.getToolName()).thenReturn("mocktool");
    Mockito.when(mockContainerizer.getExecutorService()).thenReturn(Optional.empty());
    Mockito.when(mockContainerizer.buildEventHandlers()).thenReturn(EventHandlers.NONE);
    return mockContainerizer;
  }
}
}
