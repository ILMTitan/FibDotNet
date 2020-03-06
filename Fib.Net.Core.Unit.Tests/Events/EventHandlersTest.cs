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

using Fib.Net.Core.Api;
using Fib.Net.Core.Configuration;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace Fib.Net.Core.Unit.Tests.Events
{
    /** Tests for {@link EventHandlers}. */
    public class EventHandlersTest
    {
        /** Test {@link FibEvent}. */
        public interface ITestFibEvent1 : IFibEvent
        {
            string GetPayload();
        }

        /** Test implementation of {@link FibEvent}. */
        private class TestFibEvent2 : IFibEvent
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

        /** Test {@link FibEvent}. */
        private class TestFibEvent3 : IFibEvent { }

        [Test]
        public void TestAdd()
        {
            ITestFibEvent1 mockTestFibEvent1 = Mock.Of<ITestFibEvent1>();
            Mock.Get(mockTestFibEvent1).Setup(m => m.GetPayload()).Returns("payload");
            TestFibEvent2 testFibEvent2 = new TestFibEvent2();

            int counter = 0;
            EventHandlers eventHandlers =
                EventHandlers.CreateBuilder()
                    .Add<ITestFibEvent1>(
                        testFibEvent1 => Assert.AreEqual("payload", testFibEvent1.GetPayload()))
                    .Add<TestFibEvent2>(e => e.SayHello("Fib"))
                    .Add<IFibEvent>(_ => counter++)
                    .Build();

            eventHandlers.Dispatch(mockTestFibEvent1);
            eventHandlers.Dispatch(testFibEvent2);

            Assert.AreEqual(2, counter);
            Mock.Get(mockTestFibEvent1).Verify(m => m.GetPayload());
            Mock.Get(mockTestFibEvent1).VerifyNoOtherCalls();
            testFibEvent2.AssertMessageCorrect("Fib");
        }

        [Test]
        public void TestDispatch()
        {
            IList<string> emissions = new List<string>();

            EventHandlers eventHandlers =
                EventHandlers.CreateBuilder()
                    .Add<TestFibEvent2>(_ => emissions.Add("handled 2 first"))
                    .Add<TestFibEvent2>(_ => emissions.Add("handled 2 second"))

                    .Add<TestFibEvent3>(_ => emissions.Add("handled 3"))

                    .Add<IFibEvent>(_ => emissions.Add("handled generic"))

                    .Build();

            TestFibEvent2 testFibEvent2 = new TestFibEvent2();
            TestFibEvent3 testFibEvent3 = new TestFibEvent3();

            eventHandlers.Dispatch(testFibEvent2);
            eventHandlers.Dispatch(testFibEvent3);

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
