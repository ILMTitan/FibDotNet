// Copyright 2017 Google LLC.
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
using Fib.Net.Core.Registry.Credentials;
using Fib.Net.Test.Common;
using NUnit.Framework;

namespace Fib.Net.Core.Integration.Tests.Registry.Credentials
{
    /** Integration tests for {@link DockerCredentialHelper}. */
    public class DockerCredentialHelperIntegrationTest
    {
        /** Tests retrieval via {@code docker-credential-gcr} CLI. */
        [Test]
        public void TestRetrieveGCR()
        {
            new Command("docker-credential-gcr", "store")
                .Run(Files.ReadAllBytes(Paths.Get(TestResources.GetResource("credentials.json").ToURI())));

            DockerCredentialHelper dockerCredentialHelper = new DockerCredentialHelper("myregistry", "gcr");

            Credential credentials = dockerCredentialHelper.Retrieve();
            Assert.AreEqual("myusername", credentials.GetUsername());
            Assert.AreEqual("mysecret", credentials.GetPassword());
        }

        [Test]
        public void TestRetrieve_nonexistentCredentialHelper()
        {
            try
            {
                DockerCredentialHelper fakeDockerCredentialHelper =
                    new DockerCredentialHelper("", "fake-cloud-provider");

                fakeDockerCredentialHelper.Retrieve();

                Assert.Fail("Retrieve should have failed for nonexistent credential helper");
            }
            catch (CredentialHelperNotFoundException ex)
            {
                Assert.AreEqual(
                    "The system does not have docker-credential-fake-cloud-provider CLI", ex.Message);
            }
        }

        [Test]
        public void TestRetrieve_nonexistentServerUrl()
        {
            try
            {
                DockerCredentialHelper fakeDockerCredentialHelper =
                    new DockerCredentialHelper("fake.server.url", "gcr");

                fakeDockerCredentialHelper.Retrieve();

                Assert.Fail("Retrieve should have failed for nonexistent server Uri");
            }
            catch (CredentialHelperUnhandledServerUrlException ex)
            {
                Assert.That(
                    ex.Message,
                    Does.Contain(
                        "The credential helper (docker-credential-gcr) has nothing for server Uri: fake.server.url"));
            }
        }
    }
}
