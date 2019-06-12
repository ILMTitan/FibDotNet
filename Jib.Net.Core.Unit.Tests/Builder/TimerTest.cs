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

using com.google.cloud.tools.jib.cache;
using Jib.Net.Core.Global;
using Moq;
using NodaTime;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.builder
{
    /** Tests for {@link Timer}. */
    [RunWith(typeof(MockitoJUnitRunner))]
    public class TimerTest
    {
        private IClock mockClock = Mock.Of<IClock>();

        [Test]
        public void testLap()
        {
            Mock.Get(mockClock).Setup(m => m.instant()).Returns(Instant.FromUnixTimeSeconds(0));

            Timer parentTimer = new Timer(mockClock, null);
            Mock.Get(mockClock).Setup(m => m.instant()).Returns(Instant.FromUnixTimeSeconds(0).plusMillis(5));

            Duration parentDuration1 = parentTimer.lap();
            Mock.Get(mockClock).Setup(m => m.instant()).Returns(Instant.FromUnixTimeSeconds(0).plusMillis(15));

            Duration parentDuration2 = parentTimer.lap();

            Mock.Get(mockClock).Setup(m => m.instant()).Returns(Instant.FromUnixTimeSeconds(0).plusMillis(16));

            Timer childTimer = new Timer(mockClock, parentTimer);
            Mock.Get(mockClock).Setup(m => m.instant()).Returns(Instant.FromUnixTimeSeconds(0).plusMillis(16).plusNanos(1));

            Duration childDuration = childTimer.lap();

            Mock.Get(mockClock).Setup(m => m.instant()).Returns(Instant.FromUnixTimeSeconds(0).plusMillis(16).plusNanos(2));

            Duration parentDuration3 = parentTimer.lap();

            Assert.IsTrue(parentDuration2.compareTo(parentDuration1) > 0);
            Assert.IsTrue(parentDuration1.compareTo(parentDuration3) > 0);
            Assert.IsTrue(parentDuration3.compareTo(childDuration) > 0);
        }
    }
}
