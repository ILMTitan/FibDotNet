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

using com.google.cloud.tools.jib.api;
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
        public void testBuilder_nullValues()
        {
            // Java arguments element should not be null.
            try
            {
                ContainerConfiguration.builder().setProgramArguments(Arrays.asList("first", null));
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("program arguments list contains null elements", ex.getMessage());
            }

            // Entrypoint element should not be null.
            try
            {
                ContainerConfiguration.builder().setEntrypoint(Arrays.asList("first", null));
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("entrypoint contains null elements", ex.getMessage());
            }

            // Exposed ports element should not be null.
            ISet<Port> badPorts = new HashSet<Port>(Arrays.asList(Port.tcp(1000), null));
            try
            {
                ContainerConfiguration.builder().setExposedPorts(badPorts);
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("ports list contains null elements", ex.getMessage());
            }

            // Volume element should not be null.
            ISet<AbsoluteUnixPath> badVolumes =
                new HashSet<AbsoluteUnixPath>(Arrays.asList(AbsoluteUnixPath.get("/"), null));
            try
            {
                ContainerConfiguration.builder().setVolumes(badVolumes);
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("volumes list contains null elements", ex.getMessage());
            }

            IDictionary<string, string> nullKeyMap = new Dictionary<string, string>();
            nullKeyMap.put(null, "value");
            IDictionary<string, string> nullValueMap = new Dictionary<string, string>();
            nullValueMap.put("key", null);

            // Label keys should not be null.
            try
            {
                ContainerConfiguration.builder().setLabels(nullKeyMap);
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("labels map contains null keys", ex.getMessage());
            }

            // Labels values should not be null.
            try
            {
                ContainerConfiguration.builder().setLabels(nullValueMap);
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("labels map contains null values", ex.getMessage());
            }

            // Environment keys should not be null.
            try
            {
                ContainerConfiguration.builder().setEnvironment(nullKeyMap);
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("environment map contains null keys", ex.getMessage());
            }

            // Environment values should not be null.
            try
            {
                ContainerConfiguration.builder().setEnvironment(nullValueMap);
                Assert.Fail("The IllegalArgumentException should be thrown.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("environment map contains null values", ex.getMessage());
            }
        }

        [Test]
        public void testBuilder_environmentMapTypes()
        {
            // Can accept empty environment.
            ContainerConfiguration.builder().setEnvironment(ImmutableDictionary.Create<string, string>()).build();

            // Can handle other map types (https://github.com/GoogleContainerTools/jib/issues/632)
            ContainerConfiguration.builder().setEnvironment(new SortedDictionary<string, string>());
        }

        [Test]
        public void testBuilder_user()
        {
            ContainerConfiguration configuration = ContainerConfiguration.builder().setUser("john").build();
            Assert.AreEqual("john", configuration.getUser());
        }

        [Test]
        public void testBuilder_workingDirectory()
        {
            ContainerConfiguration configuration =
                ContainerConfiguration.builder().setWorkingDirectory(AbsoluteUnixPath.get("/path")).build();
            Assert.AreEqual(AbsoluteUnixPath.get("/path"), configuration.getWorkingDirectory());
        }
    }
}
