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

using Fib.Net.Core.Api;
using NodaTime;

namespace Fib.Net.Core.Events
{
    /**
     * Timer event for timing various part of Fib's execution.
     *
     * <p>Timer events follow a specific {@link Timer} through a {@link State#START}, {@link State#LAP},
     * and {@link State#FINISHED} states. The duration indicates the duration since the last {@link
     * TimerEvent} dispatched for the {@link Timer}.
     *
     * <p>Timers can also define a hierarchy.
     */
    public class TimerEvent : IFibEvent
    {
        /** The state of the timing. */
        public enum State
        {

            /** The timer has started timing. {@link #getDuration} is 0. {@link #getElapsed} is 0. */
            START,

            /**
             * The timer timed a lap. {@link #getDuration} is the time since the last event. {@link
             * #getElapsed} is the total elapsed time.
             */
            LAP,

            /**
             * The timer has finished timing. {@link #getDuration} is the time since the last event. {@link
             * #getElapsed} is the total elapsed time.
             */
            FINISHED
        }

        /** Defines a timer hierarchy. */
        public interface ITimer
        {
            /**
             * Gets the parent of this {@link Timer}.
             *
             * @return the parent of this {@link Timer}
             */
            Maybe<ITimer> GetParent();
        }

        private readonly State state;
        private readonly Duration lapDuration;
        private readonly Duration totalDuration;
        private readonly string description;

        /**
         * Creates a new {@link TimerEvent}. For internal use only.
         *
         * @param state the state of the {@link Timer}
         * @param timer the {@link Timer}
         * @param duration the lap duration
         * @param elapsed the total elapsed time since the timer was created
         * @param description the description of this event
         */
        public TimerEvent(
            State state, Duration lapDuration, Duration totalDuration, string description)
        {
            this.state = state;
            this.lapDuration = lapDuration;
            this.totalDuration = totalDuration;
            this.description = description;
        }

        /**
         * Gets the state of the timer.
         *
         * @return the state of the timer
         * @see State
         */
        public State GetState()
        {
            return state;
        }

        /**
         * Gets the duration since the last {@link TimerEvent} for this timer.
         *
         * @return the duration since the last {@link TimerEvent} for this timer.
         */
        public Duration GetDuration()
        {
            return lapDuration;
        }

        /**
         * Gets the total elapsed duration since this timer was created.
         *
         * @return the duration since this timer was created
         */
        public Duration GetElapsed()
        {
            return totalDuration;
        }

        /**
         * Gets the description associated with this event.
         *
         * @return the description
         */
        public string GetDescription()
        {
            return description;
        }

        public override string ToString()
        {
            return $"TimerEvent:{state:G}:\"{lapDuration}\":\"{totalDuration}\":{description}";
        }
    }
}
