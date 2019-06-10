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

namespace com.google.cloud.tools.jib.docker.json {














/** Tests for {@link DockerLoadManifestEntryTemplate}. */
public class DockerLoadManifestTemplateTest {

  [TestMethod]
  public void testToJson() {
    // Loads the expected JSON string.
    SystemPath jsonFile = Paths.get(Resources.getResource("core/json/loadmanifest.json").toURI());
    string expectedJson = new string(Files.readAllBytes(jsonFile), StandardCharsets.UTF_8);

    DockerLoadManifestEntryTemplate template = new DockerLoadManifestEntryTemplate();
    template.setRepoTags(
        ImageReference.of("testregistry", "testrepo", "testtag").toStringWithTag());
    template.addLayerFile("layer1.tar.gz");
    template.addLayerFile("layer2.tar.gz");
    template.addLayerFile("layer3.tar.gz");

    IList<DockerLoadManifestEntryTemplate> loadManifest = Collections.singletonList(template);
    Assert.assertEquals(expectedJson, JsonTemplateMapper.toUtf8String(loadManifest));
  }
}
}
