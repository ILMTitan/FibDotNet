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

using Fib.Net.Core.Events.Time;
using Moq;
using NodaTime;
using NUnit.Framework;

namespace Fib.Net.Core.Unit.Tests.Events.Time
{
    /** Tests for {@link Timer}. */
    public class TimerTest
    {
        private readonly IClock mockClock = Mock.Of<IClock>();

        [Test]
        public void TestLap()
        {
            Mock.Get(mockClock).Setup(m => m.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0));

            Timer parentTimer = new Timer(mockClock, null);
            Mock.Get(mockClock).Setup(m => m.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0) + Duration.FromMilliseconds(5));

            Duration parentDuration1 = parentTimer.Lap();
            Mock.Get(mockClock).Setup(m => m.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0) + Duration.FromMilliseconds(15));

            Duration parentDuration2 = parentTimer.Lap();

            Mock.Get(mockClock).Setup(m => m.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0) + Duration.FromMilliseconds(16));

            Timer childTimer = new Timer(mockClock, parentTimer);
            Mock.Get(mockClock).Setup(m => m.GetCurrentInstant()).Returns((Instant.FromUnixTimeSeconds(0) + Duration.FromMilliseconds(16)).PlusNanoseconds(1));

            Duration childDuration = childTimer.Lap();

            Mock.Get(mockClock).Setup(m => m.GetCurrentInstant()).Returns((Instant.FromUnixTimeSeconds(0) + Duration.FromMilliseconds(16)).PlusNanoseconds(2));

            Duration parentDuration3 = parentTimer.Lap();

            Assert.IsTrue(parentDuration2.CompareTo(parentDuration1) > 0);
            Assert.IsTrue(parentDuration1.CompareTo(parentDuration3) > 0);
            Assert.IsTrue(parentDuration3.CompareTo(childDuration) > 0);
        }
    }
}
