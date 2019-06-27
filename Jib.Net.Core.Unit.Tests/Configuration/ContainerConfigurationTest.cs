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
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.configuration
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
                ContainerConfiguration.CreateBuilder().SetProgramArguments(new []{"first", null});
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("program arguments list contains null elements", ex.GetMessage());
            }

            // Entrypoint element should not be null.
            try
            {
                ContainerConfiguration.CreateBuilder().SetEntrypoint(new []{"first", null});
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("entrypoint contains null elements", ex.GetMessage());
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
                Assert.AreEqual("ports list contains null elements", ex.GetMessage());
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
                Assert.AreEqual("volumes list contains null elements", ex.GetMessage());
            }

            IDictionary<string, string> nullValueMap = new Dictionary<string, string>
            {
                ["key"] = null
            };

            // Labels values should not be null.
            try
            {
                ContainerConfiguration.CreateBuilder().SetLabels(nullValueMap);
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("labels map contains null values", ex.GetMessage());
            }

            // Environment values should not be null.
            try
            {
                ContainerConfiguration.CreateBuilder().SetEnvironment(nullValueMap);
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("environment map contains null values", ex.GetMessage());
            }
        }

        [Test]
        public void TestBuilder_environmentMapTypes()
        {
            // Can accept empty environment.
            ContainerConfiguration.CreateBuilder().SetEnvironment(ImmutableDictionary.Create<string, string>()).Build();

            // Can handle other map types (https://github.com/GoogleContainerTools/jib/issues/632)
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
