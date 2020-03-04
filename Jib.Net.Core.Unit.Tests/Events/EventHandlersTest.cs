/*
 * Copyright 2018 Google LLC. All rights reserved.
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

using Jib.Net.Core.Api;
using Jib.Net.Core.Configuration;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace Jib.Net.Core.Unit.Tests.Events
{
    /** Tests for {@link EventHandlers}. */
    public class EventHandlersTest
    {
        /** Test {@link JibEvent}. */
        public interface ITestJibEvent1 : IJibEvent
        {
            string GetPayload();
        }

        /** Test implementation of {@link JibEvent}. */
        private class TestJibEvent2 : IJibEvent
        {
            private string message;

            public void AssertMessageCorrect(string name)
            {
                Assert.AreEqual("Hello " + name, message);
            }

            public void SayHello(string name)
            {
                Assert.IsNull(message);
                message = "Hello " + name;
            }
        }

        /** Test {@link JibEvent}. */
        private class TestJibEvent3 : IJibEvent { }

        [Test]
        public void TestAdd()
        {
            ITestJibEvent1 mockTestJibEvent1 = Mock.Of<ITestJibEvent1>();
            Mock.Get(mockTestJibEvent1).Setup(m => m.GetPayload()).Returns("payload");
            TestJibEvent2 testJibEvent2 = new TestJibEvent2();

            int counter = 0;
            EventHandlers eventHandlers =
                EventHandlers.CreateBuilder()
                    .Add<ITestJibEvent1>(
                        testJibEvent1 => Assert.AreEqual("payload", testJibEvent1.GetPayload()))
                    .Add<TestJibEvent2>(e => e.SayHello("Jib"))
                    .Add<IJibEvent>(_ => counter++)
                    .Build();

            eventHandlers.Dispatch(mockTestJibEvent1);
            eventHandlers.Dispatch(testJibEvent2);

            Assert.AreEqual(2, counter);
            Mock.Get(mockTestJibEvent1).Verify(m => m.GetPayload());
            Mock.Get(mockTestJibEvent1).VerifyNoOtherCalls();
            testJibEvent2.AssertMessageCorrect("Jib");
        }

        [Test]
        public void TestDispatch()
        {
            IList<string> emissions = new List<string>();

            EventHandlers eventHandlers =
                EventHandlers.CreateBuilder()
                    .Add<TestJibEvent2>(_ => JavaExtensions.Add(emissions, "handled 2 first"))
                    .Add<TestJibEvent2>(_ => JavaExtensions.Add(emissions, "handled 2 second"))

                    .Add<TestJibEvent3>(_ => JavaExtensions.Add(emissions, "handled 3"))

                    .Add<IJibEvent>(_ => JavaExtensions.Add(emissions, "handled generic"))

                    .Build();

            TestJibEvent2 testJibEvent2 = new TestJibEvent2();
            TestJibEvent3 testJibEvent3 = new TestJibEvent3();

            eventHandlers.Dispatch(testJibEvent2);
            eventHandlers.Dispatch(testJibEvent3);

            CollectionAssert.AreEqual(
                new[]
                {
                    "handled 2 first",
                    "handled 2 second",
                    "handled generic",
                    "handled 3",
                    "handled generic"
                },
                emissions);
        }
    }
}
