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

using NUnit.Framework;
using System;

namespace Fib.Net.Core.Unit.Tests
{
    /** Tests for {@link FibSystemProperties}. */
    public class FibSystemPropertiesTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(FibSystemProperties.HttpTimeout, null);
        }

        [Test]
        public void TestCheckHttpTimeoutProperty_ok()
        {
            Assert.IsNull(Environment.GetEnvironmentVariable(FibSystemProperties.HttpTimeout));
            FibSystemProperties.CheckHttpTimeoutProperty();
        }

        [Test]
        public void TestCheckHttpTimeoutProperty_stringValue()
        {
            Environment.SetEnvironmentVariable(FibSystemProperties.HttpTimeout, "random string");
            try
            {
                FibSystemProperties.CheckHttpTimeoutProperty();
                Assert.Fail();
            }
            catch (FormatException ex)
            {
                Assert.AreEqual("fib.httpTimeout must be an integer: random string", ex.Message);
            }
        }
    }
}
