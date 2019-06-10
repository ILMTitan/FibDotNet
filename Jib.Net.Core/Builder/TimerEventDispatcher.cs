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

using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.@event.events;
using NodaTime;
using System;
using static com.google.cloud.tools.jib.@event.events.TimerEvent;

namespace com.google.cloud.tools.jib.builder {









/** Handles {@link Timer}s to dispatch {@link TimerEvent}s. */
public class TimerEventDispatcher : IDisposable{

        private static readonly IClock DEFAULT_CLOCK = SystemClock.Instance;

  private readonly EventHandlers eventHandlers;
  private readonly string description;

  private readonly IClock clock;
  private readonly Timer timer;

  /**
   * Creates a new {@link TimerEventDispatcher}.
   *
   * @param eventHandlers the {@link EventHandlers} used to dispatch the {@link TimerEvent}s
   * @param description the default description for the {@link TimerEvent}s
   */
  public TimerEventDispatcher(EventHandlers eventHandlers, string description) : this(eventHandlers, description, DEFAULT_CLOCK, null) {
    
  }

  TimerEventDispatcher(
      EventHandlers eventHandlers, string description, IClock clock, Timer parentTimer) {
    this.eventHandlers = eventHandlers;
    this.description = description;
    this.clock = clock;
    this.timer = new Timer(clock, parentTimer);

    dispatchTimerEvent(State.START, Duration.Zero, description);
  }

  /**
   * Creates a new {@link TimerEventDispatcher} with its parent timer as this.
   *
   * @param description a new description
   * @return the new {@link TimerEventDispatcher}
   */
  public TimerEventDispatcher subTimer(string description) {
    return new TimerEventDispatcher(eventHandlers, description, clock, timer);
  }

  /**
   * Captures the time since last lap or creation and dispatches an {@link State#LAP} {@link
   * TimerEvent}.
   *
   * @see #lap(string)
   */
  public void lap() {
    dispatchTimerEvent(State.LAP, timer.lap(), description);
  }

  /**
   * Captures the time since last lap or creation and dispatches an {@link State#LAP} {@link
   * TimerEvent}.
   *
   * @param newDescription the description to use instead of the {@link TimerEventDispatcher}'s
   *     description
   */
  public void lap(string newDescription) {
    dispatchTimerEvent(State.LAP, timer.lap(), newDescription);
  }

  /** Laps and dispatches a {@link State#FINISHED} {@link TimerEvent} upon close. */

  public void Dispose() {
    dispatchTimerEvent(State.FINISHED, timer.lap(), description);
  }

  private void dispatchTimerEvent(State state, Duration duration, string eventDescription) {
    eventHandlers.dispatch(
        new TimerEvent(state, timer, duration, timer.getElapsedTime(), eventDescription));
  }
}
}
