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

namespace com.google.cloud.tools.jib.filesystem {















/** Tests for {@link UserCacheHome}. */
public class UserCacheHomeTest {

  [Rule] public TemporaryFolder temporaryFolder = new TemporaryFolder();

  private string fakeCacheHome;

  [TestInitialize]
  public void setUp() {
    fakeCacheHome = temporaryFolder.newFolder().getPath();
  }

  [TestMethod]
  public void testGetCacheHome_hasXdgCacheHome() {
    IDictionary<string, string> fakeEnvironment = ImmutableDictionary.of("XDG_CACHE_HOME", fakeCacheHome);

    Assert.assertEquals(
        Paths.get(fakeCacheHome),
        UserCacheHome.getCacheHome(Mockito.mock(typeof(Properties)), fakeEnvironment));
  }

  [TestMethod]
  public void testGetCacheHome_linux() {
    Properties fakeProperties = new Properties();
    fakeProperties.setProperty("user.home", fakeCacheHome);
    fakeProperties.setProperty("os.name", "os is LiNuX");

    Assert.assertEquals(
        Paths.get(fakeCacheHome, ".cache"),
        UserCacheHome.getCacheHome(fakeProperties, Collections.emptyMap()));
  }

  [TestMethod]
  public void testGetCacheHome_windows() {
    Properties fakeProperties = new Properties();
    fakeProperties.setProperty("user.home", "nonexistent");
    fakeProperties.setProperty("os.name", "os is WiNdOwS");

    IDictionary<string, string> fakeEnvironment = ImmutableDictionary.of("LOCALAPPDATA", fakeCacheHome);

    Assert.assertEquals(
        Paths.get(fakeCacheHome), UserCacheHome.getCacheHome(fakeProperties, fakeEnvironment));
  }

  [TestMethod]
  public void testGetCacheHome_mac() {
    SystemPath libraryApplicationSupport = Paths.get(fakeCacheHome, "Library", "Application Support");
    Files.createDirectories(libraryApplicationSupport);

    Properties fakeProperties = new Properties();
    fakeProperties.setProperty("user.home", fakeCacheHome);
    fakeProperties.setProperty("os.name", "os is mAc or DaRwIn");

    Assert.assertEquals(
        libraryApplicationSupport,
        UserCacheHome.getCacheHome(fakeProperties, Collections.emptyMap()));
  }
}
}
