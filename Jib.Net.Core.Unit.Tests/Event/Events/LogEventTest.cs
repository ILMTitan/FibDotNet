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
using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.Collections.Generic;
using static com.google.cloud.tools.jib.api.LogEvent;

namespace com.google.cloud.tools.jib.@event.events
{
    /** Tests for {@link LogEvent}. */
    public class LogEventTest
    {
        private readonly Queue<LogEvent> receivedLogEvents = new Queue<LogEvent>();

        // Note that in actual code, the event handler should NOT perform thread unsafe operations like
        // here.
        private readonly EventHandlers eventHandlers;

        public LogEventTest()
        {
            eventHandlers =
         EventHandlers.builder().add<LogEvent>(typeof(LogEvent), receivedLogEvents.add).build();
        }

        [Test]
        public void testFactories()
        {
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
            Assert.IsTrue(receivedLogEvents.isEmpty());
        }

        private void verifyNextLogEvent(Level level, string message)
        {
            Assert.IsFalse(receivedLogEvents.isEmpty());

            LogEvent logEvent = receivedLogEvents.poll();

            Assert.AreEqual(level, logEvent.getLevel());
            Assert.AreEqual(message, logEvent.getMessage());
        }
    }
}
