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

using Fib.Net.Core.Api;
using Fib.Net.Core.Events;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Registry.Credentials;
using Fib.Net.Test.Common;
using Moq;
using NUnit.Framework;
using System;

namespace Fib.Net.Core.Unit.Tests.Registry.Credentials
{
    /** Tests for {@link DockerConfigCredentialRetriever}. */
    public class DockerConfigCredentialRetrieverTest
    {
        private static readonly Credential FAKE_CREDENTIAL = Credential.From("username", "password");

        private readonly IDockerCredentialHelper mockDockerCredentialHelper = Mock.Of<IDockerCredentialHelper>();
        private readonly IDockerConfig mockDockerConfig = Mock.Of<IDockerConfig>();
        private readonly Action<LogEvent> mockLogger = Mock.Of<Action<LogEvent>>();

        private SystemPath dockerConfigFile;

        [SetUp]
        public void SetUp()
        {
            dockerConfigFile = Paths.Get(TestResources.GetResource("core/json/dockerconfig.json").ToURI());
            Mock.Get(mockDockerCredentialHelper).Setup(m => m.Retrieve()).Returns(FAKE_CREDENTIAL);
        }

        [Test]
        public void TestRetrieve_nonexistentDockerConfigFile()
        {
            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("some registry", Paths.Get("fake/path"));

            Assert.IsFalse(dockerConfigCredentialRetriever.Retrieve(mockLogger).IsPresent());
        }

        [Test]
        public void TestRetrieve_hasAuth()
        {
            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("some registry", dockerConfigFile);

            Maybe<Credential> credentials = dockerConfigCredentialRetriever.Retrieve(mockLogger);
            Assert.IsTrue(credentials.IsPresent());
            Assert.AreEqual("some", credentials.Get().GetUsername());
            Assert.AreEqual("auth", credentials.Get().GetPassword());
        }

        [Test]
        public void TestRetrieve_useCredsStore()
        {
            Mock.Get(mockDockerConfig).Setup(m => m.GetCredentialHelperFor("just registry")).Returns(mockDockerCredentialHelper);

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("just registry", dockerConfigFile);

            Assert.AreEqual(
                FAKE_CREDENTIAL,
                dockerConfigCredentialRetriever
                    .Retrieve(mockDockerConfig, mockLogger)
                    .OrElseThrow(() => new AssertionException("")));
        }

        [Test]
        public void TestRetrieve_useCredsStore_withProtocol()
        {
            Mock.Get(mockDockerConfig).Setup(m => m.GetCredentialHelperFor("with.protocol")).Returns(mockDockerCredentialHelper);

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("with.protocol", dockerConfigFile);

            Assert.AreEqual(
                FAKE_CREDENTIAL,
                dockerConfigCredentialRetriever
                    .Retrieve(mockDockerConfig, mockLogger)
                    .OrElseThrow(() => new AssertionException("")));
        }

        [Test]
        public void TestRetrieve_useCredHelper()
        {
            Mock.Get(mockDockerConfig).Setup(m => m.GetCredentialHelperFor("another registry")).Returns(mockDockerCredentialHelper);

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("another registry", dockerConfigFile);

            Assert.AreEqual(
                FAKE_CREDENTIAL,
                dockerConfigCredentialRetriever
                    .Retrieve(mockDockerConfig, mockLogger)
                    .OrElseThrow(() => new AssertionException("")));
        }

        [Test]
        public void TestRetrieve_useCredHelper_warn()
        {
            Mock.Get(mockDockerConfig).Setup(m => m.GetCredentialHelperFor("another registry")).Returns(mockDockerCredentialHelper);

            Mock.Get(mockDockerCredentialHelper).Setup(m => m.Retrieve()).Throws(
                    new CredentialHelperNotFoundException(
                        Paths.Get("docker-credential-path"), new Exception("cause")));

            new DockerConfigCredentialRetriever("another registry", dockerConfigFile)
                .Retrieve(mockDockerConfig, mockLogger);

            Mock.Get(mockLogger).Verify(m => m(LogEvent.Warn("The system does not have docker-credential-path CLI")));

            Mock.Get(mockLogger).Verify(m => m(LogEvent.Warn("  Caused by: cause")));
        }

        [Test]
        public void TestRetrieve_none()
        {
            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("unknown registry", dockerConfigFile);

            Assert.IsFalse(dockerConfigCredentialRetriever.Retrieve(mockLogger).IsPresent());
        }

        [Test]
        public void TestRetrieve_credentialFromAlias()
        {
            Mock.Get(mockDockerConfig).Setup(m => m.GetCredentialHelperFor("registry.hub.docker.com")).Returns(mockDockerCredentialHelper);

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("registry.hub.docker.com", dockerConfigFile);

            Assert.AreEqual(
                FAKE_CREDENTIAL,
                dockerConfigCredentialRetriever
                    .Retrieve(mockDockerConfig, mockLogger)
                    .OrElseThrow(() => new AssertionException("")));
        }

        [Test]
        public void TestRetrieve_suffixMatching()
        {
            SystemPath dockerConfigFile =
                Paths.Get(TestResources.GetResource("core/json/dockerconfig_index_docker_io_v1.json").ToURI());

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("index.docker.io", dockerConfigFile);

            Maybe<Credential> credentials = dockerConfigCredentialRetriever.Retrieve(mockLogger);
            Assert.IsTrue(credentials.IsPresent());
            Assert.AreEqual("token for", credentials.Get().GetUsername());
            Assert.AreEqual(" index.docker.io/v1/", credentials.Get().GetPassword());
        }

        [Test]
        public void TestRetrieve_suffixMatchingFromAlias()
        {
            SystemPath dockerConfigFile =
                Paths.Get(TestResources.GetResource("core/json/dockerconfig_index_docker_io_v1.json").ToURI());

            DockerConfigCredentialRetriever dockerConfigCredentialRetriever =
                new DockerConfigCredentialRetriever("registry.hub.docker.com", dockerConfigFile);

            Maybe<Credential> credentials = dockerConfigCredentialRetriever.Retrieve(mockLogger);
            Assert.IsTrue(credentials.IsPresent());
            Assert.AreEqual("token for", credentials.Get().GetUsername());
            Assert.AreEqual(" index.docker.io/v1/", credentials.Get().GetPassword());
        }
    }
}
