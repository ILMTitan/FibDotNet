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
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.configuration
{
    /** Tests for {@link DockerHealthCheck}. */
    public class DockerHealthCheckTest
    {
        [Test]
        public void testBuild()
        {
            DockerHealthCheck healthCheck =
                DockerHealthCheck.fromCommand(ImmutableArray.Create("echo", "hi"))
                    .setInterval(Duration.FromNanoseconds(123))
                    .setTimeout(Duration.FromNanoseconds(456))
                    .setStartPeriod(Duration.FromNanoseconds(789))
                    .setRetries(10)
                    .build();

            Assert.IsTrue(healthCheck.getInterval().isPresent());
            Assert.AreEqual(Duration.FromNanoseconds(123), healthCheck.getInterval().get());
            Assert.IsTrue(healthCheck.getTimeout().isPresent());
            Assert.AreEqual(Duration.FromNanoseconds(456), healthCheck.getTimeout().get());
            Assert.IsTrue(healthCheck.getStartPeriod().isPresent());
            Assert.AreEqual(Duration.FromNanoseconds(789), healthCheck.getStartPeriod().get());
            Assert.IsTrue(healthCheck.getRetries().isPresent());
            Assert.AreEqual(10, (int)healthCheck.getRetries().get());
        }

        [Test]
        public void testBuild_invalidCommand()
        {
            try
            {
                DockerHealthCheck.fromCommand(ImmutableArray.Create<string>());
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("command must not be empty", ex.getMessage());
            }

            try
            {
                DockerHealthCheck.fromCommand(Arrays.asList("CMD", null));
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("command must not contain null elements", ex.getMessage());
            }
        }
    }
}
