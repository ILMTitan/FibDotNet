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

using Fib.Net.Core.Configuration;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Immutable;

namespace Fib.Net.Core.Unit.Tests.Configuration
{
    /** Tests for {@link DockerHealthCheck}. */
    public class DockerHealthCheckTest
    {
        [Test]
        public void TestBuild()
        {
            DockerHealthCheck healthCheck =
                DockerHealthCheck.FromCommand(ImmutableArray.Create("echo", "hi"))
                    .SetInterval(Duration.FromNanoseconds(123))
                    .SetTimeout(Duration.FromNanoseconds(456))
                    .SetStartPeriod(Duration.FromNanoseconds(789))
                    .SetRetries(10)
                    .Build();

            Assert.IsTrue(healthCheck.GetInterval().IsPresent());
            Assert.AreEqual(Duration.FromNanoseconds(123), healthCheck.GetInterval().Get());
            Assert.IsTrue(healthCheck.GetTimeout().IsPresent());
            Assert.AreEqual(Duration.FromNanoseconds(456), healthCheck.GetTimeout().Get());
            Assert.IsTrue(healthCheck.GetStartPeriod().IsPresent());
            Assert.AreEqual(Duration.FromNanoseconds(789), healthCheck.GetStartPeriod().Get());
            Assert.IsTrue(healthCheck.GetRetries().IsPresent());
            Assert.AreEqual(10, healthCheck.GetRetries().Get());
        }

        [Test]
        public void TestBuild_invalidCommand()
        {
            try
            {
                DockerHealthCheck.FromCommand(ImmutableArray.Create<string>());
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("command must not be empty", ex.Message);
            }

            try
            {
                DockerHealthCheck.FromCommand(new[] { "CMD", null });
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("command must not contain null elements", ex.Message);
            }
        }
    }
}
