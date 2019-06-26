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
         EventHandlers.CreateBuilder().Add<LogEvent>(receivedLogEvents.Add).Build();
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

            VerifyNextLogEvent(Level.ERROR, "error");
            VerifyNextLogEvent(Level.LIFECYCLE, "lifecycle");
            VerifyNextLogEvent(Level.PROGRESS, "progress");
            VerifyNextLogEvent(Level.WARN, "warn");
            VerifyNextLogEvent(Level.INFO, "info");
            VerifyNextLogEvent(Level.DEBUG, "debug");
            Assert.IsTrue(receivedLogEvents.IsEmpty());
        }

        private void VerifyNextLogEvent(Level level, string message)
        {
            Assert.IsFalse(receivedLogEvents.IsEmpty());

            LogEvent logEvent = receivedLogEvents.Poll();

            Assert.AreEqual(level, logEvent.GetLevel());
            Assert.AreEqual(message, logEvent.GetMessage());
        }
    }
}
