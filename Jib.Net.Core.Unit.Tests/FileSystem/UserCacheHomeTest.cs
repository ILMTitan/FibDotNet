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
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace com.google.cloud.tools.jib.filesystem
{
    /** Tests for {@link UserCacheHome}. */
    public class UserCacheHomeTest
    {
        public TemporaryFolder temporaryFolder;

        private string fakeCacheHome;
        private IEnvironment mockEnvironment;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            temporaryFolder = new TemporaryFolder();
        }

        [OneTimeTearDown]
        public void OneTImeTearDown()
        {
            temporaryFolder.Dispose();
        }

        [SetUp]
        public void setUp()
        {
            fakeCacheHome = temporaryFolder.newFolder().getPath();
            mockEnvironment = Mock.Of<IEnvironment>();
            Mock.Get(mockEnvironment).Setup(e => e.IsOSPlatform(It.IsAny<OSPlatform>())).Returns(false);
        }

        [Test]
        public void testGetCacheHome_hasXdgCacheHome()
        {
            Mock.Get(mockEnvironment).Setup(e =>e.GetEnvironmentVariable("XDG_CACHE_HOME")).Returns(fakeCacheHome);

            Assert.AreEqual(
                Paths.get(fakeCacheHome),
                UserCacheHome.getCacheHome(mockEnvironment));
        }

        [Test]
        public void testGetCacheHome_linux()
        {
            Mock.Get(mockEnvironment).Setup(e => e.GetFolderPath(Environment.SpecialFolder.UserProfile))
                .Returns(fakeCacheHome);
            Mock.Get(mockEnvironment).Setup(e => e.IsOSPlatform(OSPlatform.Linux)).Returns(true);

            Assert.AreEqual(
                Paths.get(fakeCacheHome, ".cache"),
                UserCacheHome.getCacheHome(mockEnvironment));
        }

        [Test]
        public void testGetCacheHome_windows()
        {
            Mock.Get(mockEnvironment).Setup(e => e.GetFolderPath(Environment.SpecialFolder.UserProfile))
                .Returns("nonexistent");
            Mock.Get(mockEnvironment).Setup(e => e.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .Returns(fakeCacheHome);
            Mock.Get(mockEnvironment).Setup(e => e.IsOSPlatform(OSPlatform.Windows)).Returns(true);

            Assert.AreEqual(
                Paths.get(fakeCacheHome), UserCacheHome.getCacheHome(mockEnvironment));
        }

        [Test]
        public void testGetCacheHome_mac()
        {
            SystemPath libraryApplicationSupport = Paths.get(fakeCacheHome, "Library", "Application Support");
            Files.createDirectories(libraryApplicationSupport);

            Mock.Get(mockEnvironment).Setup(e => e.GetFolderPath(Environment.SpecialFolder.UserProfile))
                .Returns(fakeCacheHome);
            Mock.Get(mockEnvironment).Setup(e => e.IsOSPlatform(OSPlatform.OSX)).Returns(true);

            Assert.AreEqual(
                libraryApplicationSupport,
                UserCacheHome.getCacheHome(mockEnvironment));
        }
    }
}
