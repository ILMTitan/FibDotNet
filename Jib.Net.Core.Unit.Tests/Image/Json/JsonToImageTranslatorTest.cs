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

namespace com.google.cloud.tools.jib.image.json {


























/** Tests for {@link JsonToImageTranslator}. */
public class JsonToImageTranslatorTest {

  [TestMethod]
  public void testToImage_v21()
      {
    // Loads the JSON string.
    SystemPath jsonFile =
        Paths.get(getClass().getClassLoader().getResource("core/json/v21manifest.json").toURI());

    // Deserializes into a manifest JSON object.
    V21ManifestTemplate manifestTemplate =
        JsonTemplateMapper.readJsonFromFile(jsonFile, typeof(V21ManifestTemplate));

    Image image = JsonToImageTranslator.toImage(manifestTemplate);

    IList<Layer> layers = image.getLayers();
    Assert.assertEquals(2, layers.size());
    Assert.assertEquals(
        DescriptorDigest.fromDigest(
            "sha256:5bd451067f9ab05e97cda8476c82f86d9b69c2dffb60a8ad2fe3723942544ab3"),
        layers.get(0).getBlobDescriptor().getDigest());
    Assert.assertEquals(
        DescriptorDigest.fromDigest(
            "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
        layers.get(1).getBlobDescriptor().getDigest());
  }

  [TestMethod]
  public void testToImage_v22()
      {
    testToImage_buildable("core/json/v22manifest.json", typeof(V22ManifestTemplate));
  }

  [TestMethod]
  public void testToImage_oci()
      {
    testToImage_buildable("core/json/ocimanifest.json", typeof(OCIManifestTemplate));
  }

  [TestMethod]
  public void testPortMapToList() {
    ImmutableSortedDictionary<string, IDictionary<object, object>> input =
        ImmutableSortedDictionary.of(
            "1000",
            ImmutableDictionary.of(),
            "2000/tcp",
            ImmutableDictionary.of(),
            "3000/udp",
            ImmutableDictionary.of());
    ImmutableHashSet<Port> expected = ImmutableHashSet.of(Port.tcp(1000), Port.tcp(2000), Port.udp(3000));
    Assert.assertEquals(expected, JsonToImageTranslator.portMapToSet(input));

    ImmutableArray<IDictionary<string, IDictionary<object, object>>> badInputs =
        ImmutableArray.Create(
            ImmutableDictionary.of("abc", ImmutableDictionary.of()),
            ImmutableDictionary.of("1000-2000", ImmutableDictionary.of()),
            ImmutableDictionary.of("/udp", ImmutableDictionary.of()),
            ImmutableDictionary.of("123/xxx", ImmutableDictionary.of()));
    foreach (IDictionary<string, IDictionary<object, object>> badInput in badInputs)
    {
      try {
        JsonToImageTranslator.portMapToSet(badInput);
        Assert.fail();
      } catch (BadContainerConfigurationFormatException ignored) {
      }
    }
  }

  [TestMethod]
  public void testVolumeMapToList() {
    ImmutableSortedDictionary<string, IDictionary<object, object>> input =
        ImmutableSortedDictionary.of(
            "/var/job-result-data", ImmutableDictionary.of(), "/var/log/my-app-logs", ImmutableDictionary.of());
    ImmutableHashSet<AbsoluteUnixPath> expected =
        ImmutableHashSet.of(
            AbsoluteUnixPath.get("/var/job-result-data"),
            AbsoluteUnixPath.get("/var/log/my-app-logs"));
    Assert.assertEquals(expected, JsonToImageTranslator.volumeMapToSet(input));

    ImmutableArray<IDictionary<string, IDictionary<object, object>>> badInputs =
        ImmutableArray.Create(
            ImmutableDictionary.of("var/job-result-data", ImmutableDictionary.of()),
            ImmutableDictionary.of("log", ImmutableDictionary.of()),
            ImmutableDictionary.of("C:/udp", ImmutableDictionary.of()));
    foreach (IDictionary<string, IDictionary<object, object>> badInput in badInputs)
    {
      try {
        JsonToImageTranslator.volumeMapToSet(badInput);
        Assert.fail();
      } catch (BadContainerConfigurationFormatException ignored) {
      }
    }
  }

  [TestMethod]
  public void testJsonToImageTranslatorRegex() {
    assertGoodEnvironmentPattern("NAME=VALUE", "NAME", "VALUE");
    assertGoodEnvironmentPattern("A1203921=www=ww", "A1203921", "www=ww");
    assertGoodEnvironmentPattern("&*%(&#$(*@(%&@$*$(=", "&*%(&#$(*@(%&@$*$(", "");
    assertGoodEnvironmentPattern("m_a_8943=100", "m_a_8943", "100");
    assertGoodEnvironmentPattern("A_B_C_D=*****", "A_B_C_D", "*****");

    assertBadEnvironmentPattern("=================");
    assertBadEnvironmentPattern("A_B_C");
  }

  private void assertGoodEnvironmentPattern(
      string input, string expectedName, string expectedValue) {
    Match matcher = JsonToImageTranslator.ENVIRONMENT_PATTERN.matcher(input);
    Assert.assertTrue(matcher.matches());
    Assert.assertEquals(expectedName, matcher.group("name"));
    Assert.assertEquals(expectedValue, matcher.group("value"));
  }

  private void assertBadEnvironmentPattern(string input) {
    Match matcher = JsonToImageTranslator.ENVIRONMENT_PATTERN.matcher(input);
    Assert.assertFalse(matcher.matches());
  }

  private void testToImage_buildable<T>(
      string jsonFilename, Class<T> manifestTemplateClass) where T : BuildableManifestTemplate {
    // Loads the container configuration JSON.
    SystemPath containerConfigurationJsonFile =
        Paths.get(
            getClass().getClassLoader().getResource("core/json/containerconfig.json").toURI());
    ContainerConfigurationTemplate containerConfigurationTemplate =
        JsonTemplateMapper.readJsonFromFile(
            containerConfigurationJsonFile, typeof(ContainerConfigurationTemplate));

    // Loads the manifest JSON.
    SystemPath manifestJsonFile =
        Paths.get(getClass().getClassLoader().getResource(jsonFilename).toURI());
    T manifestTemplate =
        JsonTemplateMapper.readJsonFromFile(manifestJsonFile, manifestTemplateClass);

    Image image = JsonToImageTranslator.toImage(manifestTemplate, containerConfigurationTemplate);

    IList<Layer> layers = image.getLayers();
    Assert.assertEquals(1, layers.size());
    Assert.assertEquals(
        new BlobDescriptor(
            1000000,
            DescriptorDigest.fromDigest(
                "sha256:4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236")),
        layers.get(0).getBlobDescriptor());
    Assert.assertEquals(
        DescriptorDigest.fromDigest(
            "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
        layers.get(0).getDiffId());
    Assert.assertEquals(
        ImmutableArray.Create(
            HistoryEntry.builder()
                .setCreationTimestamp(Instant.EPOCH)
                .setAuthor("Bazel")
                .setCreatedBy("bazel build ...")
                .setEmptyLayer(true)
                .build(),
            HistoryEntry.builder()
                .setCreationTimestamp(Instant.FromUnixTimeSeconds(20))
                .setAuthor("Jib")
                .setCreatedBy("jib")
                .build()),
        image.getHistory());
    Assert.assertEquals(Instant.FromUnixTimeSeconds(20), image.getCreated());
    Assert.assertEquals(Arrays.asList("some", "entrypoint", "command"), image.getEntrypoint());
    Assert.assertEquals(ImmutableDictionary.of("VAR1", "VAL1", "VAR2", "VAL2"), image.getEnvironment());
    Assert.assertEquals("/some/workspace", image.getWorkingDirectory());
    Assert.assertEquals(
        ImmutableHashSet.of(Port.tcp(1000), Port.tcp(2000), Port.udp(3000)), image.getExposedPorts());
    Assert.assertEquals(
        ImmutableHashSet.of(
            AbsoluteUnixPath.get("/var/job-result-data"),
            AbsoluteUnixPath.get("/var/log/my-app-logs")),
        image.getVolumes());
    Assert.assertEquals("tomcat", image.getUser());
    Assert.assertEquals("value1", image.getLabels().get("key1"));
    Assert.assertEquals("value2", image.getLabels().get("key2"));
    Assert.assertEquals(2, image.getLabels().size());
  }
}
}
