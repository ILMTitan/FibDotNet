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














/** Tests for {@link JavaContainerBuilder}. */
public class JavaContainerBuilderTest {

  /** Gets a resource file as a {@link Path}. */
  private static Path getResource(string directory) {
    return Paths.get(Resources.getResource(directory).toURI());
  }

  /** Gets the extraction paths in the specified layer of a give {@link BuildConfiguration}. */
  private static List<AbsoluteUnixPath> getExtractionPaths(
      BuildConfiguration buildConfiguration, string layerName) {
    return buildConfiguration
        .getLayerConfigurations()
        .stream()
        .filter(layerConfiguration => layerConfiguration.getName().equals(layerName))
        .findFirst()
        .map(
            layerConfiguration =>
                layerConfiguration
                    .getLayerEntries()
                    .stream()
                    .map(LayerEntry.getExtractionPath)
                    .collect(Collectors.toList()))
        .orElse(ImmutableList.of());
  }

  [TestMethod]
  public void testToJibContainerBuilder_all()
      {
    BuildConfiguration buildConfiguration =
        JavaContainerBuilder.fromDistroless()
            .setAppRoot("/hello")
            .addResources(getResource("core/application/resources"))
            .addClasses(getResource("core/application/classes"))
            .addDependencies(
                getResource("core/application/dependencies/dependency-1.0.0.jar"),
                getResource("core/application/dependencies/more/dependency-1.0.0.jar"),
                getResource("core/application/dependencies/libraryA.jar"),
                getResource("core/application/dependencies/libraryB.jar"),
                getResource("core/application/snapshot-dependencies/dependency-1.0.0-SNAPSHOT.jar"))
            .addToClasspath(getResource("core/fileA"), getResource("core/fileB"))
            .setClassesDestination(RelativeUnixPath.get("different-classes"))
            .setResourcesDestination(RelativeUnixPath.get("different-resources"))
            .setDependenciesDestination(RelativeUnixPath.get("different-libs"))
            .setOthersDestination(RelativeUnixPath.get("different-classpath"))
            .addJvmFlags("-xflag1", "-xflag2")
            .setMainClass("HelloWorld")
            .toContainerBuilder()
            .toBuildConfiguration(
                Containerizer.to(RegistryImage.named("hello")),
                MoreExecutors.newDirectExecutorService());

    // Check entrypoint
    ContainerConfiguration containerConfiguration = buildConfiguration.getContainerConfiguration();
    Assert.assertNotNull(containerConfiguration);
    Assert.assertEquals(
        ImmutableList.of(
            "java",
            "-xflag1",
            "-xflag2",
            "-cp",
            "/hello/different-resources:/hello/different-classes:/hello/different-libs/*:/hello/different-classpath",
            "HelloWorld"),
        containerConfiguration.getEntrypoint());

    // Check dependencies
    List<AbsoluteUnixPath> expectedDependencies =
        ImmutableList.of(
            AbsoluteUnixPath.get("/hello/different-libs/dependency-1.0.0-770.jar"),
            AbsoluteUnixPath.get("/hello/different-libs/dependency-1.0.0-200.jar"),
            AbsoluteUnixPath.get("/hello/different-libs/libraryA.jar"),
            AbsoluteUnixPath.get("/hello/different-libs/libraryB.jar"));
    Assert.assertEquals(
        expectedDependencies, getExtractionPaths(buildConfiguration, "dependencies"));

    // Check snapshots
    List<AbsoluteUnixPath> expectedSnapshotDependencies =
        ImmutableList.of(
            AbsoluteUnixPath.get("/hello/different-libs/dependency-1.0.0-SNAPSHOT.jar"));
    Assert.assertEquals(
        expectedSnapshotDependencies,
        getExtractionPaths(buildConfiguration, "snapshot dependencies"));

    // Check resources
    List<AbsoluteUnixPath> expectedResources =
        ImmutableList.of(
            AbsoluteUnixPath.get("/hello/different-resources/resourceA"),
            AbsoluteUnixPath.get("/hello/different-resources/resourceB"),
            AbsoluteUnixPath.get("/hello/different-resources/world"));
    Assert.assertEquals(expectedResources, getExtractionPaths(buildConfiguration, "resources"));

    // Check classes
    List<AbsoluteUnixPath> expectedClasses =
        ImmutableList.of(
            AbsoluteUnixPath.get("/hello/different-classes/typeof(HelloWorld)"),
            AbsoluteUnixPath.get("/hello/different-classes/typeof(some)"));
    Assert.assertEquals(expectedClasses, getExtractionPaths(buildConfiguration, "classes"));

    // Check additional classpath files
    List<AbsoluteUnixPath> expectedOthers =
        ImmutableList.of(
            AbsoluteUnixPath.get("/hello/different-classpath/fileA"),
            AbsoluteUnixPath.get("/hello/different-classpath/fileB"));
    Assert.assertEquals(expectedOthers, getExtractionPaths(buildConfiguration, "extra files"));
  }

