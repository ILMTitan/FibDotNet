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

using Fib.Net.Core.Api;
using Fib.Net.Core.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Resources = Fib.Net.Core.Configuration.Resources;

namespace Fib.Net.Core.Unit.Tests.Configuration
{
    /** Tests for {@link ContainerConfiguration}. */
    public class ContainerConfigurationTest
    {
        [Test]
        public void TestBuilder_nullValues()
        {
            // Java arguments element should not be null.
            try
            {
                ContainerConfiguration.CreateBuilder().SetProgramArguments(new[] { "first", null });
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.That(ex.Message, Contains.Substring(Resources.NullProgramArgument));
            }

            // Entrypoint element should not be null.
            try
            {
                ContainerConfiguration.CreateBuilder().SetEntrypoint(new[] { "first", null });
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.That(ex.Message, Contains.Substring(Resources.NullEntrypointArgument));
            }

            // Exposed ports element should not be null.
            ISet<Port> badPorts = new HashSet<Port> { Port.Tcp(1000), null };
            try
            {
                ContainerConfiguration.CreateBuilder().SetExposedPorts(badPorts);
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.That(ex.Message, Contains.Substring(Resources.NullPort));
            }

            // Volume element should not be null.
            ISet<AbsoluteUnixPath> badVolumes =
                new HashSet<AbsoluteUnixPath> { AbsoluteUnixPath.Get("/"), null };
            try
            {
                ContainerConfiguration.CreateBuilder().SetVolumes(badVolumes);
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.That(ex.Message, Contains.Substring(Resources.NullVolume));
            }
        }

        [Test]
        public void TestBuilder_environmentMapTypes()
        {
            // Can accept empty environment.
            ContainerConfiguration.CreateBuilder().SetEnvironment(ImmutableDictionary.Create<string, string>()).Build();

            // Can handle other map types (https://github.com/GoogleContainerTools/fib/issues/632)
            ContainerConfiguration.CreateBuilder().SetEnvironment(new SortedDictionary<string, string>());
        }

        [Test]
        public void TestBuilder_user()
        {
            ContainerConfiguration configuration = ContainerConfiguration.CreateBuilder().SetUser("john").Build();
            Assert.AreEqual("john", configuration.GetUser());
        }

        [Test]
        public void TestBuilder_workingDirectory()
        {
            ContainerConfiguration configuration =
                ContainerConfiguration.CreateBuilder().SetWorkingDirectory(AbsoluteUnixPath.Get("/path")).Build();
            Assert.AreEqual(AbsoluteUnixPath.Get("/path"), configuration.GetWorkingDirectory());
        }
    }
}
