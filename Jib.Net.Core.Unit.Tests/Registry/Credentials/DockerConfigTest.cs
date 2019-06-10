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

namespace com.google.cloud.tools.jib.registry.credentials {












/** Tests for {@link DockerConfig}. */
public class DockerConfigTest {

  private static string decodeBase64(string base64String) {
    return new string(Base64.decodeBase64(base64String), StandardCharsets.UTF_8);
  }

  [TestMethod]
  public void test_fromJson() {
    // Loads the JSON string.
    SystemPath jsonFile = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

    // Deserializes into a docker config JSON object.
    DockerConfig dockerConfig =
        new DockerConfig(JsonTemplateMapper.readJsonFromFile(jsonFile, typeof(DockerConfigTemplate)));

    Assert.assertEquals("some:auth", decodeBase64(dockerConfig.getAuthFor("some registry")));
    Assert.assertEquals(
        "some:other:auth", decodeBase64(dockerConfig.getAuthFor("some other registry")));
    Assert.assertEquals("token", decodeBase64(dockerConfig.getAuthFor("registry")));
    Assert.assertEquals("token", decodeBase64(dockerConfig.getAuthFor("https://registry")));
    Assert.assertNull(dockerConfig.getAuthFor("just registry"));

    Assert.assertEquals(
        Paths.get("docker-credential-some credential store"),
        dockerConfig.getCredentialHelperFor("some registry").getCredentialHelper());
    Assert.assertEquals(
        Paths.get("docker-credential-some credential store"),
        dockerConfig.getCredentialHelperFor("some other registry").getCredentialHelper());
    Assert.assertEquals(
        Paths.get("docker-credential-some credential store"),
        dockerConfig.getCredentialHelperFor("just registry").getCredentialHelper());
    Assert.assertEquals(
        Paths.get("docker-credential-some credential store"),
        dockerConfig.getCredentialHelperFor("with.protocol").getCredentialHelper());
    Assert.assertEquals(
        Paths.get("docker-credential-another credential helper"),
        dockerConfig.getCredentialHelperFor("another registry").getCredentialHelper());
    Assert.assertNull(dockerConfig.getCredentialHelperFor("unknonwn registry"));
  }

  [TestMethod]
  public void testGetAuthFor_orderOfMatchPreference() {
    SystemPath json =
        Paths.get(Resources.getResource("core/json/dockerconfig_extra_matches.json").toURI());

    DockerConfig dockerConfig =
        new DockerConfig(JsonTemplateMapper.readJsonFromFile(json, typeof(DockerConfigTemplate)));

    Assert.assertEquals("my-registry: exact match", dockerConfig.getAuthFor("my-registry"));
    Assert.assertEquals("cool-registry: with https", dockerConfig.getAuthFor("cool-registry"));
    Assert.assertEquals(
        "awesome-registry: starting with name", dockerConfig.getAuthFor("awesome-registry"));
    Assert.assertEquals(
        "dull-registry: starting with name and with https",
        dockerConfig.getAuthFor("dull-registry"));
  }

  [TestMethod]
  public void testGetAuthFor_correctSuffixMatching() {
    SystemPath json =
        Paths.get(Resources.getResource("core/json/dockerconfig_extra_matches.json").toURI());

    DockerConfig dockerConfig =
        new DockerConfig(JsonTemplateMapper.readJsonFromFile(json, typeof(DockerConfigTemplate)));

    Assert.assertNull(dockerConfig.getAuthFor("example"));
  }

  [TestMethod]
  public void testGetCredentialHelperFor() {
    SystemPath json = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

    DockerConfig dockerConfig =
        new DockerConfig(JsonTemplateMapper.readJsonFromFile(json, typeof(DockerConfigTemplate)));

    Assert.assertEquals(
        Paths.get("docker-credential-some credential store"),
        dockerConfig.getCredentialHelperFor("just registry").getCredentialHelper());
  }

  [TestMethod]
  public void testGetCredentialHelperFor_withHttps() {
    SystemPath json = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

    DockerConfig dockerConfig =
        new DockerConfig(JsonTemplateMapper.readJsonFromFile(json, typeof(DockerConfigTemplate)));

    Assert.assertEquals(
        Paths.get("docker-credential-some credential store"),
        dockerConfig.getCredentialHelperFor("with.protocol").getCredentialHelper());
  }

  [TestMethod]
  public void testGetCredentialHelperFor_withSuffix() {
    SystemPath json = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

    DockerConfig dockerConfig =
        new DockerConfig(JsonTemplateMapper.readJsonFromFile(json, typeof(DockerConfigTemplate)));

    Assert.assertEquals(
        Paths.get("docker-credential-some credential store"),
        dockerConfig.getCredentialHelperFor("with.suffix").getCredentialHelper());
  }

  [TestMethod]
  public void testGetCredentialHelperFor_withProtocolAndSuffix()
      {
    SystemPath json = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

    DockerConfig dockerConfig =
        new DockerConfig(JsonTemplateMapper.readJsonFromFile(json, typeof(DockerConfigTemplate)));

    Assert.assertEquals(
        Paths.get("docker-credential-some credential store"),
        dockerConfig.getCredentialHelperFor("with.protocol.and.suffix").getCredentialHelper());
  }

  [TestMethod]
  public void testGetCredentialHelperFor_correctSuffixMatching()
      {
    SystemPath json = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

    DockerConfig dockerConfig =
        new DockerConfig(JsonTemplateMapper.readJsonFromFile(json, typeof(DockerConfigTemplate)));

    Assert.assertNull(dockerConfig.getCredentialHelperFor("example"));
    Assert.assertNull(dockerConfig.getCredentialHelperFor("another.example"));
  }
}
}
