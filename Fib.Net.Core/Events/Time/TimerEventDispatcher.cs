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

using Fib.Net.Core.Configuration;
using NodaTime;
using System;
using static Fib.Net.Core.Events.TimerEvent;

namespace Fib.Net.Core.Events.Time
{
    /** Handles {@link Timer}s to dispatch {@link TimerEvent}s. */
    public sealed class TimerEventDispatcher : IDisposable
    {
        private static readonly IClock DEFAULT_CLOCK = SystemClock.Instance;

        private readonly IEventHandlers eventHandlers;
        private readonly string description;

        private readonly IClock clock;
        private readonly Timer timer;

        /**
         * Creates a new {@link TimerEventDispatcher}.
         *
         * @param eventHandlers the {@link EventHandlers} used to dispatch the {@link TimerEvent}s
         * @param description the default description for the {@link TimerEvent}s
         */
        public TimerEventDispatcher(IEventHandlers eventHandlers, string description) : this(eventHandlers, description, DEFAULT_CLOCK, null)
        {
        }

        public TimerEventDispatcher(
            IEventHandlers eventHandlers, string description, IClock clock, Timer parentTimer)
        {
            this.eventHandlers = eventHandlers;
            this.description = description;
            this.clock = clock;
            timer = new Timer(clock, parentTimer);

            DispatchTimerEvent(State.START, Duration.Zero, description);
        }

        /**
         * Creates a new {@link TimerEventDispatcher} with its parent timer as this.
         *
         * @param description a new description
         * @return the new {@link TimerEventDispatcher}
         */
        public TimerEventDispatcher SubTimer(string description)
        {
            return new TimerEventDispatcher(eventHandlers, description, clock, timer);
        }

        /**
         * Captures the time since last lap or creation and dispatches an {@link State#LAP} {@link
         * TimerEvent}.
         *
         * @see #lap(string)
         */
        public void Lap()
        {
            DispatchTimerEvent(State.LAP, timer.Lap(), description);
        }

        /**
         * Captures the time since last lap or creation and dispatches an {@link State#LAP} {@link
         * TimerEvent}.
         *
         * @param newDescription the description to use instead of the {@link TimerEventDispatcher}'s
         *     description
         */
        public void Lap(string newDescription)
        {
            DispatchTimerEvent(State.LAP, timer.Lap(), newDescription);
        }

        /** Laps and dispatches a {@link State#FINISHED} {@link TimerEvent} upon close. */

        public void Dispose()
        {
            DispatchTimerEvent(State.FINISHED, timer.Lap(), description);
        }

        private void DispatchTimerEvent(State state, Duration duration, string eventDescription)
        {
            eventHandlers.Dispatch(
                new TimerEvent(state, duration, timer.GetElapsedTime(), eventDescription));
        }
    }
}
