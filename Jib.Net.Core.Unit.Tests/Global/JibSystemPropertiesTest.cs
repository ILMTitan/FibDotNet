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

using com.google.cloud.tools.jib.global;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;

namespace Jib.Net.Core.Unit.Tests.Global
{
    /** Tests for {@link JibSystemProperties}. */
    public class JibSystemPropertiesTest
    {
        [SetUp]
        public void setUp()
        {
        }

        [TearDown]
        public void tearDown()
        {
            Environment.SetEnvironmentVariable(JibSystemProperties.HTTP_TIMEOUT, null);
        }

        [Test]
        public void testCheckHttpTimeoutProperty_ok()
        {
            Assert.IsNull(Environment.GetEnvironmentVariable(JibSystemProperties.HTTP_TIMEOUT));
            JibSystemProperties.checkHttpTimeoutProperty();
        }

        [Test]
        public void testCheckHttpTimeoutProperty_stringValue()
        {
            Environment.SetEnvironmentVariable(JibSystemProperties.HTTP_TIMEOUT, "random string");
            try
            {
                JibSystemProperties.checkHttpTimeoutProperty();
                Assert.Fail();
            }
            catch (FormatException ex)
            {
                Assert.AreEqual("jib.httpTimeout must be an integer: random string", ex.getMessage());
            }
        }
    }
}
