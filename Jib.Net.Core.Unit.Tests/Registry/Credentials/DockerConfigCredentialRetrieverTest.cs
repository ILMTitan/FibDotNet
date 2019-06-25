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
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Test.Common;
using Moq;
using NUnit.Framework;
using System;

namespace com.google.cloud.tools.jib.registry.credentials
{
    /** Tests for {@link DockerConfigCredentialRetriever}. */
    public class DockerConfigCredentialRetrieverTest
    {
        private static readonly Credential FAKE_CREDENTIAL = Credential.from("username", "password");

        private readonly IDockerCredentialHelper mockDockerCredentialHelper = Mock.Of<IDockerCredentialHelper>();
        private readonly IDockerConfig mockDockerConfig = Mock.Of<IDockerConfig>();
        private readonly Action<LogEvent> mockLogger = Mock.Of<Action<LogEvent>>();

        private SystemPath dockerConfigFile;

        [SetUp]
        public void setUp()
        {
            dockerConfigFile = Paths.get(TestResources.getResource("core/json/dockerconfig.json").toURI());
            Mock.Get(mockDockerCredentialHelper).Setup(m => m.retrieve()).Returns(FAKE_CREDENTIAL);
        }

        [Test]
        public void testRetrieve_nonexistentDockerConfigFile()
        {
            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("some registry", Paths.get("fake/path"));

            Assert.IsFalse(dockerConfigCredentialRetriever.retrieve(mockLogger).isPresent());
        }

        [Test]
        public void testRetrieve_hasAuth()
        {
            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("some registry", dockerConfigFile);

            Optional<Credential> credentials = dockerConfigCredentialRetriever.retrieve(mockLogger);
            Assert.IsTrue(credentials.isPresent());
            Assert.AreEqual("some", credentials.get().getUsername());
            Assert.AreEqual("auth", credentials.get().getPassword());
        }

        [Test]
        public void testRetrieve_useCredsStore()
        {
            Mock.Get(mockDockerConfig).Setup(m => m.getCredentialHelperFor("just registry")).Returns(mockDockerCredentialHelper);

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("just registry", dockerConfigFile);

            Assert.AreEqual(
                FAKE_CREDENTIAL,
                dockerConfigCredentialRetriever
                    .retrieve(mockDockerConfig, mockLogger)
                    .orElseThrow(() => new AssertionException("")));
        }

        [Test]
        public void testRetrieve_useCredsStore_withProtocol()
        {
            Mock.Get(mockDockerConfig).Setup(m => m.getCredentialHelperFor("with.protocol")).Returns(mockDockerCredentialHelper);

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("with.protocol", dockerConfigFile);

            Assert.AreEqual(
                FAKE_CREDENTIAL,
                dockerConfigCredentialRetriever
                    .retrieve(mockDockerConfig, mockLogger)
                    .orElseThrow(() => new AssertionException("")));
        }

        [Test]
        public void testRetrieve_useCredHelper()
        {
            Mock.Get(mockDockerConfig).Setup(m => m.getCredentialHelperFor("another registry")).Returns(mockDockerCredentialHelper);

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("another registry", dockerConfigFile);

            Assert.AreEqual(
                FAKE_CREDENTIAL,
                dockerConfigCredentialRetriever
                    .retrieve(mockDockerConfig, mockLogger)
                    .orElseThrow(() => new AssertionException("")));
        }

        [Test]
        public void testRetrieve_useCredHelper_warn()
        {
            Mock.Get(mockDockerConfig).Setup(m => m.getCredentialHelperFor("another registry")).Returns(mockDockerCredentialHelper);

            Mock.Get(mockDockerCredentialHelper).Setup(m => m.retrieve()).Throws(
                    new CredentialHelperNotFoundException(
                        Paths.get("docker-credential-path"), new Exception("cause")));

            new DockerConfigCredentialRetriever("another registry", dockerConfigFile)
                .retrieve(mockDockerConfig, mockLogger);

            Mock.Get(mockLogger).Verify(m => m(LogEvent.warn("The system does not have docker-credential-path CLI")));

            Mock.Get(mockLogger).Verify(m => m(LogEvent.warn("  Caused by: cause")));
        }

        [Test]
        public void testRetrieve_none()
        {
            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("unknown registry", dockerConfigFile);

            Assert.IsFalse(dockerConfigCredentialRetriever.retrieve(mockLogger).isPresent());
        }

        [Test]
        public void testRetrieve_credentialFromAlias()
        {
            Mock.Get(mockDockerConfig).Setup(m => m.getCredentialHelperFor("registry.hub.docker.com")).Returns(mockDockerCredentialHelper);

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("registry.hub.docker.com", dockerConfigFile);

            Assert.AreEqual(
                FAKE_CREDENTIAL,
                dockerConfigCredentialRetriever
                    .retrieve(mockDockerConfig, mockLogger)
                    .orElseThrow(() => new AssertionException("")));
        }

        [Test]
        public void testRetrieve_suffixMatching()
        {
            SystemPath dockerConfigFile =
                Paths.get(TestResources.getResource("core/json/dockerconfig_index_docker_io_v1.json").toURI());

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("index.docker.io", dockerConfigFile);

            Optional<Credential> credentials = dockerConfigCredentialRetriever.retrieve(mockLogger);
            Assert.IsTrue(credentials.isPresent());
            Assert.AreEqual("token for", credentials.get().getUsername());
            Assert.AreEqual(" index.docker.io/v1/", credentials.get().getPassword());
        }

        [Test]
        public void testRetrieve_suffixMatchingFromAlias()
        {
            SystemPath dockerConfigFile =
                Paths.get(TestResources.getResource("core/json/dockerconfig_index_docker_io_v1.json").toURI());

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("registry.hub.docker.com", dockerConfigFile);

            Optional<Credential> credentials = dockerConfigCredentialRetriever.retrieve(mockLogger);
            Assert.IsTrue(credentials.isPresent());
            Assert.AreEqual("token for", credentials.get().getUsername());
            Assert.AreEqual(" index.docker.io/v1/", credentials.get().getPassword());
        }
    }
}
