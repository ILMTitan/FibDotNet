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
using Jib.Net.Core.Api;

namespace Jib.Net.Core.Unit.Tests.Api
{
    /** Tests for {@link RelativeUnixPath}. */

    public class RelativeUnixPathTest
    {
        [Test]
        public void TestGet_absolute()
        {
            try
            {
                RelativeUnixPath.Get("/absolute");
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("Path starts with forward slash (/): /absolute", ex.Message);
            }
        }

        [Test]
        public void TestGet()
        {
            CollectionAssert.AreEqual(
                ImmutableArray.Create("some", "relative", "path"),
                RelativeUnixPath.Get("some/relative///path").GetRelativePathComponents());
        }
    }
}