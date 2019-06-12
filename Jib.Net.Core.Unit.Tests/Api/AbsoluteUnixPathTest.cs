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

using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;

namespace com.google.cloud.tools.jib.api
{
    /** Test for {@link AbsoluteUnixPath}. */

    public class AbsoluteUnixPathTest
    {
        [Test]
        public void testGet_notAbsolute()
        {
            try
            {
                AbsoluteUnixPath.get("not/absolute");
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(
                    "Path does not start with forward slash (/): not/absolute", ex.getMessage());
            }
        }

        [Test]
        public void testFromPath()
        {
            Assert.AreEqual(
                "/absolute/path", AbsoluteUnixPath.fromPath(Paths.get("/absolute/path")).toString());
        }

        [Test]
        public void testFromPath_windows()
        {
            Assume.That(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            Assert.AreEqual(
                "/absolute/path", AbsoluteUnixPath.fromPath(Paths.get("T:\\absolute\\path")).toString());
        }

        [Test]
        public void testEquals()
        {
            AbsoluteUnixPath absoluteUnixPath1 = AbsoluteUnixPath.get("/absolute/path");
            AbsoluteUnixPath absoluteUnixPath2 = AbsoluteUnixPath.get("/absolute/path/");
            AbsoluteUnixPath absoluteUnixPath3 = AbsoluteUnixPath.get("/another/path");
            Assert.AreEqual(absoluteUnixPath1, absoluteUnixPath2);
            Assert.AreNotEqual(absoluteUnixPath1, absoluteUnixPath3);
        }

        [Test]
        public void testResolve_relativeUnixPath()
        {
            AbsoluteUnixPath absoluteUnixPath1 = AbsoluteUnixPath.get("/");
            Assert.AreEqual(absoluteUnixPath1, absoluteUnixPath1.resolve(""));
            Assert.AreEqual("/file", absoluteUnixPath1.resolve("file").toString());
            Assert.AreEqual("/relative/path", absoluteUnixPath1.resolve("relative/path").toString());

            AbsoluteUnixPath absoluteUnixPath2 = AbsoluteUnixPath.get("/some/path");
            Assert.AreEqual(absoluteUnixPath2, absoluteUnixPath2.resolve(""));
            Assert.AreEqual("/some/path/file", absoluteUnixPath2.resolve("file").toString());
            Assert.AreEqual(
                "/some/path/relative/path", absoluteUnixPath2.resolve("relative/path").toString());
        }

        [Test]
        public void testResolve_Path_notRelative()
        {
            try
            {
                AbsoluteUnixPath.get("/").resolve(Paths.get("/not/relative"));
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(
                    "Cannot resolve against absolute Path: " + Paths.get("/not/relative"), ex.getMessage());
            }
        }

        [Test]
        public void testResolve_Path()
        {
            AbsoluteUnixPath absoluteUnixPath1 = AbsoluteUnixPath.get("/");
            Assert.AreEqual(absoluteUnixPath1, absoluteUnixPath1.resolve(Paths.get("")));
            Assert.AreEqual("/file", absoluteUnixPath1.resolve(Paths.get("file")).toString());
            Assert.AreEqual(
                "/relative/path", absoluteUnixPath1.resolve(Paths.get("relative/path")).toString());

            AbsoluteUnixPath absoluteUnixPath2 = AbsoluteUnixPath.get("/some/path");
            Assert.AreEqual(absoluteUnixPath2, absoluteUnixPath2.resolve(Paths.get("")));
            Assert.AreEqual(
                "/some/path/file", absoluteUnixPath2.resolve(Paths.get("file///")).toString());
            Assert.AreEqual(
                "/some/path/relative/path",
                absoluteUnixPath2.resolve(Paths.get("relative//path/")).toString());
        }
    }
}