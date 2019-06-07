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

namespace com.google.cloud.tools.jib.frontend {






















/** Tests for {@link CredentialRetrieverFactory}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class CredentialRetrieverFactoryTest {

  private static readonly Credential FAKE_CREDENTIALS = Credential.from("username", "password");

  /**
   * Returns a {@link DockerCredentialHelperFactory} that checks given parameters upon creating a
   * {@link DockerCredentialHelper} instance.
   *
   * @param expectedRegistry the expected registry given to the factory
   * @param expectedCredentialHelper the expected credential helper path given to the factory
   * @param returnedCredentialHelper the mock credential helper to return
   * @return a new {@link DockerCredentialHelperFactory}
   */
  private static DockerCredentialHelperFactory getTestFactory(
      string expectedRegistry,
      Path expectedCredentialHelper,
      DockerCredentialHelper returnedCredentialHelper) {
    return (registry, credentialHelper) => {
      Assert.assertEquals(expectedRegistry, registry);
      Assert.assertEquals(expectedCredentialHelper, credentialHelper);
      return returnedCredentialHelper;
    };
  }

  [Mock] private Consumer<LogEvent> mockLogger;
  [Mock] private DockerCredentialHelper mockDockerCredentialHelper;
  [Mock] private DockerConfigCredentialRetriever mockDockerConfigCredentialRetriever;

  /** A {@link DockerCredentialHelper} that throws {@link CredentialHelperNotFoundException}. */
  [Mock] private DockerCredentialHelper mockNonexistentDockerCredentialHelper;

  [Mock] private CredentialHelperNotFoundException mockCredentialHelperNotFoundException;

  [TestInitialize]
  public void setUp()
      {
    Mockito.when(mockDockerCredentialHelper.retrieve()).thenReturn(FAKE_CREDENTIALS);
    Mockito.when(mockNonexistentDockerCredentialHelper.retrieve())
        .thenThrow(mockCredentialHelperNotFoundException);
  }

  [TestMethod]
  public void testDockerCredentialHelper() {
    CredentialRetrieverFactory credentialRetrieverFactory =
        new CredentialRetrieverFactory(
            ImageReference.of("registry", "repository", null),
            mockLogger,
            getTestFactory(
                "registry", Paths.get("docker-credential-helper"), mockDockerCredentialHelper));

    Assert.assertEquals(
        FAKE_CREDENTIALS,
        credentialRetrieverFactory
            .dockerCredentialHelper(Paths.get("docker-credential-helper"))
            .retrieve()
            .orElseThrow(AssertionError.new));
    Mockito.verify(mockLogger).accept(LogEvent.info("Using docker-credential-helper for registry"));
  }

  [TestMethod]
  public void testInferCredentialHelper() {
    CredentialRetrieverFactory credentialRetrieverFactory =
        new CredentialRetrieverFactory(
            ImageReference.of("something.gcr.io", "repository", null),
            mockLogger,
            getTestFactory(
                "something.gcr.io",
                Paths.get("docker-credential-gcr"),
                mockDockerCredentialHelper));

    Assert.assertEquals(
        FAKE_CREDENTIALS,
        credentialRetrieverFactory
            .inferCredentialHelper()
            .retrieve()
            .orElseThrow(AssertionError.new));
    Mockito.verify(mockLogger)
        .accept(LogEvent.info("Using docker-credential-gcr for something.gcr.io"));
  }

  [TestMethod]
  public void testInferCredentialHelper_info() {
    CredentialRetrieverFactory credentialRetrieverFactory =
        new CredentialRetrieverFactory(
            ImageReference.of("something.amazonaws.com", "repository", null),
            mockLogger,
            getTestFactory(
                "something.amazonaws.com",
                Paths.get("docker-credential-ecr-login"),
                mockNonexistentDockerCredentialHelper));

    Mockito.when(mockCredentialHelperNotFoundException.getMessage()).thenReturn("warning");
    Mockito.when(mockCredentialHelperNotFoundException.getCause())
        .thenReturn(new IOException("the root cause"));
    Assert.assertFalse(credentialRetrieverFactory.inferCredentialHelper().retrieve().isPresent());
    Mockito.verify(mockLogger).accept(LogEvent.info("warning"));
    Mockito.verify(mockLogger).accept(LogEvent.info("  Caused by: the root cause"));
  }

  [TestMethod]
  public void testDockerConfig() {
    CredentialRetrieverFactory credentialRetrieverFactory =
        CredentialRetrieverFactory.forImage(
            ImageReference.of("registry", "repository", null), mockLogger);

    Mockito.when(mockDockerConfigCredentialRetriever.retrieve(mockLogger))
        .thenReturn(Optional.of(FAKE_CREDENTIALS));

    Assert.assertEquals(
        FAKE_CREDENTIALS,
        credentialRetrieverFactory
            .dockerConfig(mockDockerConfigCredentialRetriever)
            .retrieve()
            .orElseThrow(AssertionError.new));
    Mockito.verify(mockLogger)
        .accept(LogEvent.info("Using credentials from Docker config for registry"));
  }
}
}
