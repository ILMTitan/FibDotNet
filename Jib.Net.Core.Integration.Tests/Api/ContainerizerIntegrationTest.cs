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































// TODO: now it looks like we can move everything here into JibIntegrationTest.
/** Integration tests for {@link Containerizer}. */
public class ContainerizerIntegrationTest {

  /**
   * Helper class to hold a {@link ProgressEventHandler} and verify that it handles a full progress.
   */
  private class ProgressChecker {

    private readonly ProgressEventHandler progressEventHandler =
        new ProgressEventHandler(
            update => {
              lastProgress = update.getProgress();
              areTasksFinished = update.getUnfinishedLeafTasks().isEmpty();
            });

    private volatile double lastProgress = 0.0;
    private volatile bool areTasksFinished = false;

    private void checkCompletion() {
      Assert.assertEquals(1.0, lastProgress, DOUBLE_ERROR_MARGIN);
      Assert.assertTrue(areTasksFinished);
    }
  }

  [ClassRule] public static readonly LocalRegistry localRegistry = new LocalRegistry(5000);

  private static readonly ExecutorService executorService = Executors.newCachedThreadPool();
  private static readonly Logger logger = LoggerFactory.getLogger(typeof(ContainerizerIntegrationTest));
  private static readonly string DISTROLESS_DIGEST =
      "sha256:f488c213f278bc5f9ffe3ddf30c5dbb2303a15a74146b738d12453088e662880";
  private static readonly double DOUBLE_ERROR_MARGIN = 1e-10;

  public static ImmutableList<LayerConfiguration> fakeLayerConfigurations;

  [ClassInitialize]
  public static void setUp() {
    fakeLayerConfigurations =
        ImmutableList.of(
            makeLayerConfiguration("core/application/dependencies", "/app/libs/"),
            makeLayerConfiguration("core/application/resources", "/app/resources/"),
            makeLayerConfiguration("core/application/classes", "/app/classes/"));
  }

  [ClassCleanup]
  public static void cleanUp() {
    executorService.shutdown();
  }

  /**
   * Lists the files in the {@code resourcePath} resources directory and builds a {@link
   * LayerConfiguration} from those files.
   */
  private static LayerConfiguration makeLayerConfiguration(
      string resourcePath, string pathInContainer) {
    using (Stream<Path> fileStream =
        Files.list(Paths.get(Resources.getResource(resourcePath).toURI()))) {
      LayerConfiguration.Builder layerConfigurationBuilder = LayerConfiguration.builder();
      fileStream.forEach(
          sourceFile =>
              layerConfigurationBuilder.addEntry(
                  sourceFile, AbsoluteUnixPath.get(pathInContainer + sourceFile.getFileName())));
      return layerConfigurationBuilder.build();
    }
  }

  private static void assertDockerInspect(string imageReference)
      {
    string dockerContainerConfig = new Command("docker", "inspect", imageReference).run();
    Assert.assertThat(
        dockerContainerConfig,
        CoreMatchers.containsString(
            "            \"ExposedPorts\": {\n"
                + "                \"1000/tcp\": {},\n"
                + "                \"2000/tcp\": {},\n"
                + "                \"2001/tcp\": {},\n"
                + "                \"2002/tcp\": {},\n"
                + "                \"3000/udp\": {}"));
    Assert.assertThat(
        dockerContainerConfig,
        CoreMatchers.containsString(
            "            \"Labels\": {\n"
                + "                \"key1\": \"value1\",\n"
                + "                \"key2\": \"value2\"\n"
                + "            }"));
    string dockerConfigEnv =
        new Command("docker", "inspect", "-f", "{{.Config.Env}}", imageReference).run();
    Assert.assertThat(dockerConfigEnv, CoreMatchers.containsString("env1=envvalue1"));
    Assert.assertThat(dockerConfigEnv, CoreMatchers.containsString("env2=envvalue2"));
    string history = new Command("docker", "history", imageReference).run();
    Assert.assertThat(history, CoreMatchers.containsString("jib-integration-test"));
    Assert.assertThat(history, CoreMatchers.containsString("bazel build ..."));
  }

  private static void assertLayerSizer(int expected, string imageReference)
      {
    Command command =
        new Command("docker", "inspect", "-f", "{{join .RootFS.Layers \",\"}}", imageReference);
    string layers = command.run().trim();
    Assert.assertEquals(expected, Splitter.on(",").splitToList(layers).size());
  }

  [Rule] public readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

  private readonly ProgressChecker progressChecker = new ProgressChecker();

  [TestMethod]
  public void testSteps_forBuildToDockerRegistry()
      {
    long lastTime = System.nanoTime();
    JibContainer image1 =
        buildRegistryImage(
            ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
            ImageReference.of("localhost:5000", "testimage", "testtag"),
            Collections.emptyList());

    progressChecker.checkCompletion();

    logger.info("Initial build time: " + ((System.nanoTime() - lastTime) / 1_000_000));

    lastTime = System.nanoTime();
    JibContainer image2 =
        buildRegistryImage(
            ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
            ImageReference.of("localhost:5000", "testimage", "testtag"),
            Collections.emptyList());

    logger.info("Secondary build time: " + ((System.nanoTime() - lastTime) / 1_000_000));

    Assert.assertEquals(image1, image2);

    string imageReference = "localhost:5000/testimage:testtag";
    localRegistry.pull(imageReference);
    assertDockerInspect(imageReference);
    assertLayerSizer(7, imageReference);
    Assert.assertEquals(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).run());

