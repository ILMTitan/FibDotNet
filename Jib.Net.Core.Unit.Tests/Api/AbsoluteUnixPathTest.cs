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
        public void TestGet_notAbsolute()
        {
            try
            {
                AbsoluteUnixPath.Get("not/absolute");
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(
                    "Path does not start with forward slash (/): not/absolute", ex.GetMessage());
            }
        }

        [Test]
        public void TestFromPath()
        {
            Assert.AreEqual(
                "/absolute/path", JavaExtensions.ToString(AbsoluteUnixPath.FromPath(Paths.Get("/absolute/path"))));
        }

        [Test]
        public void TestFromPath_windows()
        {
            Assume.That(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            Assert.AreEqual(
                "/absolute/path", JavaExtensions.ToString(AbsoluteUnixPath.FromPath(Paths.Get("T:\\absolute\\path"))));
        }

        [Test]
        public void TestEquals()
        {
            AbsoluteUnixPath absoluteUnixPath1 = AbsoluteUnixPath.Get("/absolute/path");
            AbsoluteUnixPath absoluteUnixPath2 = AbsoluteUnixPath.Get("/absolute/path/");
            AbsoluteUnixPath absoluteUnixPath3 = AbsoluteUnixPath.Get("/another/path");
            Assert.AreEqual(absoluteUnixPath1, absoluteUnixPath2);
            Assert.AreNotEqual(absoluteUnixPath1, absoluteUnixPath3);
        }

        [Test]
        public void TestResolve_relativeUnixPath()
        {
            AbsoluteUnixPath absoluteUnixPath1 = AbsoluteUnixPath.Get("/");
            Assert.AreEqual(absoluteUnixPath1, absoluteUnixPath1.Resolve(""));
            Assert.AreEqual("/file", JavaExtensions.ToString(absoluteUnixPath1.Resolve("file")));
            Assert.AreEqual("/relative/path", JavaExtensions.ToString(absoluteUnixPath1.Resolve("relative/path")));

            AbsoluteUnixPath absoluteUnixPath2 = AbsoluteUnixPath.Get("/some/path");
            Assert.AreEqual(absoluteUnixPath2, absoluteUnixPath2.Resolve(""));
            Assert.AreEqual("/some/path/file", JavaExtensions.ToString(absoluteUnixPath2.Resolve("file")));
            Assert.AreEqual(
                "/some/path/relative/path", JavaExtensions.ToString(absoluteUnixPath2.Resolve("relative/path")));
        }

        [Test]
        public void TestResolve_Path_notRelative()
        {
            try
            {
                AbsoluteUnixPath.Get("/").Resolve(Paths.Get("/not/relative"));
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(
                    "Cannot resolve against absolute Path: " + Paths.Get("/not/relative"), ex.GetMessage());
            }
        }

        [Test]
        public void TestResolve_Path()
        {
            AbsoluteUnixPath absoluteUnixPath1 = AbsoluteUnixPath.Get("/");
            Assert.AreEqual(absoluteUnixPath1, absoluteUnixPath1.Resolve(Paths.Get("")));
            Assert.AreEqual("/file", JavaExtensions.ToString(absoluteUnixPath1.Resolve(Paths.Get("file"))));
            Assert.AreEqual(
                "/relative/path", JavaExtensions.ToString(absoluteUnixPath1.Resolve(Paths.Get("relative/path"))));

            AbsoluteUnixPath absoluteUnixPath2 = AbsoluteUnixPath.Get("/some/path");
            Assert.AreEqual(absoluteUnixPath2, absoluteUnixPath2.Resolve(Paths.Get("")));
            Assert.AreEqual(
                "/some/path/file", JavaExtensions.ToString(absoluteUnixPath2.Resolve(Paths.Get("file///"))));
            Assert.AreEqual(
                "/some/path/relative/path",
                JavaExtensions.ToString(absoluteUnixPath2.Resolve(Paths.Get("relative//path/"))));
        }
    }
}