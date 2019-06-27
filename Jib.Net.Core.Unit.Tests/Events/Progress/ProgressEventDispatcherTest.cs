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
using Jib.Net.Core.Events;
using Jib.Net.Core.Events.Progress;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace Jib.Net.Core.Unit.Tests.Events.Progress
{
    /** Tests for {@link ProgressEventDispatcher}. */
    public class ProgressEventDispatcherTest
    {
        private readonly IEventHandlers mockEventHandlers = Mock.Of<IEventHandlers>();

        [Test]
        public void TestDispatch()
        {
            var progressEvents = new List<ProgressEvent>();
            Mock.Get(mockEventHandlers)
                .Setup(m => m.Dispatch(It.IsAny<ProgressEvent>()))
                .Callback((IJibEvent e) => progressEvents.Add((ProgressEvent)e));
            using (ProgressEventDispatcher progressEventDispatcher =
                    ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 10))
            using (ProgressEventDispatcher ignored =
                    progressEventDispatcher.NewChildProducer().Create("ignored", 20))
            {

                // empty
            }

            Mock.Get(mockEventHandlers).Verify(m => m.Dispatch(It.IsAny<ProgressEvent>()), Times.Exactly(4));

            Assert.AreSame(progressEvents[0].GetAllocation(), progressEvents[3].GetAllocation());
            Assert.AreSame(progressEvents[1].GetAllocation(), progressEvents[2].GetAllocation());

            Assert.AreEqual(0, progressEvents[0].GetUnits());
            Assert.AreEqual(0, progressEvents[1].GetUnits());
            Assert.AreEqual(20, progressEvents[2].GetUnits());
            Assert.AreEqual(9, progressEvents[3].GetUnits());
        }

        [Test]
        public void TestDispatch_safeWithtooMuchProgress()
        {
            var progressEvents = new List<ProgressEvent>();
            Mock.Get(mockEventHandlers).Setup(m => m.Dispatch(It.IsAny<ProgressEvent>()))
                .Callback((IJibEvent e) => progressEvents.Add((ProgressEvent)e));
            using (ProgressEventDispatcher progressEventDispatcher =
                ProgressEventDispatcher.NewRoot(mockEventHandlers, "allocation description", 10))
            {
                progressEventDispatcher.DispatchProgress(6);
                progressEventDispatcher.DispatchProgress(8);
                progressEventDispatcher.DispatchProgress(1);
            }

            Assert.AreEqual(4, progressEvents.Count);
            Assert.AreSame(progressEvents[0].GetAllocation(), progressEvents[1].GetAllocation());
            Assert.AreSame(progressEvents[1].GetAllocation(), progressEvents[2].GetAllocation());
            Assert.AreSame(progressEvents[2].GetAllocation(), progressEvents[3].GetAllocation());

            Assert.AreEqual(10, progressEvents[0].GetAllocation().GetAllocationUnits());

            Assert.AreEqual(0, progressEvents[0].GetUnits());
            Assert.AreEqual(6, progressEvents[1].GetUnits());
            Assert.AreEqual(4, progressEvents[2].GetUnits());
            Assert.AreEqual(0, progressEvents[3].GetUnits());
        }

        [Test]
        public void TestDispatch_safeWithTooManyChildren()
        {
            var progressEvents = new List<ProgressEvent>();
            Mock.Get(mockEventHandlers).Setup(m => m.Dispatch(It.IsAny<ProgressEvent>()))
                .Callback((IJibEvent e) => progressEvents.Add((ProgressEvent)e));
            using (ProgressEventDispatcher progressEventDispatcher =
                    ProgressEventDispatcher.NewRoot(mockEventHandlers, "allocation description", 1))
            using (ProgressEventDispatcher ignored1 =
                    progressEventDispatcher.NewChildProducer().Create("ignored", 5))
            using (ProgressEventDispatcher ignored2 =
                    progressEventDispatcher.NewChildProducer().Create("ignored", 4))
            {

                // empty
            }

            Assert.AreEqual(5, progressEvents.Count);
            Assert.AreEqual(1, progressEvents[0].GetAllocation().GetAllocationUnits());
            Assert.AreEqual(5, progressEvents[1].GetAllocation().GetAllocationUnits());
            Assert.AreEqual(4, progressEvents[2].GetAllocation().GetAllocationUnits());

            // child1 (of allocation 5) opening and closing
            Assert.AreSame(progressEvents[1].GetAllocation(), progressEvents[4].GetAllocation());
            // child1 (of allocation 4) opening and closing
            Assert.AreSame(progressEvents[2].GetAllocation(), progressEvents[3].GetAllocation());

            Assert.AreEqual(0, progressEvents[0].GetUnits()); // 0-progress sent when root creation
            Assert.AreEqual(0, progressEvents[1].GetUnits()); // when child1 creation
            Assert.AreEqual(0, progressEvents[2].GetUnits()); // when child2 creation
            Assert.AreEqual(4, progressEvents[3].GetUnits()); // when child2 closes
            Assert.AreEqual(5, progressEvents[4].GetUnits()); // when child1 closes
        }
    }
}
