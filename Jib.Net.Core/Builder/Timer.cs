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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.@event.events;
using Jib.Net.Core.Global;
using NodaTime;
using System;

namespace com.google.cloud.tools.jib.builder
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

            startTime = clock.instant();
            lapStartTime = startTime;
        }

        public Optional<TimerEvent.ITimer> getParent()
        {
            return Optional.ofNullable<TimerEvent.ITimer>(parentTimer);
        }

        /**
         * Captures the time since last lap or creation, and resets the start time.
         *
         * @return the duration of the last lap, or since creation
         */
        public Duration lap()
        {
            Instant now = clock.instant();
            Duration duration = now - lapStartTime;
            lapStartTime = now;
            return duration;
        }

        /**
         * Gets the total elapsed time since creation.
         *
         * @return the total elapsed time
         */
        public Duration getElapsedTime()
        {
            return clock.instant() - startTime;
        }
    }
}