    string imageReferenceByDigest = "localhost:5000/testimage@" + image1.getDigest();
    localRegistry.pull(imageReferenceByDigest);
    assertDockerInspect(imageReferenceByDigest);
    Assert.assertEquals(
        "Hello, world. An argument.\n",
        new Command("docker", "run", "--rm", imageReferenceByDigest).run());
  }

  [TestMethod]
  public void testSteps_forBuildToDockerRegistry_multipleTags()
      {
    buildRegistryImage(
        ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
        ImageReference.of("localhost:5000", "testimage", "testtag"),
        Arrays.asList("testtag2", "testtag3"));

    string imageReference = "localhost:5000/testimage:testtag";
    localRegistry.pull(imageReference);
    assertDockerInspect(imageReference);
    Assert.assertEquals(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).run());

    string imageReference2 = "localhost:5000/testimage:testtag2";
    localRegistry.pull(imageReference2);
    assertDockerInspect(imageReference2);
    Assert.assertEquals(
        "Hello, world. An argument.\n",
        new Command("docker", "run", "--rm", imageReference2).run());

    string imageReference3 = "localhost:5000/testimage:testtag3";
    localRegistry.pull(imageReference3);
    assertDockerInspect(imageReference3);
    Assert.assertEquals(
        "Hello, world. An argument.\n",
        new Command("docker", "run", "--rm", imageReference3).run());
  }

  [TestMethod]
  public void tesBuildToDockerRegistry_dockerHubBaseImage()
      {
    buildRegistryImage(
        ImageReference.parse("openjdk:8-jre-alpine"),
        ImageReference.of("localhost:5000", "testimage", "testtag"),
        Collections.emptyList());

    string imageReference = "localhost:5000/testimage:testtag";
    new Command("docker", "pull", imageReference).run();
    Assert.assertEquals(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).run());
  }

  [TestMethod]
  public void testBuildToDockerDaemon()
      {
    buildDockerDaemonImage(
        ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
        ImageReference.of(null, "testdocker", null),
        Collections.emptyList());

    progressChecker.checkCompletion();

    assertDockerInspect("testdocker");
    assertLayerSizer(7, "testdocker");
    Assert.assertEquals(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", "testdocker").run());
  }

  [TestMethod]
  public void testBuildToDockerDaemon_multipleTags()
      {
    string imageReference = "testdocker";
    buildDockerDaemonImage(
        ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
        ImageReference.of(null, imageReference, null),
        Arrays.asList("testtag2", "testtag3"));

    assertDockerInspect(imageReference);
    Assert.assertEquals(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).run());
    assertDockerInspect(imageReference + ":testtag2");
    Assert.assertEquals(
        "Hello, world. An argument.\n",
        new Command("docker", "run", "--rm", imageReference + ":testtag2").run());
    assertDockerInspect(imageReference + ":testtag3");
    Assert.assertEquals(
        "Hello, world. An argument.\n",
        new Command("docker", "run", "--rm", imageReference + ":testtag3").run());
  }

  [TestMethod]
  public void testBuildTarball()
      {
    Path outputPath = temporaryFolder.newFolder().toPath().resolve("test.tar");
    buildTarImage(
        ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
        ImageReference.of(null, "testtar", null),
        outputPath,
        Collections.emptyList());

    progressChecker.checkCompletion();

    new Command("docker", "load", "--input", outputPath.toString()).run();
    assertLayerSizer(7, "testtar");
    Assert.assertEquals(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", "testtar").run());
  }

  private JibContainer buildRegistryImage(
      ImageReference baseImage, ImageReference targetImage, List<string> additionalTags)
      {
    return buildImage(
        baseImage, Containerizer.to(RegistryImage.named(targetImage)), additionalTags);
  }

  private JibContainer buildDockerDaemonImage(
      ImageReference baseImage, ImageReference targetImage, List<string> additionalTags)
      {
    return buildImage(
        baseImage, Containerizer.to(DockerDaemonImage.named(targetImage)), additionalTags);
  }

  private JibContainer buildTarImage(
      ImageReference baseImage,
      ImageReference targetImage,
      Path outputPath,
      List<string> additionalTags)
      {
    return buildImage(
        baseImage,
        Containerizer.to(TarImage.named(targetImage).saveTo(outputPath)),
        additionalTags);
  }

  private JibContainer buildImage(
      ImageReference baseImage, Containerizer containerizer, List<string> additionalTags)
      {
    JibContainerBuilder containerBuilder =
        Jib.from(baseImage)
            .setEntrypoint(
                Arrays.asList(
                    "java", "-cp", "/app/resources:/app/classes:/app/libs/*", "HelloWorld"))
            .setProgramArguments(Collections.singletonList("An argument."))
            .setEnvironment(ImmutableMap.of("env1", "envvalue1", "env2", "envvalue2"))
            .setExposedPorts(Ports.parse(Arrays.asList("1000", "2000-2002/tcp", "3000/udp")))
            .setLabels(ImmutableMap.of("key1", "value1", "key2", "value2"))
            .setLayers(fakeLayerConfigurations);

    Path cacheDirectory = temporaryFolder.newFolder().toPath();
    containerizer
        .setBaseImageLayersCache(cacheDirectory)
        .setApplicationLayersCache(cacheDirectory)
        .setAllowInsecureRegistries(true)
        .setToolName("jib-integration-test")
        .setExecutorService(executorService)
        .addEventHandler(typeof(ProgressEvent), progressChecker.progressEventHandler);
    additionalTags.forEach(containerizer.withAdditionalTag);

    return containerBuilder.containerize(containerizer);
  }
}
}
