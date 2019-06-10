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


















/** Tests for {@link ContainerConfigurationTemplate}. */
public class ContainerConfigurationTemplateTest {

  [TestMethod]
  public void testToJson() {
    // Loads the expected JSON string.
    SystemPath jsonFile = Paths.get(Resources.getResource("core/json/containerconfig.json").toURI());
    string expectedJson = new string(Files.readAllBytes(jsonFile), StandardCharsets.UTF_8);

    // Creates the JSON object to serialize.
    ContainerConfigurationTemplate containerConfigJson = new ContainerConfigurationTemplate();

    containerConfigJson.setCreated("1970-01-01T00:00:20Z");
    containerConfigJson.setArchitecture("wasm");
    containerConfigJson.setOs("js");
    containerConfigJson.setContainerEnvironment(Arrays.asList("VAR1=VAL1", "VAR2=VAL2"));
    containerConfigJson.setContainerEntrypoint(Arrays.asList("some", "entrypoint", "command"));
    containerConfigJson.setContainerCmd(Arrays.asList("arg1", "arg2"));
    containerConfigJson.setContainerHealthCheckTest(Arrays.asList("CMD-SHELL", "/checkhealth"));
    containerConfigJson.setContainerHealthCheckInterval(3000000000L);
    containerConfigJson.setContainerHealthCheckTimeout(1000000000L);
    containerConfigJson.setContainerHealthCheckStartPeriod(2000000000L);
    containerConfigJson.setContainerHealthCheckRetries(3);
    containerConfigJson.setContainerExposedPorts(
        ImmutableSortedDictionary.of(
            "1000/tcp",
            ImmutableDictionary.of(),
            "2000/tcp",
            ImmutableDictionary.of(),
            "3000/udp",
            ImmutableDictionary.of()));
    containerConfigJson.setContainerLabels(ImmutableDictionary.of("key1", "value1", "key2", "value2"));
    containerConfigJson.setContainerVolumes(
        ImmutableDictionary.of(
            "/var/job-result-data", ImmutableDictionary.of(), "/var/log/my-app-logs", ImmutableDictionary.of()));
    containerConfigJson.setContainerWorkingDir("/some/workspace");
    containerConfigJson.setContainerUser("tomcat");

    containerConfigJson.addLayerDiffId(
        DescriptorDigest.fromDigest(
            "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"));
    containerConfigJson.addHistoryEntry(
        HistoryEntry.builder()
            .setCreationTimestamp(Instant.EPOCH)
            .setAuthor("Bazel")
            .setCreatedBy("bazel build ...")
            .setEmptyLayer(true)
            .build());
    containerConfigJson.addHistoryEntry(
        HistoryEntry.builder()
            .setCreationTimestamp(Instant.FromUnixTimeSeconds(20))
            .setAuthor("Jib")
            .setCreatedBy("jib")
            .build());

    // Serializes the JSON object.
    Assert.assertEquals(expectedJson, JsonTemplateMapper.toUtf8String(containerConfigJson));
  }

  [TestMethod]
  public void testFromJson() {
    // Loads the JSON string.
    SystemPath jsonFile = Paths.get(Resources.getResource("core/json/containerconfig.json").toURI());

    // Deserializes into a manifest JSON object.
    ContainerConfigurationTemplate containerConfigJson =
        JsonTemplateMapper.readJsonFromFile(jsonFile, typeof(ContainerConfigurationTemplate));

    Assert.assertEquals("1970-01-01T00:00:20Z", containerConfigJson.getCreated());
    Assert.assertEquals("wasm", containerConfigJson.getArchitecture());
    Assert.assertEquals("js", containerConfigJson.getOs());
    Assert.assertEquals(
        Arrays.asList("VAR1=VAL1", "VAR2=VAL2"), containerConfigJson.getContainerEnvironment());
    Assert.assertEquals(
        Arrays.asList("some", "entrypoint", "command"),
        containerConfigJson.getContainerEntrypoint());
    Assert.assertEquals(Arrays.asList("arg1", "arg2"), containerConfigJson.getContainerCmd());

    Assert.assertEquals(
        Arrays.asList("CMD-SHELL", "/checkhealth"), containerConfigJson.getContainerHealthTest());
    Assert.assertNotNull(containerConfigJson.getContainerHealthInterval());
    Assert.assertEquals(3000000000L, containerConfigJson.getContainerHealthInterval().longValue());
    Assert.assertNotNull(containerConfigJson.getContainerHealthTimeout());
    Assert.assertEquals(1000000000L, containerConfigJson.getContainerHealthTimeout().longValue());
    Assert.assertNotNull(containerConfigJson.getContainerHealthStartPeriod());
    Assert.assertEquals(
        2000000000L, containerConfigJson.getContainerHealthStartPeriod().longValue());
    Assert.assertNotNull(containerConfigJson.getContainerHealthRetries());
    Assert.assertEquals(3, containerConfigJson.getContainerHealthRetries().intValue());

    Assert.assertEquals(
        ImmutableDictionary.of("key1", "value1", "key2", "value2"),
        containerConfigJson.getContainerLabels());
    Assert.assertEquals("/some/workspace", containerConfigJson.getContainerWorkingDir());
    Assert.assertEquals(
        DescriptorDigest.fromDigest(
            "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
        containerConfigJson.getLayerDiffId(0));
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
        containerConfigJson.getHistory());
  }
}
}
