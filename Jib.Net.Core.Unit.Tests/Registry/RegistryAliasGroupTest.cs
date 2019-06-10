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

namespace com.google.cloud.tools.jib.registry {







/** Tests for {@link RegistryAliasGroup}. */
public class RegistryAliasGroupTest {

  [TestMethod]
  public void testGetAliasesGroup_noKnownAliases() {
    IList<string> singleton = RegistryAliasGroup.getAliasesGroup("something.gcr.io");
    Assert.assertEquals(1, singleton.size());
    Assert.assertEquals("something.gcr.io", singleton.get(0));
  }

  [TestMethod]
  public void testGetAliasesGroup_dockerHub() {
    ISet<string> aliases =
        Sets.newHashSet(
            "registry.hub.docker.com", "index.docker.io", "registry-1.docker.io", "docker.io");
    foreach (string alias in aliases)
    {
      Assert.assertEquals(aliases, new HashSet<>(RegistryAliasGroup.getAliasesGroup(alias)));
    }
  }

  [TestMethod]
  public void testGetHost_noAlias() {
    string host = RegistryAliasGroup.getHost("something.gcr.io");
    Assert.assertEquals("something.gcr.io", host);
  }

  [TestMethod]
  public void testGetHost_dockerIo() {
    string host = RegistryAliasGroup.getHost("docker.io");
    Assert.assertEquals("registry-1.docker.io", host);
  }
}
}
