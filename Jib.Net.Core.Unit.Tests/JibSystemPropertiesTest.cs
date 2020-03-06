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

using Jib.Net.Core.Global;
using NUnit.Framework;
using System;

namespace Jib.Net.Core.Unit.Tests
{
    /** Tests for {@link JibSystemProperties}. */
    public class JibSystemPropertiesTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(JibSystemProperties.HttpTimeout, null);
        }

        [Test]
        public void TestCheckHttpTimeoutProperty_ok()
        {
            Assert.IsNull(Environment.GetEnvironmentVariable(JibSystemProperties.HttpTimeout));
            JibSystemProperties.CheckHttpTimeoutProperty();
        }

        [Test]
        public void TestCheckHttpTimeoutProperty_stringValue()
        {
            Environment.SetEnvironmentVariable(JibSystemProperties.HttpTimeout, "random string");
            try
            {
                JibSystemProperties.CheckHttpTimeoutProperty();
                Assert.Fail();
            }
            catch (FormatException ex)
            {
                Assert.AreEqual("jib.httpTimeout must be an integer: random string", ex.Message);
            }
        }
    }
}
