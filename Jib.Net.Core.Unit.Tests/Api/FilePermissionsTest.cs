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

using Jib.Net.Core.FileSystem;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Jib.Net.Core.Unit.Tests.Api
{
    /** Tests for {@link FilePermissions}. */

    public class FilePermissionsTest
    {
        [Test]
        public void TestFromOctalString()
        {
            Assert.AreEqual(
                new FilePermissions(
                    PosixFilePermissions.OwnerAll | PosixFilePermissions.GroupAll | PosixFilePermissions.OthersAll),
                FilePermissions.FromOctalString("777"));
            Assert.AreEqual(
                new FilePermissions(PosixFilePermissions.None),
                FilePermissions.FromOctalString("000"));
            Assert.AreEqual(
                new FilePermissions(
                    PosixFilePermissions.OwnerExecute |
                    PosixFilePermissions.GroupWrite |
                    PosixFilePermissions.OthersRead),
                FilePermissions.FromOctalString("124"));
            Assert.AreEqual(
                new FilePermissions(
                    PosixFilePermissions.OwnerAll |
                    PosixFilePermissions.GroupRead |
                    PosixFilePermissions.GroupExecute |
                    PosixFilePermissions.OthersRead |
                    PosixFilePermissions.OthersExecute),
                FilePermissions.FromOctalString("755"));
            Assert.AreEqual(
                new FilePermissions((PosixFilePermissions)0b110_100_100),
                FilePermissions.FromOctalString("644"));

            ImmutableArray<string> badStrings = ImmutableArray.Create("abc", "-123", "777444333", "987", "3");
            foreach (string badString in badStrings)
            {
                try
                {
                    FilePermissions.FromOctalString(badString);
                    Assert.Fail();
                }
                catch (ArgumentException ex)
                {
                    Assert.AreEqual(
                        "octalPermissions must be a 3-digit octal number (000-777)", ex.Message);
                }
            }
        }

        [Test]
        public void TestFromPosixFilePermissions()
        {
            Assert.AreEqual(
                new FilePermissions(PosixFilePermissions.None), FilePermissions.FromPosixFilePermissions(ImmutableHashSet.Create<PosixFilePermissions>()));
            Assert.AreEqual(
                new FilePermissions(PosixFilePermissions.OwnerExecute | PosixFilePermissions.GroupExecute),
                FilePermissions.FromPosixFilePermissions(
                    ImmutableHashSet.Create(PosixFilePermissions.OwnerExecute, PosixFilePermissions.GroupExecute)));
            Assert.AreEqual(
                new FilePermissions(PosixFilePermissions.OwnerWrite | PosixFilePermissions.OthersWrite),
                FilePermissions.FromPosixFilePermissions(
                    ImmutableHashSet.Create(PosixFilePermissions.OwnerWrite, PosixFilePermissions.OthersWrite)));
            Assert.AreEqual(
                new FilePermissions(PosixFilePermissions.GroupRead | PosixFilePermissions.OthersRead),
                FilePermissions.FromPosixFilePermissions(
                    ImmutableHashSet.Create(PosixFilePermissions.GroupRead, PosixFilePermissions.OthersRead)));
            Assert.AreEqual(
                new FilePermissions(PosixFilePermissions.OwnerAll | PosixFilePermissions.GroupAll | PosixFilePermissions.OthersAll),
                FilePermissions.FromPosixFilePermissions(
                    ImmutableHashSet.CreateRange(Enum.GetValues(typeof(PosixFilePermissions)).Cast<PosixFilePermissions>())));
        }
    }
}