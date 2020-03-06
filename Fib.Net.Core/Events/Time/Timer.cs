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
using System;

namespace Fib.Net.Core.Events.Time
{
    /** Times code execution intervals. Call {@link #lap} at the end of each interval. */
    public class Timer : TimerEvent.ITimer
    {
        private readonly IClock clock;
        private readonly Timer parentTimer;

        private readonly Instant startTime;
        private Instant lapStartTime;

        public Timer(IClock clock, Timer parentTimer)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.parentTimer = parentTimer;

            startTime = clock.GetCurrentInstant();
            lapStartTime = startTime;
        }

        public Maybe<TimerEvent.ITimer> GetParent()
        {
            return Maybe.OfNullable<TimerEvent.ITimer>(parentTimer);
        }

        /**
         * Captures the time since last lap or creation, and resets the start time.
         *
         * @return the duration of the last lap, or since creation
         */
        public Duration Lap()
        {
            Instant now = clock.GetCurrentInstant();
            Duration duration = now - lapStartTime;
            lapStartTime = now;
            return duration;
        }

        /**
         * Gets the total elapsed time since creation.
         *
         * @return the total elapsed time
         */
        public Duration GetElapsedTime()
        {
            return clock.GetCurrentInstant() - startTime;
        }
    }
}
