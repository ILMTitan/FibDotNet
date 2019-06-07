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
































/** Tests for {@link ImageToJsonTranslator}. */
public class ImageToJsonTranslatorTest {

  private ImageToJsonTranslator imageToJsonTranslator;

        private void setUp<T>(Class<T> imageFormat)where T:BuildableManifestTemplate
        {
    Image.Builder testImageBuilder =
        Image.builder(imageFormat)
            .setCreated(Instant.ofEpochSecond(20))
            .setArchitecture("wasm")
            .setOs("js")
            .addEnvironmentVariable("VAR1", "VAL1")
            .addEnvironmentVariable("VAR2", "VAL2")
            .setEntrypoint(Arrays.asList("some", "entrypoint", "command"))
            .setProgramArguments(Arrays.asList("arg1", "arg2"))
            .setHealthCheck(
                DockerHealthCheck.fromCommand(ImmutableList.of("CMD-SHELL", "/checkhealth"))
                    .setInterval(Duration.ofSeconds(3))
                    .setTimeout(Duration.ofSeconds(1))
                    .setStartPeriod(Duration.ofSeconds(2))
                    .setRetries(3)
                    .build())
            .addExposedPorts(ImmutableSet.of(Port.tcp(1000), Port.tcp(2000), Port.udp(3000)))
            .addVolumes(
                ImmutableSet.of(
                    AbsoluteUnixPath.get("/var/job-result-data"),
                    AbsoluteUnixPath.get("/var/log/my-app-logs")))
            .addLabels(ImmutableMap.of("key1", "value1", "key2", "value2"))
            .setWorkingDirectory("/some/workspace")
            .setUser("tomcat");

    DescriptorDigest fakeDigest =
        DescriptorDigest.fromDigest(
            "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad");
    testImageBuilder.addLayer(
        new FakeLayer());
    testImageBuilder.addHistory(
        HistoryEntry.builder()
            .setCreationTimestamp(Instant.EPOCH)
            .setAuthor("Bazel")
            .setCreatedBy("bazel build ...")
            .setEmptyLayer(true)
            .build());
    testImageBuilder.addHistory(
        HistoryEntry.builder()
            .setCreationTimestamp(Instant.ofEpochSecond(20))
            .setAuthor("Jib")
            .setCreatedBy("jib")
            .build());
    imageToJsonTranslator = new ImageToJsonTranslator(testImageBuilder.build());
  }

        class FakeLayer
        {

            public Blob getBlob()
            {
                return Blobs.from("ignored");
            }

            public BlobDescriptor getBlobDescriptor()
            {
                return new BlobDescriptor(1000, fakeDigest);
            }

            public DescriptorDigest getDiffId()
            {
                return fakeDigest;
            }
        }

        [TestMethod]
  public void testGetContainerConfiguration()
      {
    setUp(typeof(V22ManifestTemplate));

    // Loads the expected JSON string.
    Path jsonFile = Paths.get(Resources.getResource("core/json/containerconfig.json").toURI());
    string expectedJson = new string(Files.readAllBytes(jsonFile), StandardCharsets.UTF_8);

    // Translates the image to the container configuration and writes the JSON string.
    JsonTemplate containerConfiguration = imageToJsonTranslator.getContainerConfiguration();

    Assert.assertEquals(expectedJson, JsonTemplateMapper.toUtf8String(containerConfiguration));
  }

  [TestMethod]
  public void testGetManifest_v22() {
    setUp(typeof(V22ManifestTemplate));
    testGetManifest(typeof(V22ManifestTemplate), "core/json/translated_v22manifest.json");
  }

  [TestMethod]
  public void testGetManifest_oci() {
    setUp(typeof(OCIManifestTemplate));
    testGetManifest(typeof(OCIManifestTemplate), "core/json/translated_ocimanifest.json");
  }

  [TestMethod]
  public void testPortListToMap() {
    ImmutableSet<Port> input = ImmutableSet.of(Port.tcp(1000), Port.udp(2000));
    ImmutableSortedMap<string, Map<object, object>> expected =
        ImmutableSortedMap.of("1000/tcp", ImmutableMap.of(), "2000/udp", ImmutableMap.of());
    Assert.assertEquals(expected, ImageToJsonTranslator.portSetToMap(input));
  }

  [TestMethod]
  public void testVolumeListToMap() {
    ImmutableSet<AbsoluteUnixPath> input =
        ImmutableSet.of(
            AbsoluteUnixPath.get("/var/job-result-data"),
            AbsoluteUnixPath.get("/var/log/my-app-logs"));
    ImmutableSortedMap<string, Map<object, object>> expected =
        ImmutableSortedMap.of(
            "/var/job-result-data", ImmutableMap.of(), "/var/log/my-app-logs", ImmutableMap.of());
    Assert.assertEquals(expected, ImageToJsonTranslator.volumesSetToMap(input));
  }

  [TestMethod]
  public void testEnvironmentMapToList() {
    ImmutableMap<string, string> input = ImmutableMap.of("NAME1", "VALUE1", "NAME2", "VALUE2");
    ImmutableList<string> expected = ImmutableList.of("NAME1=VALUE1", "NAME2=VALUE2");
    Assert.assertEquals(expected, ImageToJsonTranslator.environmentMapToList(input));
  }

  /** Tests translation of image to {@link BuildableManifestTemplate}. */
  private void testGetManifest<T>(
      Class<T> manifestTemplateClass, string translatedJsonFilename) where T : BuildableManifestTemplate {
    // Loads the expected JSON string.
    Path jsonFile = Paths.get(Resources.getResource(translatedJsonFilename).toURI());
    string expectedJson = new string(Files.readAllBytes(jsonFile), StandardCharsets.UTF_8);

    // Translates the image to the manifest and writes the JSON string.
    JsonTemplate containerConfiguration = imageToJsonTranslator.getContainerConfiguration();
    BlobDescriptor blobDescriptor = Digests.computeDigest(containerConfiguration);
    T manifestTemplate =
        imageToJsonTranslator.getManifestTemplate(manifestTemplateClass, blobDescriptor);

    Assert.assertEquals(expectedJson, JsonTemplateMapper.toUtf8String(manifestTemplate));
  }
}
}
