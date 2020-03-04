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

using Jib.Net.Core.Configuration;
using Jib.Net.Core.Events;
using NUnit.Framework;
using System.Collections.Generic;
using static Jib.Net.Core.Events.LogEvent;

namespace Jib.Net.Core.Unit.Tests.Events
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
         EventHandlers.CreateBuilder().Add<LogEvent>(receivedLogEvents.Enqueue).Build();
        }

        [Test]
        public void TestFactories()
        {
            eventHandlers.Dispatch(Error("error"));
            eventHandlers.Dispatch(Lifecycle("lifecycle"));
            eventHandlers.Dispatch(LogEvent.Progress("progress"));
            eventHandlers.Dispatch(Warn("warn"));
            eventHandlers.Dispatch(Info("info"));
            eventHandlers.Dispatch(Debug("debug"));

            VerifyNextLogEvent(Level.Error, "error");
            VerifyNextLogEvent(Level.Lifecycle, "lifecycle");
            VerifyNextLogEvent(Level.Progress, "progress");
            VerifyNextLogEvent(Level.Warn, "warn");
            VerifyNextLogEvent(Level.Info, "info");
            VerifyNextLogEvent(Level.Debug, "debug");
            Assert.IsTrue(receivedLogEvents.Count == 0);
        }

        private void VerifyNextLogEvent(Level level, string message)
        {
            Assert.IsFalse(receivedLogEvents.Count == 0);

            LogEvent logEvent = receivedLogEvents.Dequeue();

            Assert.AreEqual(level, logEvent.GetLevel());
            Assert.AreEqual(message, logEvent.GetMessage());
        }
    }
}
