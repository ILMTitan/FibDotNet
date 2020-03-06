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
using Fib.Net.Core.Events;
using Fib.Net.Core.Events.Time;
using Moq;
using NodaTime;
using NUnit.Framework;
using System.Collections.Generic;
using static Fib.Net.Core.Events.TimerEvent;

namespace Fib.Net.Core.Unit.Tests.Events.Time
{
    /** Tests for {@link TimerEventDispatcher}. */
    public class TimerEventDispatcherTest
    {
        private readonly Queue<TimerEvent> timerEventQueue = new Queue<TimerEvent>();

        private readonly IClock mockClock = Mock.Of<IClock>();

        [Test]
        public void TestLogging()
        {
            EventHandlers eventHandlers =
                EventHandlers.CreateBuilder().Add<TimerEvent>(timerEventQueue.Enqueue).Build();

            Mock.Get(mockClock).Setup(m => m.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0));

            using (TimerEventDispatcher parentTimerEventDispatcher =
                new TimerEventDispatcher(eventHandlers, "description", mockClock, null))
            {
                Mock.Get(mockClock).Setup(m => m.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0) + Duration.FromMilliseconds(1));

                parentTimerEventDispatcher.Lap();
                Mock.Get(mockClock).Setup(m => m.GetCurrentInstant()).Returns((Instant.FromUnixTimeSeconds(0) + Duration.FromMilliseconds(1)).PlusNanoseconds(1));

                using (TimerEventDispatcher ignored =
                    parentTimerEventDispatcher.SubTimer("child description"))
                {
                    Mock.Get(mockClock).Setup(m => m.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0) + Duration.FromMilliseconds(2));

                    // Laps on close.
                }
            }

            TimerEvent timerEvent = GetNextTimerEvent();
            VerifyStartState(timerEvent);
            VerifyDescription(timerEvent, "description");

            timerEvent = GetNextTimerEvent();
            VerifyStateFirstLap(timerEvent, State.LAP);
            VerifyDescription(timerEvent, "description");

            timerEvent = GetNextTimerEvent();
            VerifyStartState(timerEvent);
            VerifyDescription(timerEvent, "child description");

            timerEvent = GetNextTimerEvent();
            VerifyStateFirstLap(timerEvent, State.FINISHED);
            VerifyDescription(timerEvent, "child description");

            timerEvent = GetNextTimerEvent();
            VerifyStateNotFirstLap(timerEvent, State.FINISHED);
            VerifyDescription(timerEvent, "description");

            Assert.IsTrue(timerEventQueue.Count == 0);
        }

        /**
         * Verifies that the {@code timerEvent}'s state is {@link State#START}.
         *
         * @param timerEvent the {@link TimerEvent} to verify
         */
        private void VerifyStartState(TimerEvent timerEvent)
        {
            Assert.AreEqual(State.START, timerEvent.GetState());
            Assert.AreEqual(Duration.Zero, timerEvent.GetDuration());
            Assert.AreEqual(Duration.Zero, timerEvent.GetElapsed());
        }

        /**
         * Verifies that the {@code timerEvent}'s state is {@code expectedState} and that this is the
         * first lap for the timer.
         *
         * @param timerEvent the {@link TimerEvent} to verify
         * @param expectedState the expected {@link State}
         */
        private void VerifyStateFirstLap(TimerEvent timerEvent, State expectedState)
        {
            Assert.AreEqual(expectedState, timerEvent.GetState());
            Assert.IsTrue(timerEvent.GetDuration() > Duration.Zero, timerEvent.GetDuration().ToString());
            Assert.AreEqual(timerEvent.GetElapsed(), timerEvent.GetDuration());
        }

        /**
         * Verifies that the {@code timerEvent}'s state is {@code expectedState} and that this is not the
         * first lap for the timer.
         *
         * @param timerEvent the {@link TimerEvent} to verify
         * @param expectedState the expected {@link State}
         */
        private void VerifyStateNotFirstLap(TimerEvent timerEvent, State expectedState)
        {
            Assert.AreEqual(expectedState, timerEvent.GetState());
            Assert.IsTrue(timerEvent.GetDuration().CompareTo(Duration.Zero) > 0);
            Assert.IsTrue(timerEvent.GetElapsed().CompareTo(timerEvent.GetDuration()) > 0);
        }

        /**
         * Verifies that the {@code timerEvent}'s description is {@code expectedDescription}.
         *
         * @param timerEvent the {@link TimerEvent} to verify
         * @param expectedDescription the expected description
         */
        private void VerifyDescription(TimerEvent timerEvent, string expectedDescription)
        {
            Assert.AreEqual(expectedDescription, timerEvent.GetDescription());
        }

        /**
         * Gets the next {@link TimerEvent} on the {@link #timerEventQueue}.
         *
         * @return the next {@link TimerEvent}
         */
        private TimerEvent GetNextTimerEvent()
        {
            TimerEvent timerEvent = timerEventQueue.Dequeue();
            Assert.IsNotNull(timerEvent);
            return timerEvent;
        }
    }
}
