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

using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.@event.progress;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.@event.events
{
    /** Tests for {@link ProgressEvent}. */
    public class ProgressEventTest
    {
        /** The root node. */
        private Allocation root;

        /** First child of the root node. */
        private Allocation child1;

        /** Child of the first child of the root node. */
        private Allocation child1Child;

        /** Second child of the root node. */
        private Allocation child2;
        

        [SetUp]
        public void SetUp()
        {
            root = Allocation.newRoot("ignored", 2);
            child1 = root.newChild("ignored", 1);
            child1Child = child1.newChild("ignored", 100);
            child2 = root.newChild("ignored", 200);
        }

        private static EventHandlers makeEventHandlers(Action<ProgressEvent> progressEventConsumer)
        {
            return EventHandlers.builder().add<ProgressEvent>(progressEventConsumer).build();
        }

        private static readonly double DOUBLE_ERROR_MARGIN = 1e-10;

        private readonly IDictionary<Allocation, long> allocationCompletionMap = new Dictionary<Allocation, long>();

        private double progress = 0.0;

        [Test]
        public void testAccumulateProgress()
        {
            Consumer<ProgressEvent> progressEventConsumer =
                progressEvent =>
                {
                    double fractionOfRoot = progressEvent.getAllocation().getFractionOfRoot();
                    long units = progressEvent.getUnits();

                    progress += units * fractionOfRoot;
                };

            EventHandlers eventHandlers = makeEventHandlers(progressEventConsumer);

            eventHandlers.dispatch(new ProgressEvent(child1Child, 50));
            Assert.AreEqual(1.0 / 2 / 100 * 50, progress, DOUBLE_ERROR_MARGIN);

            eventHandlers.dispatch(new ProgressEvent(child1Child, 50));
            Assert.AreEqual(1.0 / 2, progress, DOUBLE_ERROR_MARGIN);

            eventHandlers.dispatch(new ProgressEvent(child2, 10));
            Assert.AreEqual(1.0 / 2 + (1.0 / 2 / 200 * 10), progress, DOUBLE_ERROR_MARGIN);

            eventHandlers.dispatch(new ProgressEvent(child2, 190));
            Assert.AreEqual(1.0, progress, DOUBLE_ERROR_MARGIN);
        }

        private EventHandlers makeEventHandlers(Consumer<ProgressEvent> progressEventConsumer)
        {
            return EventHandlers.builder().add<ProgressEvent>(e => progressEventConsumer(e)).build();
        }

        [Test]
        public void testSmoke()
        {
            Consumer<ProgressEvent> progressEventConsumer =
                progressEvent =>
                {
                    Allocation allocation = progressEvent.getAllocation();
                    long units = progressEvent.getUnits();

                    updateCompletionMap(allocation, units);
                };

            EventHandlers eventHandlers = makeEventHandlers(progressEventConsumer);

            eventHandlers.dispatch(new ProgressEvent(child1Child, 50));

            Assert.AreEqual(1, allocationCompletionMap.size());
            Assert.AreEqual(50, allocationCompletionMap.get(child1Child).longValue());

            eventHandlers.dispatch(new ProgressEvent(child1Child, 50));

            Assert.AreEqual(3, allocationCompletionMap.size());
            Assert.AreEqual(100, allocationCompletionMap.get(child1Child).longValue());
            Assert.AreEqual(1, allocationCompletionMap.get(child1).longValue());
            Assert.AreEqual(1, allocationCompletionMap.get(root).longValue());

            eventHandlers.dispatch(new ProgressEvent(child2, 200));

            Assert.AreEqual(4, allocationCompletionMap.size());
            Assert.AreEqual(100, allocationCompletionMap.get(child1Child).longValue());
            Assert.AreEqual(1, allocationCompletionMap.get(child1).longValue());
            Assert.AreEqual(200, allocationCompletionMap.get(child2).longValue());
            Assert.AreEqual(2, allocationCompletionMap.get(root).longValue());
        }

        [Test]
        public void testType()
        {
            // Used to test whether or not progress event was consumed
            bool[] called = new bool[] { false };
            Consumer<ProgressEvent> buildImageConsumer =
                progressEvent =>
                {
                    called[0] = true;
                };

            EventHandlers eventHandlers = makeEventHandlers(buildImageConsumer);
            eventHandlers.dispatch(new ProgressEvent(child1, 50));
            Assert.IsTrue(called[0]);
        }

        /**
         * Updates the {@link #allocationCompletionMap} with {@code units} more progress for {@code
         * allocation}. This also updates {@link Allocation} parents if {@code allocation} is complete in
         * terms of progress.
         *
         * @param allocation the allocation the progress is made on
         * @param units the progress units
         */
        private void updateCompletionMap(Allocation allocation, long units)
        {
            if (allocationCompletionMap.containsKey(allocation))
            {
                units += allocationCompletionMap.get(allocation);
            }
            allocationCompletionMap.put(allocation, units);

            if (allocation.getAllocationUnits() == units)
            {
                allocation
                    .getParent()
                    .ifPresent(parentAllocation => updateCompletionMap(parentAllocation, 1));
            }
        }
    }
}
