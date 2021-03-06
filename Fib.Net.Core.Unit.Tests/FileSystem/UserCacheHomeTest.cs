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
using Fib.Net.Core.FileSystem;
using Fib.Net.Test.Common;
using Moq;
using NUnit.Framework;
using System;

namespace Fib.Net.Core.Unit.Tests.FileSystem
{
    /** Tests for {@link UserCacheHome}. */
    public class UserCacheHomeTest : IDisposable
    {
        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        private string fakeCacheHome;
        private IEnvironment mockEnvironment;

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            fakeCacheHome = temporaryFolder.NewFolder().FullName;
            mockEnvironment = Mock.Of<IEnvironment>();
            Mock.Get(mockEnvironment).Setup(e => e.IsOsx()).Returns(false);
        }

        [Test]
        public void TestGetCacheHome_hasXdgCacheHome()
        {
            Mock.Get(mockEnvironment).Setup(e => e.GetEnvironmentVariable("XDG_CACHE_HOME")).Returns(fakeCacheHome);

            Assert.AreEqual(
                Paths.Get(fakeCacheHome),
                UserCacheHome.GetCacheHome(mockEnvironment));
        }

        [Test]
        public void TestGetCacheHome_linux()
        {
            Mock.Get(mockEnvironment).Setup(e => e.GetFolderPath(Environment.SpecialFolder.UserProfile))
                .Returns(fakeCacheHome);

            Assert.AreEqual(
                Paths.Get(fakeCacheHome, ".cache"),
                UserCacheHome.GetCacheHome(mockEnvironment));
        }

        [Test]
        public void TestGetCacheHome_windows()
        {
            Mock.Get(mockEnvironment).Setup(e => e.GetFolderPath(Environment.SpecialFolder.UserProfile))
                .Returns("nonexistent");
            Mock.Get(mockEnvironment).Setup(e => e.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .Returns(fakeCacheHome);

            Assert.AreEqual(
                Paths.Get(fakeCacheHome), UserCacheHome.GetCacheHome(mockEnvironment));
        }

        [Test]
        public void TestGetCacheHome_mac()
        {
            SystemPath libraryApplicationSupport = Paths.Get(fakeCacheHome, "Library", "Application Support");
            Files.CreateDirectories(libraryApplicationSupport);

            Mock.Get(mockEnvironment).Setup(e => e.GetFolderPath(Environment.SpecialFolder.UserProfile))
                .Returns(fakeCacheHome);
            Mock.Get(mockEnvironment).Setup(e => e.IsOsx()).Returns(true);

            Assert.AreEqual(
                libraryApplicationSupport,
                UserCacheHome.GetCacheHome(mockEnvironment));
        }
    }
}
