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

namespace com.google.cloud.tools.jib.event.events {








/** Tests for {@link LogEvent}. */
public class LogEventTest {

  private readonly Deque<LogEvent> receivedLogEvents = new ArrayDeque<>();

  // Note that in actual code, the event handler should NOT perform thread unsafe operations like
  // here.
  private readonly EventHandlers eventHandlers =
      EventHandlers.builder().add(typeof(LogEvent), receivedLogEvents.offer).build();

  [TestMethod]
  public void testFactories() {
    eventHandlers.dispatch(LogEvent.error("error"));
    eventHandlers.dispatch(LogEvent.lifecycle("lifecycle"));
    eventHandlers.dispatch(LogEvent.progress("progress"));
    eventHandlers.dispatch(LogEvent.warn("warn"));
    eventHandlers.dispatch(LogEvent.info("info"));
    eventHandlers.dispatch(LogEvent.debug("debug"));

    verifyNextLogEvent(Level.ERROR, "error");
    verifyNextLogEvent(Level.LIFECYCLE, "lifecycle");
    verifyNextLogEvent(Level.PROGRESS, "progress");
    verifyNextLogEvent(Level.WARN, "warn");
    verifyNextLogEvent(Level.INFO, "info");
    verifyNextLogEvent(Level.DEBUG, "debug");
    Assert.assertTrue(receivedLogEvents.isEmpty());
  }

  private void verifyNextLogEvent(Level level, string message) {
    Assert.assertFalse(receivedLogEvents.isEmpty());

    LogEvent logEvent = receivedLogEvents.poll();

    Assert.assertEquals(level, logEvent.getLevel());
    Assert.assertEquals(message, logEvent.getMessage());
  }
}
}
