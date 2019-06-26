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
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.filesystem
{
    /** Tests for {@link UnixPathParser}. */
    public class UnixPathParserTest
    {
        [Test]
        public void TestParse()
        {
            CollectionAssert.AreEqual(ImmutableArray.Create("some", "path"), UnixPathParser.Parse("/some/path"));
            CollectionAssert.AreEqual(ImmutableArray.Create("some", "path"), UnixPathParser.Parse("some/path/"));
            CollectionAssert.AreEqual(ImmutableArray.Create("some", "path"), UnixPathParser.Parse("some///path///"));
            // Windows-style paths are resolved in Unix semantics.
            CollectionAssert.AreEqual(
                ImmutableArray.Create("\\windows\\path"), UnixPathParser.Parse("\\windows\\path"));
            CollectionAssert.AreEqual(ImmutableArray.Create("T:\\dir"), UnixPathParser.Parse("T:\\dir"));
            CollectionAssert.AreEqual(
                ImmutableArray.Create("T:\\dir", "real", "path"), UnixPathParser.Parse("T:\\dir/real/path"));
        }
    }
}
