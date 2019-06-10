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













/** Tests for {@link OCIManifestTemplate}. */
public class OCIManifestTemplateTest {

  [TestMethod]
  public void testToJson() {
    // Loads the expected JSON string.
    SystemPath jsonFile = Paths.get(Resources.getResource("core/json/ocimanifest.json").toURI());
    string expectedJson = new string(Files.readAllBytes(jsonFile), StandardCharsets.UTF_8);

    // Creates the JSON object to serialize.
    OCIManifestTemplate manifestJson = new OCIManifestTemplate();

    manifestJson.setContainerConfiguration(
        1000,
        DescriptorDigest.fromDigest(
            "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"));

    manifestJson.addLayer(
        1000_000,
        DescriptorDigest.fromHash(
            "4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236"));

    // Serializes the JSON object.
    Assert.assertEquals(expectedJson, JsonTemplateMapper.toUtf8String(manifestJson));
  }

  [TestMethod]
  public void testFromJson() {
    // Loads the JSON string.
    SystemPath jsonFile = Paths.get(Resources.getResource("core/json/ocimanifest.json").toURI());

    // Deserializes into a manifest JSON object.
    OCIManifestTemplate manifestJson =
        JsonTemplateMapper.readJsonFromFile(jsonFile, typeof(OCIManifestTemplate));

    Assert.assertEquals(
        DescriptorDigest.fromDigest(
            "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
        manifestJson.getContainerConfiguration().getDigest());

    Assert.assertEquals(1000, manifestJson.getContainerConfiguration().getSize());

    Assert.assertEquals(
        DescriptorDigest.fromHash(
            "4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236"),
        manifestJson.getLayers().get(0).getDigest());

    Assert.assertEquals(1000_000, manifestJson.getLayers().get(0).getSize());
  }
}
}