  [TestMethod]
  public void testToJibContainerBuilder_missingAndMultipleAdds()
      {
    BuildConfiguration buildConfiguration =
        JavaContainerBuilder.fromDistroless()
            .addDependencies(getResource("core/application/dependencies/libraryA.jar"))
            .addDependencies(getResource("core/application/dependencies/libraryB.jar"))
            .addDependencies(
                getResource("core/application/snapshot-dependencies/dependency-1.0.0-SNAPSHOT.jar"))
            .addClasses(getResource("core/application/classes/"))
            .addClasses(getResource("core/class-finder-tests/extension"))
            .setMainClass("HelloWorld")
            .toContainerBuilder()
            .toBuildConfiguration(
                Containerizer.to(RegistryImage.named("hello")),
                MoreExecutors.newDirectExecutorService());

    // Check entrypoint
    ContainerConfiguration containerConfiguration = buildConfiguration.getContainerConfiguration();
    Assert.assertNotNull(containerConfiguration);
    Assert.assertEquals(
        ImmutableList.of("java", "-cp", "/app/libs/*:/app/classes", "HelloWorld"),
        containerConfiguration.getEntrypoint());

    // Check dependencies
    List<AbsoluteUnixPath> expectedDependencies =
        ImmutableList.of(
            AbsoluteUnixPath.get("/app/libs/libraryA.jar"),
            AbsoluteUnixPath.get("/app/libs/libraryB.jar"));
    Assert.assertEquals(
        expectedDependencies, getExtractionPaths(buildConfiguration, "dependencies"));

    // Check snapshots
    List<AbsoluteUnixPath> expectedSnapshotDependencies =
        ImmutableList.of(AbsoluteUnixPath.get("/app/libs/dependency-1.0.0-SNAPSHOT.jar"));
    Assert.assertEquals(
        expectedSnapshotDependencies,
        getExtractionPaths(buildConfiguration, "snapshot dependencies"));

    // Check classes
    List<AbsoluteUnixPath> expectedClasses =
        ImmutableList.of(
            AbsoluteUnixPath.get("/app/classes/typeof(HelloWorld)"),
            AbsoluteUnixPath.get("/app/classes/typeof(some)"),
            AbsoluteUnixPath.get("/app/classes/main/"),
            AbsoluteUnixPath.get("/app/classes/main/typeof(MainClass)"),
            AbsoluteUnixPath.get("/app/classes/pack/"),
            AbsoluteUnixPath.get("/app/classes/pack/typeof(Apple)"),
            AbsoluteUnixPath.get("/app/classes/pack/typeof(Orange)"));
    Assert.assertEquals(expectedClasses, getExtractionPaths(buildConfiguration, "classes"));

    // Check empty layers
    Assert.assertEquals(ImmutableList.of(), getExtractionPaths(buildConfiguration, "resources"));
    Assert.assertEquals(ImmutableList.of(), getExtractionPaths(buildConfiguration, "extra files"));
  }

