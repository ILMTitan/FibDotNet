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
using NUnit.Framework;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.builder
{

    /** Tests for {@link ProgressEventDispatcher}. */
    [RunWith(typeof(MockitoJUnitRunner))]
    public class ProgressEventDispatcherTest
    {
        private EventHandlers mockEventHandlers = Mock.Of<EventHandlers>();

        [Test]
        public void testDispatch()
        {
            using (ProgressEventDispatcher progressEventDispatcher =
                    ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 10))
            using (ProgressEventDispatcher ignored =
                    progressEventDispatcher.newChildProducer().create("ignored", 20))
            {

                // empty
            }

            ArgumentCaptor<ProgressEvent> progressEventArgumentCaptor =
                ArgumentCaptor.forClass<ProgressEvent>();
            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(progressEventArgumentCaptor.capture()), Times.Exactly(4));
            IList<ProgressEvent> progressEvents = progressEventArgumentCaptor.getAllValues();

            Assert.AreSame(progressEvents.get(0).getAllocation(), progressEvents.get(3).getAllocation());
            Assert.AreSame(progressEvents.get(1).getAllocation(), progressEvents.get(2).getAllocation());

            Assert.AreEqual(0, progressEvents.get(0).getUnits());
            Assert.AreEqual(0, progressEvents.get(1).getUnits());
            Assert.AreEqual(20, progressEvents.get(2).getUnits());
            Assert.AreEqual(9, progressEvents.get(3).getUnits());
        }

        [Test]
        public void testDispatch_safeWithtooMuchProgress()
        {
            using (ProgressEventDispatcher progressEventDispatcher =
                ProgressEventDispatcher.newRoot(mockEventHandlers, "allocation description", 10))
            {
                progressEventDispatcher.dispatchProgress(6);
                progressEventDispatcher.dispatchProgress(8);
                progressEventDispatcher.dispatchProgress(1);
            }

            ArgumentCaptor<ProgressEvent> eventsCaptor = ArgumentCaptor.forClass<ProgressEvent>();
            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(eventsCaptor.capture()), Times.Exactly(4));
            IList<ProgressEvent> progressEvents = eventsCaptor.getAllValues();

            Assert.AreSame(progressEvents.get(0).getAllocation(), progressEvents.get(1).getAllocation());
            Assert.AreSame(progressEvents.get(1).getAllocation(), progressEvents.get(2).getAllocation());
            Assert.AreSame(progressEvents.get(2).getAllocation(), progressEvents.get(3).getAllocation());

            Assert.AreEqual(10, progressEvents.get(0).getAllocation().getAllocationUnits());

            Assert.AreEqual(0, progressEvents.get(0).getUnits());
            Assert.AreEqual(6, progressEvents.get(1).getUnits());
            Assert.AreEqual(4, progressEvents.get(2).getUnits());
            Assert.AreEqual(0, progressEvents.get(3).getUnits());
        }

        [Test]
        public void testDispatch_safeWithTooManyChildren()
        {
            using (ProgressEventDispatcher progressEventDispatcher =
                    ProgressEventDispatcher.newRoot(mockEventHandlers, "allocation description", 1))
            using (ProgressEventDispatcher ignored1 =
                    progressEventDispatcher.newChildProducer().create("ignored", 5))
            using (ProgressEventDispatcher ignored2 =
                    progressEventDispatcher.newChildProducer().create("ignored", 4))
            {

                // empty
            }

            ArgumentCaptor<ProgressEvent> eventsCaptor = ArgumentCaptor.forClass<ProgressEvent>();
            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(eventsCaptor.capture()), Times.Exactly(5));
            IList<ProgressEvent> progressEvents = eventsCaptor.getAllValues();

            Assert.AreEqual(1, progressEvents.get(0).getAllocation().getAllocationUnits());
            Assert.AreEqual(5, progressEvents.get(1).getAllocation().getAllocationUnits());
            Assert.AreEqual(4, progressEvents.get(2).getAllocation().getAllocationUnits());

            // child1 (of allocation 5) opening and closing
            Assert.AreSame(progressEvents.get(1).getAllocation(), progressEvents.get(4).getAllocation());
            // child1 (of allocation 4) opening and closing
            Assert.AreSame(progressEvents.get(2).getAllocation(), progressEvents.get(3).getAllocation());

            Assert.AreEqual(0, progressEvents.get(0).getUnits()); // 0-progress sent when root creation
            Assert.AreEqual(0, progressEvents.get(1).getUnits()); // when child1 creation
            Assert.AreEqual(0, progressEvents.get(2).getUnits()); // when child2 creation
            Assert.AreEqual(4, progressEvents.get(3).getUnits()); // when child2 closes
            Assert.AreEqual(5, progressEvents.get(4).getUnits()); // when child1 closes
        }
    }
}
