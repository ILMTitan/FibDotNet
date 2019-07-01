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
using Jib.Net.Core;
using Jib.Net.Core.Events;
using Jib.Net.Core.Events.Progress;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Jib.Net.Core.Unit.Tests.Events
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
            root = Allocation.NewRoot("ignored", 2);
            child1 = root.NewChild("ignored", 1);
            child1Child = child1.NewChild("ignored", 100);
            child2 = root.NewChild("ignored", 200);
        }

        private static EventHandlers MakeEventHandlers(Action<ProgressEvent> progressEventConsumer)
        {
            return EventHandlers.CreateBuilder().Add(progressEventConsumer).Build();
        }

        private const double DOUBLE_ERROR_MARGIN = 1e-10;

        private readonly IDictionary<Allocation, long> allocationCompletionMap = new Dictionary<Allocation, long>();

        private double progress = 0.0;

        [Test]
        public void TestAccumulateProgress()
        {
            void progressEventConsumer(ProgressEvent progressEvent)
            {
                double fractionOfRoot = progressEvent.GetAllocation().GetFractionOfRoot();
                long units = progressEvent.GetUnits();

                progress += units * fractionOfRoot;
            }

            EventHandlers eventHandlers = MakeEventHandlers(progressEventConsumer);

            eventHandlers.Dispatch(new ProgressEvent(child1Child, 50));
            Assert.AreEqual(1.0 / 2 / 100 * 50, progress, DOUBLE_ERROR_MARGIN);

            eventHandlers.Dispatch(new ProgressEvent(child1Child, 50));
            Assert.AreEqual(1.0 / 2, progress, DOUBLE_ERROR_MARGIN);

            eventHandlers.Dispatch(new ProgressEvent(child2, 10));
            Assert.AreEqual((1.0 / 2) + (1.0 / 2 / 200 * 10), progress, DOUBLE_ERROR_MARGIN);

            eventHandlers.Dispatch(new ProgressEvent(child2, 190));
            Assert.AreEqual(1.0, progress, DOUBLE_ERROR_MARGIN);
        }

        [Test]
        public void TestSmoke()
        {
            void progressEventConsumer(ProgressEvent progressEvent)
            {
                Allocation allocation = progressEvent.GetAllocation();
                long units = progressEvent.GetUnits();

                UpdateCompletionMap(allocation, units);
            }

            EventHandlers eventHandlers = MakeEventHandlers(progressEventConsumer);

            eventHandlers.Dispatch(new ProgressEvent(child1Child, 50));

            Assert.AreEqual(1, allocationCompletionMap.Count);
            Assert.AreEqual(50, allocationCompletionMap.Get(child1Child));

            eventHandlers.Dispatch(new ProgressEvent(child1Child, 50));

            Assert.AreEqual(3, allocationCompletionMap.Count);
            Assert.AreEqual(100, allocationCompletionMap.Get(child1Child));
            Assert.AreEqual(1, allocationCompletionMap.Get(child1));
            Assert.AreEqual(1, allocationCompletionMap.Get(root));

            eventHandlers.Dispatch(new ProgressEvent(child2, 200));

            Assert.AreEqual(4, allocationCompletionMap.Count);
            Assert.AreEqual(100, allocationCompletionMap.Get(child1Child));
            Assert.AreEqual(1, allocationCompletionMap.Get(child1));
            Assert.AreEqual(200, allocationCompletionMap.Get(child2));
            Assert.AreEqual(2, allocationCompletionMap.Get(root));
        }

        [Test]
        public void TestType()
        {
            // Used to test whether or not progress event was consumed
            bool[] called = new bool[] { false };
            void buildImageConsumer(ProgressEvent _)
            {
                called[0] = true;
            }

            EventHandlers eventHandlers = MakeEventHandlers(buildImageConsumer);
            eventHandlers.Dispatch(new ProgressEvent(child1, 50));
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
        private void UpdateCompletionMap(Allocation allocation, long units)
        {
            if (allocationCompletionMap.ContainsKey(allocation))
            {
                units += allocationCompletionMap.Get(allocation);
            }
            allocationCompletionMap[allocation] = units;

            if (allocation.GetAllocationUnits() == units)
            {
                allocation
                    .GetParent()
                    .IfPresent(parentAllocation => UpdateCompletionMap(parentAllocation, 1));
            }
        }
    }
}
