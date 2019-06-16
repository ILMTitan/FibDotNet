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

using com.google.cloud.tools.jib.builder.steps;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.filesystem
{


    /** Tests for {@link DirectoryWalker}. */
    public class DirectoryWalkerTest
    {
        private ISet<SystemPath> walkedPaths;
        private PathConsumer addToWalkedPaths;

        private SystemPath testDir;

        [SetUp]
        public void setUp()
        {
            testDir = Paths.get(Resources.getResource("core/layer").toURI());
            walkedPaths = new HashSet<SystemPath>();
            addToWalkedPaths = walkedPaths.add;
        }

        [Test]
        public void testWalk()
        {
            new DirectoryWalker(testDir).walk(addToWalkedPaths);

            ISet<SystemPath> expectedPaths =
                new HashSet<SystemPath>(
                    Arrays.asList(
                        testDir,
                        testDir.resolve("a"),
                        testDir.resolve("a").resolve("b"),
                        testDir.resolve("a").resolve("b").resolve("bar"),
                        testDir.resolve("c"),
                        testDir.resolve("c").resolve("cat"),
                        testDir.resolve("foo")));
            Assert.AreEqual(expectedPaths, walkedPaths);
        }

        [Test]
        public void testWalk_withFilter()
        {
            // Filters to immediate subdirectories of testDir, and foo.
            new DirectoryWalker(testDir)
                .filter(path => path.getParent().Equals(testDir))
                .filter(path => !path.ToString().endsWith("foo"))
                .walk(addToWalkedPaths);

            ISet<SystemPath> expectedPaths =
                new HashSet<SystemPath>(Arrays.asList(testDir.resolve("a"), testDir.resolve("c")));
            CollectionAssert.AreEquivalent(expectedPaths, walkedPaths);
        }

        [Test]
        public void testWalk_withFilterRoot()
        {
            new DirectoryWalker(testDir).filterRoot().walk(addToWalkedPaths);

            ISet<SystemPath> expectedPaths =
                new HashSet<SystemPath>(
                    Arrays.asList(
                        testDir.resolve("a"),
                        testDir.resolve("a").resolve("b"),
                        testDir.resolve("a").resolve("b").resolve("bar"),
                        testDir.resolve("c"),
                        testDir.resolve("c").resolve("cat"),
                        testDir.resolve("foo")));
            CollectionAssert.AreEqual(expectedPaths, walkedPaths);
        }
    }
}