  [TestMethod]
  public void testToJibContainerBuilder_setAppRootLate()
      {
    BuildConfiguration buildConfiguration =
        JavaContainerBuilder.fromDistroless()
            .addClasses(getResource("core/application/classes"))
            .addResources(getResource("core/application/resources"))
            .addDependencies(getResource("core/application/dependencies/libraryA.jar"))
            .addToClasspath(getResource("core/fileA"))
            .setAppRoot("/different")
            .setMainClass("HelloWorld")
            .toContainerBuilder()
            .toBuildConfiguration(
                Containerizer.to(RegistryImage.named("hello")),
                MoreExecutors.newDirectExecutorService());

    // Check entrypoint
    ContainerConfiguration containerConfiguration = buildConfiguration.getContainerConfiguration();
    Assert.assertNotNull(containerConfiguration);
    Assert.assertEquals(
        ImmutableList.of(
            "java",
            "-cp",
            "/different/classes:/different/resources:/different/libs/*:/different/classpath",
            "HelloWorld"),
        containerConfiguration.getEntrypoint());

    // Check classes
    List<AbsoluteUnixPath> expectedClasses =
        ImmutableList.of(
            AbsoluteUnixPath.get("/different/classes/typeof(HelloWorld)"),
            AbsoluteUnixPath.get("/different/classes/typeof(some)"));
    Assert.assertEquals(expectedClasses, getExtractionPaths(buildConfiguration, "classes"));

    // Check resources
    List<AbsoluteUnixPath> expectedResources =
        ImmutableList.of(
            AbsoluteUnixPath.get("/different/resources/resourceA"),
            AbsoluteUnixPath.get("/different/resources/resourceB"),
            AbsoluteUnixPath.get("/different/resources/world"));
    Assert.assertEquals(expectedResources, getExtractionPaths(buildConfiguration, "resources"));

    // Check dependencies
    List<AbsoluteUnixPath> expectedDependencies =
        ImmutableList.of(AbsoluteUnixPath.get("/different/libs/libraryA.jar"));
    Assert.assertEquals(
        expectedDependencies, getExtractionPaths(buildConfiguration, "dependencies"));

    Assert.assertEquals(expectedClasses, getExtractionPaths(buildConfiguration, "classes"));

    // Check additional classpath files
    List<AbsoluteUnixPath> expectedOthers =
        ImmutableList.of(AbsoluteUnixPath.get("/different/classpath/fileA"));
    Assert.assertEquals(expectedOthers, getExtractionPaths(buildConfiguration, "extra files"));
  }

  [TestMethod]
  public void testToJibContainerBuilder_mainClassNull()
      {
    BuildConfiguration buildConfiguration =
        JavaContainerBuilder.fromDistroless()
            .addClasses(getResource("core/application/classes/"))
            .toContainerBuilder()
            .toBuildConfiguration(
                Containerizer.to(RegistryImage.named("hello")),
                MoreExecutors.newDirectExecutorService());
    Assert.assertNotNull(buildConfiguration.getContainerConfiguration());
    Assert.assertNull(buildConfiguration.getContainerConfiguration().getEntrypoint());

    try {
      JavaContainerBuilder.fromDistroless().addJvmFlags("-flag1", "-flag2").toContainerBuilder();
      Assert.fail();

    } catch (IllegalStateException ex) {
      Assert.assertEquals(
          "Failed to construct entrypoint on JavaContainerBuilder; jvmFlags were set, but "
              + "mainClass is null. Specify the main class using "
              + "JavaContainerBuilder#setMainClass(string), or consider using a "
              + "jib.frontend.MainClassFinder to infer the main class.",
          ex.getMessage());
    }
  }

  [TestMethod]
  public void testToJibContainerBuilder_classpathEmpty() {
    try {
      JavaContainerBuilder.fromDistroless().setMainClass("Hello").toContainerBuilder();
      Assert.fail();

    } catch (IllegalStateException ex) {
      Assert.assertEquals(
          "Failed to construct entrypoint because no files were added to the JavaContainerBuilder",
          ex.getMessage());
    }
  }
}
}
