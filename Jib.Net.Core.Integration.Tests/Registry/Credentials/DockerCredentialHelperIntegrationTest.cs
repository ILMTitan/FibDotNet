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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.builder.steps;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.registry.credentials
{

    /** Integration tests for {@link DockerCredentialHelper}. */
    public class DockerCredentialHelperIntegrationTest
    {
        /** Tests retrieval via {@code docker-credential-gcr} CLI. */
        [Test]
        public void testRetrieveGCR()
        {
            new Command("docker-credential-gcr", "store")
                .run(Files.readAllBytes(Paths.get(Resources.getResource("credentials.json").toURI())));

            DockerCredentialHelper dockerCredentialHelper = new DockerCredentialHelper("myregistry", "gcr");

            Credential credentials = dockerCredentialHelper.retrieve();
            Assert.AreEqual("myusername", credentials.getUsername());
            Assert.AreEqual("mysecret", credentials.getPassword());
        }

        [Test]
        public void testRetrieve_nonexistentCredentialHelper()
        {
            try
            {
                DockerCredentialHelper fakeDockerCredentialHelper =
                    new DockerCredentialHelper("", "fake-cloud-provider");

                fakeDockerCredentialHelper.retrieve();

                Assert.Fail("Retrieve should have failed for nonexistent credential helper");
            }
            catch (CredentialHelperNotFoundException ex)
            {
                Assert.AreEqual(
                    "The system does not have docker-credential-fake-cloud-provider CLI", ex.getMessage());
            }
        }

        [Test]
        public void testRetrieve_nonexistentServerUrl()
        {
            try
            {
                DockerCredentialHelper fakeDockerCredentialHelper =
                    new DockerCredentialHelper("fake.server.url", "gcr");

                fakeDockerCredentialHelper.retrieve();

                Assert.Fail("Retrieve should have failed for nonexistent server Uri");
            }
            catch (CredentialHelperUnhandledServerUrlException ex)
            {
                StringAssert.Contains(
                    ex.getMessage(),
                        "The credential helper (docker-credential-gcr) has nothing for server Uri: fake.server.url");
            }
        }
    }
}