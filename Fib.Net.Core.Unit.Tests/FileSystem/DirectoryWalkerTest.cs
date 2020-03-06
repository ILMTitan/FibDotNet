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

using Fib.Net.Core.Api;
using Fib.Net.Core.FileSystem;
using Fib.Net.Test.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Fib.Net.Core.Unit.Tests.FileSystem
{
    /** Tests for {@link DirectoryWalker}. */
    public class DirectoryWalkerTest
    {
        private ISet<SystemPath> walkedPaths;
        private PathConsumer addToWalkedPaths;

        private SystemPath testDir;

        [SetUp]
        public void SetUp()
        {
            testDir = Paths.Get(TestResources.GetResource("core/layer").ToURI());
            walkedPaths = new HashSet<SystemPath>();
            addToWalkedPaths = p => walkedPaths.Add(p);
        }

        [Test]
        public void TestWalk()
        {
            new DirectoryWalker(testDir).Walk(addToWalkedPaths);

            ISet<SystemPath> expectedPaths = new HashSet<SystemPath>
            {
                testDir,
                testDir.Resolve("a"),
                testDir.Resolve("a").Resolve("b"),
                testDir.Resolve("a").Resolve("b").Resolve("bar"),
                testDir.Resolve("c"),
                testDir.Resolve("c").Resolve("cat"),
                testDir.Resolve("foo")
            };
            Assert.AreEqual(expectedPaths, walkedPaths);
        }

        [Test]
        public void TestWalk_withFilter()
        {
            // Filters to immediate subdirectories of testDir, and foo.
            new DirectoryWalker(testDir)
                .Filter(path => path.GetParent().Equals(testDir))
                .Filter(path => !path.ToString().EndsWith("foo", StringComparison.Ordinal))
                .Walk(addToWalkedPaths);

            ISet<SystemPath> expectedPaths =
                new HashSet<SystemPath> { testDir.Resolve("a"), testDir.Resolve("c") };
            CollectionAssert.AreEquivalent(expectedPaths, walkedPaths);
        }

        [Test]
        public void TestWalk_withFilterRoot()
        {
            new DirectoryWalker(testDir).FilterRoot().Walk(addToWalkedPaths);

            ISet<SystemPath> expectedPaths = new HashSet<SystemPath>
            {
                testDir.Resolve("a"),
                testDir.Resolve("a").Resolve("b"),
                testDir.Resolve("a").Resolve("b").Resolve("bar"),
                testDir.Resolve("c"),
                testDir.Resolve("c").Resolve("cat"),
                testDir.Resolve("foo")
            };
            CollectionAssert.AreEqual(expectedPaths, walkedPaths);
        }
    }
}
