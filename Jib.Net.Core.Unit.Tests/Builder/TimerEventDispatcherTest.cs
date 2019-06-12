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
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.@event.events;
using Jib.Net.Core.Global;
using Moq;
using NodaTime;
using NUnit.Framework;
using System.Collections.Generic;
using static com.google.cloud.tools.jib.@event.events.TimerEvent;

namespace com.google.cloud.tools.jib.builder {















/** Tests for {@link TimerEventDispatcher}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class TimerEventDispatcherTest {

  private readonly Queue<TimerEvent> timerEventQueue = new Queue<TimerEvent>();

  private IClock mockClock = Mock.Of<IClock>();

  [Test]
  public void testLogging() {
    EventHandlers eventHandlers =
        EventHandlers.builder().add<TimerEvent>(timerEventQueue.Enqueue).build();

    Mock.Get(mockClock).Setup(m => m.instant()).Returns(Instant.FromUnixTimeSeconds(0));

    using (TimerEventDispatcher parentTimerEventDispatcher =
        new TimerEventDispatcher(eventHandlers, "description", mockClock, null)) {
      Mock.Get(mockClock).Setup(m => m.instant()).Returns(Instant.FromUnixTimeSeconds(0).plusMillis(1));

      parentTimerEventDispatcher.lap();
      Mock.Get(mockClock).Setup(m => m.instant()).Returns(Instant.FromUnixTimeSeconds(0).plusMillis(1).plusNanos(1));

      using (TimerEventDispatcher ignored =
          parentTimerEventDispatcher.subTimer("child description")) {
        Mock.Get(mockClock).Setup(m => m.instant()).Returns(Instant.FromUnixTimeSeconds(0).plusMillis(2));

        // Laps on close.
      }
    }

    TimerEvent timerEvent = getNextTimerEvent();
    verifyNoParent(timerEvent);
    verifyStartState(timerEvent);
    verifyDescription(timerEvent, "description");

    TimerEvent.Timer parentTimer = timerEvent.getTimer();

    timerEvent = getNextTimerEvent();
    verifyNoParent(timerEvent);
    verifyStateFirstLap(timerEvent, State.LAP);
    verifyDescription(timerEvent, "description");

    timerEvent = getNextTimerEvent();
    verifyParent(timerEvent, parentTimer);
    verifyStartState(timerEvent);
    verifyDescription(timerEvent, "child description");

    timerEvent = getNextTimerEvent();
    verifyParent(timerEvent, parentTimer);
    verifyStateFirstLap(timerEvent, State.FINISHED);
    verifyDescription(timerEvent, "child description");

    timerEvent = getNextTimerEvent();
    verifyNoParent(timerEvent);
    verifyStateNotFirstLap(timerEvent, State.FINISHED);
    verifyDescription(timerEvent, "description");

    Assert.IsTrue(timerEventQueue.Count == 0);
  }

  /**
   * Verifies that the {@code timerEvent}'s timer has no parent.
   *
   * @param timerEvent the {@link TimerEvent} to verify
   */
  private void verifyNoParent(TimerEvent timerEvent) {
    Assert.IsFalse(timerEvent.getTimer().getParent().isPresent());
  }

  /**
   * Verifies that the {@code timerEvent}'s timer has parent {@code expectedParentTimer}.
   *
   * @param timerEvent the {@link TimerEvent} to verify
   * @param expectedParentTimer the expected parent timer
   */
  private void verifyParent(TimerEvent timerEvent, TimerEvent.Timer expectedParentTimer) {
    Assert.IsTrue(timerEvent.getTimer().getParent().isPresent());
    Assert.AreSame(expectedParentTimer, timerEvent.getTimer().getParent().get());
  }

  /**
   * Verifies that the {@code timerEvent}'s state is {@link State#START}.
   *
   * @param timerEvent the {@link TimerEvent} to verify
   */
  private void verifyStartState(TimerEvent timerEvent) {
    Assert.AreEqual(State.START, timerEvent.getState());
    Assert.AreEqual(Duration.Zero, timerEvent.getDuration());
    Assert.AreEqual(Duration.Zero, timerEvent.getElapsed());
  }

  /**
   * Verifies that the {@code timerEvent}'s state is {@code expectedState} and that this is the
   * first lap for the timer.
   *
   * @param timerEvent the {@link TimerEvent} to verify
   * @param expectedState the expected {@link State}
   */
  private void verifyStateFirstLap(TimerEvent timerEvent, State expectedState) {
    Assert.AreEqual(expectedState, timerEvent.getState());
    Assert.IsTrue(timerEvent.getDuration().compareTo(Duration.Zero) > 0);
    Assert.AreEqual(0, timerEvent.getElapsed().compareTo(timerEvent.getDuration()));
  }

  /**
   * Verifies that the {@code timerEvent}'s state is {@code expectedState} and that this is not the
   * first lap for the timer.
   *
   * @param timerEvent the {@link TimerEvent} to verify
   * @param expectedState the expected {@link State}
   */
  private void verifyStateNotFirstLap(TimerEvent timerEvent, State expectedState) {
    Assert.AreEqual(expectedState, timerEvent.getState());
    Assert.IsTrue(timerEvent.getDuration().compareTo(Duration.Zero) > 0);
    Assert.IsTrue(timerEvent.getElapsed().compareTo(timerEvent.getDuration()) > 0);
  }

  /**
   * Verifies that the {@code timerEvent}'s description is {@code expectedDescription}.
   *
   * @param timerEvent the {@link TimerEvent} to verify
   * @param expectedDescription the expected description
   */
  private void verifyDescription(TimerEvent timerEvent, string expectedDescription) {
    Assert.AreEqual(expectedDescription, timerEvent.getDescription());
  }

  /**
   * Gets the next {@link TimerEvent} on the {@link #timerEventQueue}.
   *
   * @return the next {@link TimerEvent}
   */
  private TimerEvent getNextTimerEvent() {
    TimerEvent timerEvent = timerEventQueue.poll();
    Assert.IsNotNull(timerEvent);
    return timerEvent;
  }
}
}
