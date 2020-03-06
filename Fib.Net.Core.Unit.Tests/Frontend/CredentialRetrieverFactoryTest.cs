// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using Fib.Net.Core.Api;
using Fib.Net.Core.Events;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Frontend;
using Fib.Net.Core.Registry.Credentials;
using Moq;
using NUnit.Framework;
using System;
using System.IO;

namespace Fib.Net.Core.Unit.Tests.Frontend
{
    /** Tests for {@link CredentialRetrieverFactory}. */
    public class CredentialRetrieverFactoryTest
    {
        private static readonly Credential fakeCredentials = Credential.From("username", "password");

        /**
         * Returns a {@link DockerCredentialHelperFactory} that checks given parameters upon creating a
         * {@link DockerCredentialHelper} instance.
         *
         * @param expectedRegistry the expected registry given to the factory
         * @param expectedCredentialHelper the expected credential helper path given to the factory
         * @param returnedCredentialHelper the mock credential helper to return
         * @return a new {@link DockerCredentialHelperFactory}
         */
        private static DockerCredentialHelperFactory GetTestFactory(
            string expectedRegistry,
            SystemPath expectedCredentialHelper,
            IDockerCredentialHelper returnedCredentialHelper)
        {
            return (registry, credentialHelper) =>
            {
                Assert.AreEqual(expectedRegistry, registry);
                Assert.AreEqual(expectedCredentialHelper, credentialHelper);
                return returnedCredentialHelper;
            };
        }

        private readonly Action<LogEvent> mockLogger = Mock.Of<Action<LogEvent>>();
        private readonly IDockerCredentialHelper mockDockerCredentialHelper = Mock.Of<IDockerCredentialHelper>();
        private readonly IDockerConfigCredentialRetriever mockDockerConfigCredentialRetriever = Mock.Of<IDockerConfigCredentialRetriever>();

        /** A {@link DockerCredentialHelper} that throws {@link CredentialHelperNotFoundException}. */
        private readonly IDockerCredentialHelper mockNonexistentDockerCredentialHelper = Mock.Of<IDockerCredentialHelper>();

        private readonly CredentialHelperNotFoundException mockCredentialHelperNotFoundException = new CredentialHelperNotFoundException("ignored", null);

        [SetUp]
        public void SetUp()
        {
            Mock.Get(mockDockerCredentialHelper).Setup(m => m.Retrieve()).Returns(fakeCredentials);

            Mock.Get(mockNonexistentDockerCredentialHelper).Setup(m => m.Retrieve()).Throws(mockCredentialHelperNotFoundException);
        }

        [Test]
        public void TestDockerCredentialHelper()
        {
            CredentialRetrieverFactory credentialRetrieverFactory =
                new CredentialRetrieverFactory(
                    ImageReference.Of("registry", "repository", null),
                    mockLogger,
                    GetTestFactory(
                        "registry", Paths.Get("docker-credential-helper"), mockDockerCredentialHelper));

            Assert.AreEqual(
                fakeCredentials,
                credentialRetrieverFactory
                    .DockerCredentialHelper(Paths.Get("docker-credential-helper"))
                    .Retrieve()
                    .OrElseThrow(() => new AssertionException("")));
            Mock.Get(mockLogger).Verify(m => m(LogEvent.Info("Using docker-credential-helper for registry")));
        }

        [Test]
        public void TestInferCredentialHelper()
        {
            CredentialRetrieverFactory credentialRetrieverFactory =
                new CredentialRetrieverFactory(
                    ImageReference.Of("something.gcr.io", "repository", null),
                    mockLogger,
                    GetTestFactory(
                        "something.gcr.io",
                        Paths.Get("docker-credential-gcr"),
                        mockDockerCredentialHelper));

            Assert.AreEqual(
                fakeCredentials,
                credentialRetrieverFactory
                    .InferCredentialHelper()
                    .Retrieve()
                    .OrElseThrow(() => new AssertionException("")));
            Mock.Get(mockLogger).Verify(m => m(LogEvent.Info("Using docker-credential-gcr for something.gcr.io")));
        }

        [Test]
        public void TestInferCredentialHelper_info()
        {
            CredentialRetrieverFactory credentialRetrieverFactory =
                new CredentialRetrieverFactory(
                    ImageReference.Of("something.amazonaws.com", "repository", null),
                    mockLogger,
                    GetTestFactory(
                        "something.amazonaws.com",
                        Paths.Get("docker-credential-ecr-login"),
                        mockNonexistentDockerCredentialHelper));

            Mock.Get(mockNonexistentDockerCredentialHelper)
                .Setup(m => m.Retrieve())
                .Throws(new CredentialHelperNotFoundException("warning", new IOException("the root cause")));

            Assert.IsFalse(credentialRetrieverFactory.InferCredentialHelper().Retrieve().IsPresent());
            Mock.Get(mockLogger).Verify(m => m(LogEvent.Info("warning")));

            Mock.Get(mockLogger).Verify(m => m(LogEvent.Info("  Caused by: the root cause")));
        }

        [Test]
        public void TestDockerConfig()
        {
            CredentialRetrieverFactory credentialRetrieverFactory =
                CredentialRetrieverFactory.ForImage(
                    ImageReference.Of("registry", "repository", null), mockLogger);

            Mock.Get(mockDockerConfigCredentialRetriever).Setup(m => m.Retrieve(mockLogger)).Returns(Maybe.Of(fakeCredentials));

            Assert.AreEqual(
                fakeCredentials,
                credentialRetrieverFactory
                    .DockerConfig(mockDockerConfigCredentialRetriever)
                    .Retrieve()
                    .OrElseThrow(() => new AssertionException("")));
            Mock.Get(mockLogger).Verify(m => m(LogEvent.Info("Using credentials from Docker config for registry")));
        }
    }
}
