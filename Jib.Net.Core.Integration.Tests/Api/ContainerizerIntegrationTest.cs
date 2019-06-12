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

using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.@event.events;
using com.google.cloud.tools.jib.@event.progress;
using com.google.cloud.tools.jib.filesystem;
using com.google.cloud.tools.jib.registry;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace com.google.cloud.tools.jib.api {































// TODO: now it looks like we can move everything here into JibIntegrationTest.
/** Integration tests for {@link Containerizer}. */
public class ContainerizerIntegrationTest {

  /**
   * Helper class to hold a {@link ProgressEventHandler} and verify that it handles a full progress.
   */
  private class ProgressChecker {

            public readonly ProgressEventHandler progressEventHandler;

    private double lastProgress = 0.0;
    private volatile bool areTasksFinished = false;
            public ProgressChecker()
            {
                progressEventHandler =
           new ProgressEventHandler(
               update => {
                   lastProgress = update.getProgress();
                   areTasksFinished = update.getUnfinishedLeafTasks().isEmpty();
               });

            }

    public void checkCompletion() {
      Assert.AreEqual(1.0, lastProgress, DOUBLE_ERROR_MARGIN);
      Assert.IsTrue(areTasksFinished);
    }
  }

  [ClassRule] public static readonly LocalRegistry localRegistry = new LocalRegistry(5000);

        private static readonly Logger logger = new Logger(TestContext.Out);
  private static readonly string DISTROLESS_DIGEST =
      "sha256:f488c213f278bc5f9ffe3ddf30c5dbb2303a15a74146b738d12453088e662880";
  private static readonly double DOUBLE_ERROR_MARGIN = 1e-10;

  public static ImmutableArray<LayerConfiguration> fakeLayerConfigurations;

  [OneTimeSetUp]
  public static void setUp() {
    fakeLayerConfigurations =
        ImmutableArray.Create(
            makeLayerConfiguration("core/application/dependencies", "/app/libs/"),
            makeLayerConfiguration("core/application/resources", "/app/resources/"),
            makeLayerConfiguration("core/application/classes", "/app/classes/"));
  }

  /**
   * Lists the files in the {@code resourcePath} resources directory and builds a {@link
   * LayerConfiguration} from those files.
   */
  private static LayerConfiguration makeLayerConfiguration(
      string resourcePath, string pathInContainer) {
            IEnumerable<SystemPath> fileStream =
                Files.list(Paths.get(Resources.getResource(resourcePath).toURI()));
            {
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
            StringAssert.Contains(
                dockerContainerConfig,
                    "            \"ExposedPorts\": {\n"
                        + "                \"1000/tcp\": {},\n"
                        + "                \"2000/tcp\": {},\n"
                        + "                \"2001/tcp\": {},\n"
                        + "                \"2002/tcp\": {},\n"
                        + "                \"3000/udp\": {}");
    StringAssert.Contains(
        dockerContainerConfig,
            "            \"Labels\": {\n"
                + "                \"key1\": \"value1\",\n"
                + "                \"key2\": \"value2\"\n"
                + "            }");
    string dockerConfigEnv =
        new Command("docker", "inspect", "-f", "{{.Config.Env}}", imageReference).run();
    StringAssert.Contains(dockerConfigEnv, "env1=envvalue1");
;
    StringAssert.Contains(dockerConfigEnv, "env2=envvalue2");
;
    string history = new Command("docker", "history", imageReference).run();
    StringAssert.Contains(history, "jib-integration-test");
;
    StringAssert.Contains(history, "bazel build ...");
;
  }

  private static void assertLayerSizer(int expected, string imageReference)
      {
    Command command =
        new Command("docker", "inspect", "-f", "{{join .RootFS.Layers \",\"}}", imageReference);
    string layers = command.run().trim();
    Assert.AreEqual(expected, Splitter.on(",").splitToList(layers).size());
  }

  [Rule] public readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

  private readonly ProgressChecker progressChecker = new ProgressChecker();

  [Test]
  public void testSteps_forBuildToDockerRegistry()
      {
            Stopwatch s = Stopwatch.StartNew();
    JibContainer image1 =
        buildRegistryImage(
            ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
            ImageReference.of("localhost:5000", "testimage", "testtag"),
            Collections.emptyList<string>());

    progressChecker.checkCompletion();

    logger.info("Initial build time: " + ((s.Elapsed)));
            s.Restart();
    JibContainer image2 =
        buildRegistryImage(
            ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
            ImageReference.of("localhost:5000", "testimage", "testtag"),
            Collections.emptyList<string>());

    logger.info("Secondary build time: " + s.Elapsed);

    Assert.AreEqual(image1, image2);

    string imageReference = "localhost:5000/testimage:testtag";
    localRegistry.pull(imageReference);
    assertDockerInspect(imageReference);
    assertLayerSizer(7, imageReference);
    Assert.AreEqual(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).run());

    string imageReferenceByDigest = "localhost:5000/testimage@" + image1.getDigest();
    localRegistry.pull(imageReferenceByDigest);
    assertDockerInspect(imageReferenceByDigest);
    Assert.AreEqual(
        "Hello, world. An argument.\n",
        new Command("docker", "run", "--rm", imageReferenceByDigest).run());
  }

  [Test]
  public void testSteps_forBuildToDockerRegistry_multipleTags()
      {
    buildRegistryImage(
        ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
        ImageReference.of("localhost:5000", "testimage", "testtag"),
        Arrays.asList("testtag2", "testtag3"));

    string imageReference = "localhost:5000/testimage:testtag";
    localRegistry.pull(imageReference);
    assertDockerInspect(imageReference);
    Assert.AreEqual(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).run());

    string imageReference2 = "localhost:5000/testimage:testtag2";
    localRegistry.pull(imageReference2);
    assertDockerInspect(imageReference2);
    Assert.AreEqual(
        "Hello, world. An argument.\n",
        new Command("docker", "run", "--rm", imageReference2).run());

    string imageReference3 = "localhost:5000/testimage:testtag3";
    localRegistry.pull(imageReference3);
    assertDockerInspect(imageReference3);
    Assert.AreEqual(
        "Hello, world. An argument.\n",
        new Command("docker", "run", "--rm", imageReference3).run());
  }

  [Test]
  public void tesBuildToDockerRegistry_dockerHubBaseImage()
      {
    buildRegistryImage(
        ImageReference.parse("openjdk:8-jre-alpine"),
        ImageReference.of("localhost:5000", "testimage", "testtag"),
        Collections.emptyList<string>());

    string imageReference = "localhost:5000/testimage:testtag";
    new Command("docker", "pull", imageReference).run();
    Assert.AreEqual(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).run());
  }

  [Test]
  public void testBuildToDockerDaemon()
      {
    buildDockerDaemonImage(
        ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
        ImageReference.of(null, "testdocker", null),
        Collections.emptyList<string>());

    progressChecker.checkCompletion();

    assertDockerInspect("testdocker");
    assertLayerSizer(7, "testdocker");
    Assert.AreEqual(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", "testdocker").run());
  }

  [Test]
  public void testBuildToDockerDaemon_multipleTags()
      {
    string imageReference = "testdocker";
    buildDockerDaemonImage(
        ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
        ImageReference.of(null, imageReference, null),
        Arrays.asList("testtag2", "testtag3"));

    assertDockerInspect(imageReference);
    Assert.AreEqual(
        "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).run());
    assertDockerInspect(imageReference + ":testtag2");
    Assert.AreEqual(
        "Hello, world. An argument.\n",
        new Command("docker", "run", "--rm", imageReference + ":testtag2").run());
    assertDockerInspect(imageReference + ":testtag3");
    Assert.AreEqual(
        "Hello, world. An argument.\n",
        new Command("docker", "run", "--rm", imageReference + ":testtag3").run());
  }

  [Test]
  public void testBuildTarball()
      {
    SystemPath outputPath = temporaryFolder.newFolder().toPath().resolve("test.tar");
    buildTarImage(
        ImageReference.of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
        ImageReference.of(null, "testtar", null),
        outputPath,
        Collections.emptyList<string>());

    progressChecker.checkCompletion();

    new Command("docker", "load", "--input", outputPath.toString()).run();
    assertLayerSizer(7, "testtar");
    Assert.AreEqual(
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
      SystemPath outputPath,
      List<string> additionalTags)
      {
    return buildImage(
        baseImage,
        Containerizer.to(TarImage.named(targetImage).saveTo(outputPath)),
        additionalTags);
  }

  private JibContainer buildImage(
      ImageReference baseImage, Containerizer containerizer, IList<string> additionalTags)
      {
    JibContainerBuilder containerBuilder =
        Jib.from(baseImage)
            .setEntrypoint(
                Arrays.asList(
                    "java", "-cp", "/app/resources:/app/classes:/app/libs/*", "HelloWorld"))
            .setProgramArguments(Collections.singletonList("An argument."))
            .setEnvironment(ImmutableDic.of("env1", "envvalue1", "env2", "envvalue2"))
            .setExposedPorts(Ports.parse(Arrays.asList("1000", "2000-2002/tcp", "3000/udp")))
            .setLabels(ImmutableDic.of("key1", "value1", "key2", "value2"))
            .setLayers(fakeLayerConfigurations);

    SystemPath cacheDirectory = temporaryFolder.newFolder().toPath();
    containerizer
        .setBaseImageLayersCache(cacheDirectory)
        .setApplicationLayersCache(cacheDirectory)
        .setAllowInsecureRegistries(true)
        .setToolName("jib-integration-test")
        .addEventHandler<ProgressEvent>(progressChecker.progressEventHandler);
    additionalTags.forEach(containerizer.withAdditionalTag);

    return containerBuilder.containerize(containerizer);
  }
}
}
