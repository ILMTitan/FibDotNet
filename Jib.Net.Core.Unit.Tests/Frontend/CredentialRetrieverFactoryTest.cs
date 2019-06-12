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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.registry.credentials;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System.IO;

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
      SystemPath expectedCredentialHelper,
      DockerCredentialHelper returnedCredentialHelper) {
    return (registry, credentialHelper) => {
      Assert.AreEqual(expectedRegistry, registry);
      Assert.AreEqual(expectedCredentialHelper, credentialHelper);
      return returnedCredentialHelper;
    };
  }

  private Consumer<LogEvent> mockLogger = Mock.Of<Consumer<LogEvent>>();
  private DockerCredentialHelper mockDockerCredentialHelper = Mock.Of<DockerCredentialHelper>();
  private DockerConfigCredentialRetriever mockDockerConfigCredentialRetriever = Mock.Of<DockerConfigCredentialRetriever>();

  /** A {@link DockerCredentialHelper} that throws {@link CredentialHelperNotFoundException}. */
  private DockerCredentialHelper mockNonexistentDockerCredentialHelper = Mock.Of<DockerCredentialHelper>();

  private CredentialHelperNotFoundException mockCredentialHelperNotFoundException = Mock.Of<CredentialHelperNotFoundException>();

  [SetUp]
  public void setUp()
      {
    Mock.Get(mockDockerCredentialHelper).Setup(m => m.retrieve()).Returns(FAKE_CREDENTIALS);

    Mock.Get(mockNonexistentDockerCredentialHelper).Setup(m => m.retrieve()).Throws(mockCredentialHelperNotFoundException);
  }

  [Test]
  public void testDockerCredentialHelper() {
    CredentialRetrieverFactory credentialRetrieverFactory =
        new CredentialRetrieverFactory(
            ImageReference.of("registry", "repository", null),
            mockLogger,
            getTestFactory(
                "registry", Paths.get("docker-credential-helper"), mockDockerCredentialHelper));

    Assert.AreEqual(
        FAKE_CREDENTIALS,
        credentialRetrieverFactory
            .dockerCredentialHelper(Paths.get("docker-credential-helper"))
            .retrieve()
            .orElseThrow(() => new AssertionException("")));
    Mock.Get(mockLogger).Verify(m => m.accept(LogEvent.info("Using docker-credential-helper for registry")));

  }

  [Test]
  public void testInferCredentialHelper() {
    CredentialRetrieverFactory credentialRetrieverFactory =
        new CredentialRetrieverFactory(
            ImageReference.of("something.gcr.io", "repository", null),
            mockLogger,
            getTestFactory(
                "something.gcr.io",
                Paths.get("docker-credential-gcr"),
                mockDockerCredentialHelper));

    Assert.AreEqual(
        FAKE_CREDENTIALS,
        credentialRetrieverFactory
            .inferCredentialHelper()
            .retrieve()
            .orElseThrow(() => new AssertionException("")));
    Mock.Get(mockLogger).Verify(m => m.accept(LogEvent.info("Using docker-credential-gcr for something.gcr.io")));

  }

  [Test]
  public void testInferCredentialHelper_info() {
    CredentialRetrieverFactory credentialRetrieverFactory =
        new CredentialRetrieverFactory(
            ImageReference.of("something.amazonaws.com", "repository", null),
            mockLogger,
            getTestFactory(
                "something.amazonaws.com",
                Paths.get("docker-credential-ecr-login"),
                mockNonexistentDockerCredentialHelper));

    Mock.Get(mockCredentialHelperNotFoundException).Setup(m => m.getMessage()).Returns("warning");

    Mock.Get(mockCredentialHelperNotFoundException).Setup(m => m.getCause()).Returns(new IOException("the root cause"));

    Assert.IsFalse(credentialRetrieverFactory.inferCredentialHelper().retrieve().isPresent());
    Mock.Get(mockLogger).Verify(m => m.accept(LogEvent.info("warning")));

    Mock.Get(mockLogger).Verify(m => m.accept(LogEvent.info("  Caused by: the root cause")));

  }

  [Test]
  public void testDockerConfig() {
    CredentialRetrieverFactory credentialRetrieverFactory =
        CredentialRetrieverFactory.forImage(
            ImageReference.of("registry", "repository", null), mockLogger);

    Mock.Get(mockDockerConfigCredentialRetriever).Setup(m => m.retrieve(mockLogger)).Returns(Optional.of(FAKE_CREDENTIALS));

    Assert.AreEqual(
        FAKE_CREDENTIALS,
        credentialRetrieverFactory
            .dockerConfig(mockDockerConfigCredentialRetriever)
            .retrieve()
            .orElseThrow(() => new AssertionException("")));
    Mock.Get(mockLogger).Verify(m => m.accept(LogEvent.info("Using credentials from Docker config for registry")));

  }
}
}
