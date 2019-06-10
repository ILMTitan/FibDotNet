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

















/** Tests for {@link DockerConfigCredentialRetriever}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class DockerConfigCredentialRetrieverTest {

  private static readonly Credential FAKE_CREDENTIAL = Credential.from("username", "password");

  [Mock] private DockerCredentialHelper mockDockerCredentialHelper;
  [Mock] private DockerConfig mockDockerConfig;
  [Mock] private Consumer<LogEvent> mockLogger;

  private SystemPath dockerConfigFile;

  [TestInitialize]
  public void setUp()
      {
    dockerConfigFile = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());
    Mockito.when(mockDockerCredentialHelper.retrieve()).thenReturn(FAKE_CREDENTIAL);
  }

  [TestMethod]
  public void testRetrieve_nonexistentDockerConfigFile() {
    DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
        new DockerConfigCredentialRetriever("some registry", Paths.get("fake/path"));

    Assert.assertFalse(dockerConfigCredentialRetriever.retrieve(mockLogger).isPresent());
  }

  [TestMethod]
  public void testRetrieve_hasAuth() {
    DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
        new DockerConfigCredentialRetriever("some registry", dockerConfigFile);

    Optional<Credential> credentials = dockerConfigCredentialRetriever.retrieve(mockLogger);
    Assert.assertTrue(credentials.isPresent());
    Assert.assertEquals("some", credentials.get().getUsername());
    Assert.assertEquals("auth", credentials.get().getPassword());
  }

  [TestMethod]
  public void testRetrieve_useCredsStore() {
    Mockito.when(mockDockerConfig.getCredentialHelperFor("just registry"))
        .thenReturn(mockDockerCredentialHelper);
    DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
        new DockerConfigCredentialRetriever("just registry", dockerConfigFile);

    Assert.assertEquals(
        FAKE_CREDENTIAL,
        dockerConfigCredentialRetriever
            .retrieve(mockDockerConfig, mockLogger)
            .orElseThrow(AssertionError.new));
  }

  [TestMethod]
  public void testRetrieve_useCredsStore_withProtocol() {
    Mockito.when(mockDockerConfig.getCredentialHelperFor("with.protocol"))
        .thenReturn(mockDockerCredentialHelper);
    DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
        new DockerConfigCredentialRetriever("with.protocol", dockerConfigFile);

    Assert.assertEquals(
        FAKE_CREDENTIAL,
        dockerConfigCredentialRetriever
            .retrieve(mockDockerConfig, mockLogger)
            .orElseThrow(AssertionError.new));
  }

  [TestMethod]
  public void testRetrieve_useCredHelper() {
    Mockito.when(mockDockerConfig.getCredentialHelperFor("another registry"))
        .thenReturn(mockDockerCredentialHelper);
    DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
        new DockerConfigCredentialRetriever("another registry", dockerConfigFile);

    Assert.assertEquals(
        FAKE_CREDENTIAL,
        dockerConfigCredentialRetriever
            .retrieve(mockDockerConfig, mockLogger)
            .orElseThrow(AssertionError.new));
  }

  [TestMethod]
  public void testRetrieve_useCredHelper_warn()
      {
    Mockito.when(mockDockerConfig.getCredentialHelperFor("another registry"))
        .thenReturn(mockDockerCredentialHelper);
    Mockito.when(mockDockerCredentialHelper.retrieve())
        .thenThrow(
            new CredentialHelperNotFoundException(
                Paths.get("docker-credential-path"), new Throwable("cause")));

    new DockerConfigCredentialRetriever("another registry", dockerConfigFile)
        .retrieve(mockDockerConfig, mockLogger);

    Mockito.verify(mockLogger)
        .accept(LogEvent.warn("The system does not have docker-credential-path CLI"));
    Mockito.verify(mockLogger).accept(LogEvent.warn("  Caused by: cause"));
  }

  [TestMethod]
  public void testRetrieve_none() {
    DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
        new DockerConfigCredentialRetriever("unknown registry", dockerConfigFile);

    Assert.assertFalse(dockerConfigCredentialRetriever.retrieve(mockLogger).isPresent());
  }

  [TestMethod]
  public void testRetrieve_credentialFromAlias() {
    Mockito.when(mockDockerConfig.getCredentialHelperFor("registry.hub.docker.com"))
        .thenReturn(mockDockerCredentialHelper);
    DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
        new DockerConfigCredentialRetriever("registry.hub.docker.com", dockerConfigFile);

    Assert.assertEquals(
        FAKE_CREDENTIAL,
        dockerConfigCredentialRetriever
            .retrieve(mockDockerConfig, mockLogger)
            .orElseThrow(AssertionError.new));
  }

  [TestMethod]
  public void testRetrieve_suffixMatching() {
    SystemPath dockerConfigFile =
        Paths.get(Resources.getResource("core/json/dockerconfig_index_docker_io_v1.json").toURI());

    DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
        new DockerConfigCredentialRetriever("index.docker.io", dockerConfigFile);

    Optional<Credential> credentials = dockerConfigCredentialRetriever.retrieve(mockLogger);
    Assert.assertTrue(credentials.isPresent());
    Assert.assertEquals("token for", credentials.get().getUsername());
    Assert.assertEquals(" index.docker.io/v1/", credentials.get().getPassword());
  }

  [TestMethod]
  public void testRetrieve_suffixMatchingFromAlias() {
    SystemPath dockerConfigFile =
        Paths.get(Resources.getResource("core/json/dockerconfig_index_docker_io_v1.json").toURI());

    DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
        new DockerConfigCredentialRetriever("registry.hub.docker.com", dockerConfigFile);

    Optional<Credential> credentials = dockerConfigCredentialRetriever.retrieve(mockLogger);
    Assert.assertTrue(credentials.isPresent());
    Assert.assertEquals("token for", credentials.get().getUsername());
    Assert.assertEquals(" index.docker.io/v1/", credentials.get().getPassword());
  }
}
}
