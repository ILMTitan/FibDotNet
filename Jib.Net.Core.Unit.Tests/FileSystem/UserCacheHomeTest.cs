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
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.filesystem
{





    /** Tests for {@link UserCacheHome}. */
    public class UserCacheHomeTest
    {
        [Rule] public TemporaryFolder temporaryFolder = new TemporaryFolder();

        private string fakeCacheHome;

        [SetUp]
        public void setUp()
        {
            fakeCacheHome = temporaryFolder.newFolder().getPath();
        }

        [Test]
        public void testGetCacheHome_hasXdgCacheHome()
        {
            IDictionary<string, string> fakeEnvironment = ImmutableDic.of("XDG_CACHE_HOME", fakeCacheHome);

            Assert.AreEqual(
                Paths.get(fakeCacheHome),
                UserCacheHome.getCacheHome());
        }

        [Test]
        public void testGetCacheHome_linux()
        {
            Properties fakeProperties = new Properties();
            fakeProperties.setProperty("user.home", fakeCacheHome);
            fakeProperties.setProperty("os.name", "os is LiNuX");

            Assert.AreEqual(
                Paths.get(fakeCacheHome, ".cache"),
                UserCacheHome.getCacheHome());
        }

        [Test]
        public void testGetCacheHome_windows()
        {
            Properties fakeProperties = new Properties();
            fakeProperties.setProperty("user.home", "nonexistent");
            fakeProperties.setProperty("os.name", "os is WiNdOwS");

            IDictionary<string, string> fakeEnvironment = ImmutableDic.of("LOCALAPPDATA", fakeCacheHome);

            Assert.AreEqual(
                Paths.get(fakeCacheHome), UserCacheHome.getCacheHome());
        }

        [Test]
        public void testGetCacheHome_mac()
        {
            SystemPath libraryApplicationSupport = Paths.get(fakeCacheHome, "Library", "Application Support");
            Files.createDirectories(libraryApplicationSupport);

            Properties fakeProperties = new Properties();
            fakeProperties.setProperty("user.home", fakeCacheHome);
            fakeProperties.setProperty("os.name", "os is mAc or DaRwIn");

            Assert.AreEqual(
                libraryApplicationSupport,
                UserCacheHome.getCacheHome());
        }
    }
}
