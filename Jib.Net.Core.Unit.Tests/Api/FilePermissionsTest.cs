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

using Jib.Net.Core;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link FilePermissions}. */

    public class FilePermissionsTest
    {
        [Test]
        public void testFromOctalString()
        {
            Assert.AreEqual(new FilePermissions(0777), FilePermissions.fromOctalString("777"));
            Assert.AreEqual(new FilePermissions(0000), FilePermissions.fromOctalString("000"));
            Assert.AreEqual(new FilePermissions(0123), FilePermissions.fromOctalString("123"));
            Assert.AreEqual(new FilePermissions(0755), FilePermissions.fromOctalString("755"));
            Assert.AreEqual(new FilePermissions(0644), FilePermissions.fromOctalString("644"));

            ImmutableArray<string> badStrings = ImmutableArray.Create("abc", "-123", "777444333", "987", "3");
            foreach (string badString in badStrings)
            {
                try
                {
                    FilePermissions.fromOctalString(badString);
                    Assert.Fail();
                }
                catch (ArgumentException ex)
                {
                    Assert.AreEqual(
                        "octalPermissions must be a 3-digit octal number (000-777)", ex.getMessage());
                }
            }
        }

        [Test]
        public void testFromPosixFilePermissions()
        {
            Assert.AreEqual(
                new FilePermissions(0000), FilePermissions.fromPosixFilePermissions(ImmutableHashSet.Create<PosixFilePermission>()));
            Assert.AreEqual(
                new FilePermissions(0110),
                FilePermissions.fromPosixFilePermissions(
                    ImmutableHashSet.Create(PosixFilePermission.OWNER_EXECUTE, PosixFilePermission.GROUP_EXECUTE)));
            Assert.AreEqual(
                new FilePermissions(0202),
                FilePermissions.fromPosixFilePermissions(
                    ImmutableHashSet.Create(PosixFilePermission.OWNER_WRITE, PosixFilePermission.OTHERS_WRITE)));
            Assert.AreEqual(
                new FilePermissions(0044),
                FilePermissions.fromPosixFilePermissions(
                    ImmutableHashSet.Create(PosixFilePermission.GROUP_READ, PosixFilePermission.OTHERS_READ)));
            Assert.AreEqual(
                new FilePermissions(0777),
                FilePermissions.fromPosixFilePermissions(
                    ImmutableHashSet.CreateRange(Enum.GetValues(typeof(PosixFilePermission)).Cast<PosixFilePermission>())));
        }
    }
}