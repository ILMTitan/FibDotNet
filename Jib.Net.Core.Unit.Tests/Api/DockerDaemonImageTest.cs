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

namespace com.google.cloud.tools.jib.api {






/** Tests for {@link DockerDaemonImage}. */
public class DockerDaemonImageTest {

  [TestMethod]
  public void testGetters_default() {
    DockerDaemonImage dockerDaemonImage = DockerDaemonImage.named("docker/daemon/image");

    Assert.assertEquals("docker/daemon/image", dockerDaemonImage.getImageReference().toString());
    Assert.assertEquals(Optional.empty(), dockerDaemonImage.getDockerExecutable());
    Assert.assertEquals(0, dockerDaemonImage.getDockerEnvironment().size());
  }

  [TestMethod]
  public void testGetters() {
    DockerDaemonImage dockerDaemonImage =
        DockerDaemonImage.named("docker/daemon/image")
            .setDockerExecutable(Paths.get("docker/binary"))
            .setDockerEnvironment(ImmutableDictionary.of("key", "value"));

    Assert.assertEquals(Paths.get("docker/binary"), dockerDaemonImage.getDockerExecutable().get());
    Assert.assertEquals(ImmutableDictionary.of("key", "value"), dockerDaemonImage.getDockerEnvironment());
  }
}
}
