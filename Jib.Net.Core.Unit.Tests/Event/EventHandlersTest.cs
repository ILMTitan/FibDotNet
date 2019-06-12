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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.@event
{
    /** Tests for {@link EventHandlers}. */
    public class EventHandlersTest
    {
        /** Test {@link JibEvent}. */
        private interface TestJibEvent1 : JibEvent
        {
            string getPayload();
        }

        /** Test implementation of {@link JibEvent}. */
        private class TestJibEvent2 : JibEvent
        {
            private string message;

            public void assertMessageCorrect(string name)
            {
                Assert.AreEqual("Hello " + name, message);
            }

            public void sayHello(string name)
            {
                Assert.IsNull(message);
                message = "Hello " + name;
            }
        }

        /** Test {@link JibEvent}. */
        private class TestJibEvent3 : JibEvent { }

        [Test]
        public void testAdd()
        {
            int[] counter = new int[1];
            EventHandlers eventHandlers =
                EventHandlers.builder()
                    .add<TestJibEvent1>(
                        testJibEvent1 => Assert.AreEqual("payload", testJibEvent1.getPayload()))
                    .add<TestJibEvent2>(e => e.sayHello("Jib"))

                    .add<JibEvent>(jibEvent => counter[0]++)

                    .build();

            TestJibEvent1 mockTestJibEvent1 = Mock.Of<TestJibEvent1>();
            Mock.Get(mockTestJibEvent1).Setup(m => m.getPayload()).Returns("payload");

            TestJibEvent2 testJibEvent2 = new TestJibEvent2();

            Assert.AreEqual(2, counter[0]);
            Mock.Get(mockTestJibEvent1).Verify(m => m.getPayload());

            Mock.Get(mockTestJibEvent1).VerifyNoOtherCalls();
            testJibEvent2.assertMessageCorrect("Jib");
        }

        [Test]
        public void testDispatch()
        {
            IList<string> emissions = new List<string>();

            EventHandlers eventHandlers =
                EventHandlers.builder()
                    .add<TestJibEvent2>(_ => emissions.add("handled 2 first"))
                    .add<TestJibEvent2>(_ => emissions.add("handled 2 second"))

                    .add<TestJibEvent3>(_ => emissions.add("handled 3"))

                    .add<JibEvent>(_ => emissions.add("handled generic"))

                    .build();

            TestJibEvent2 testJibEvent2 = new TestJibEvent2();
            TestJibEvent3 testJibEvent3 = new TestJibEvent3();

            eventHandlers.dispatch(testJibEvent2);
            eventHandlers.dispatch(testJibEvent3);

            Assert.AreEqual(
                Arrays.asList(
                    "handled generic",
                    "handled 2 first",
                    "handled 2 second",
                    "handled generic",
                    "handled 3"),
                emissions);
        }
    }
}
