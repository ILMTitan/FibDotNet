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

using NUnit.Framework;
using Jib.Net.Core.Global;
using System;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link RelativeUnixPath}. */

    public class RelativeUnixPathTest
    {
        [Test]
        public void testGet_absolute()
        {
            try
            {
                RelativeUnixPath.get("/absolute");
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("Path starts with forward slash (/): /absolute", ex.getMessage());
            }
        }

        [Test]
        public void testGet()
        {
            CollectionAssert.AreEqual(
                ImmutableArray.Create("some", "relative", "path"),
                RelativeUnixPath.get("some/relative///path").getRelativePathComponents());
        }
    }
}