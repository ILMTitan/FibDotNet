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

using Fib.Net.Core.Registry;
using NUnit.Framework;
using System.Collections.Generic;

namespace Fib.Net.Core.Unit.Tests.Registry
{
    /** Tests for {@link RegistryAliasGroup}. */
    public class RegistryAliasGroupTest
    {
        [Test]
        public void TestGetAliasesGroup_noKnownAliases()
        {
            IList<string> singleton = RegistryAliasGroup.GetAliasesGroup("something.gcr.io");
            Assert.AreEqual(1, singleton.Count);
            Assert.AreEqual("something.gcr.io", singleton[0]);
        }

        [Test]
        public void TestGetAliasesGroup_dockerHub()
        {
            ISet<string> aliases = new HashSet<string>
            {
                "registry.hub.docker.com",
                "index.docker.io",
                "registry-1.docker.io",
                "docker.io"
            };

            foreach (string alias in aliases)
            {
                CollectionAssert.AreEquivalent(aliases, new HashSet<string>(RegistryAliasGroup.GetAliasesGroup(alias)));
            }
        }

        [Test]
        public void TestGetHost_noAlias()
        {
            string host = RegistryAliasGroup.GetHost("something.gcr.io");
            Assert.AreEqual("something.gcr.io", host);
        }

        [Test]
        public void TestGetHost_dockerIo()
        {
            string host = RegistryAliasGroup.GetHost("docker.io");
            Assert.AreEqual("registry-1.docker.io", host);
        }
    }
}
