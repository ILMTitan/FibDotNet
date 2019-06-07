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

namespace com.google.cloud.tools.jib.registry.credentials {











/** Integration tests for {@link DockerCredentialHelper}. */
public class DockerCredentialHelperIntegrationTest {

  /** Tests retrieval via {@code docker-credential-gcr} CLI. */
  [TestMethod]
  public void testRetrieveGCR()
      {
    new Command("docker-credential-gcr", "store")
        .run(Files.readAllBytes(Paths.get(Resources.getResource("credentials.json").toURI())));

    DockerCredentialHelper dockerCredentialHelper = new DockerCredentialHelper("myregistry", "gcr");

    Credential credentials = dockerCredentialHelper.retrieve();
    Assert.assertEquals("myusername", credentials.getUsername());
    Assert.assertEquals("mysecret", credentials.getPassword());
  }

  [TestMethod]
  public void testRetrieve_nonexistentCredentialHelper()
      {
    try {
      DockerCredentialHelper fakeDockerCredentialHelper =
          new DockerCredentialHelper("", "fake-cloud-provider");

      fakeDockerCredentialHelper.retrieve();

      Assert.fail("Retrieve should have failed for nonexistent credential helper");

    } catch (CredentialHelperNotFoundException ex) {
      Assert.assertEquals(
          "The system does not have docker-credential-fake-cloud-provider CLI", ex.getMessage());
    }
  }

  [TestMethod]
  public void testRetrieve_nonexistentServerUrl()
      {
    try {
      DockerCredentialHelper fakeDockerCredentialHelper =
          new DockerCredentialHelper("fake.server.url", "gcr");

      fakeDockerCredentialHelper.retrieve();

      Assert.fail("Retrieve should have failed for nonexistent server Uri");

    } catch (CredentialHelperUnhandledServerUrlException ex) {
      Assert.assertThat(
          ex.getMessage(),
          CoreMatchers.containsString(
              "The credential helper (docker-credential-gcr) has nothing for server Uri: fake.server.url"));
    }
  }
}
}
