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

using com.google.cloud.tools.jib;
using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.@event.progress;
using Jib.Net.Core;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jib.Net.Core.Unit.Tests.Events.Progress
{
    /** Tests for {@link AllocationCompletionTracker}. */
    public class AllocationCompletionTrackerTest
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
            root = Allocation.newRoot("root", 2);
            child1 = root.newChild("child1", 1);
            child1Child = child1.newChild("child1Child", 100);
            child2 = root.newChild("child2", 200);
        }

        [Test]
        public void testGetUnfinishedAllocations_singleThread()
        {
            AllocationCompletionTracker allocationCompletionTracker = new AllocationCompletionTracker();

            Assert.IsTrue(allocationCompletionTracker.updateProgress(root, 0L));
            Assert.AreEqual(
                new List<Allocation> { root },
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(child1, 0L));
            Assert.AreEqual(
                Arrays.asList(root, child1),
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(child1Child, 0L));
            Assert.AreEqual(
                Arrays.asList(root, child1, child1Child),
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(child1Child, 50L));
            Assert.AreEqual(
                Arrays.asList(root, child1, child1Child),
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(child1Child, 50L));
            Assert.AreEqual(
                new List<Allocation> { root },
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(child2, 100L));
            Assert.AreEqual(
                Arrays.asList(root, child2),
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(child2, 100L));
            Assert.AreEqual(
                new List<Allocation>(), allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsFalse(allocationCompletionTracker.updateProgress(child2, 0L));
            Assert.AreEqual(
                new List<Allocation>(), allocationCompletionTracker.getUnfinishedAllocations());

            try
            {
                allocationCompletionTracker.updateProgress(child1, 1L);
                Assert.Fail();
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Progress exceeds max for 'child1': 1 more beyond 1", ex.getMessage());
            }
        }

        [Test]
        public async Task testGetUnfinishedAllocations_multipleThreadsAsync()
        {
            AllocationCompletionTracker allocationCompletionTracker = new AllocationCompletionTracker();

            // Adds root, child1, and child1Child.
            Assert.AreEqual(
                true,
                await MultithreadedExecutor.invokeAsync(
                    () => allocationCompletionTracker.updateProgress(root, 0L)).ConfigureAwait(false));
            Assert.AreEqual(
                true,
                await MultithreadedExecutor.invokeAsync(
                    () => allocationCompletionTracker.updateProgress(child1, 0L)).ConfigureAwait(false));
            Assert.AreEqual(
                true,
                await MultithreadedExecutor.invokeAsync(
                    () => allocationCompletionTracker.updateProgress(child1Child, 0L)).ConfigureAwait(false));
            Assert.AreEqual(
                Arrays.asList(root, child1, child1Child),
                allocationCompletionTracker.getUnfinishedAllocations());

            // Adds 50 to child1Child and 100 to child2.
            IList<Func<bool>> callables = new List<Func<bool>>(150);
            callables.addAll(
                Enumerable.Repeat((Func<bool>)(() => allocationCompletionTracker.updateProgress(child1Child, 1L)), 50));
            callables.addAll(
                Enumerable.Repeat((Func<bool>)(() => allocationCompletionTracker.updateProgress(child2, 1L)), 100));

            CollectionAssert.AreEqual(
                Enumerable.Repeat(true, 150), await MultithreadedExecutor.invokeAllAsync(callables).ConfigureAwait(false));
            Assert.AreEqual(
                Arrays.asList(
                    root,
                    child1,
                    child1Child,
                    child2),
                allocationCompletionTracker.getUnfinishedAllocations());

            // 0 progress doesn't do anything.
            Assert.AreEqual(
                Enumerable.Repeat(false, 100),
                await MultithreadedExecutor.invokeAllAsync(
                    Enumerable.Repeat((Func<bool>)(() => allocationCompletionTracker.updateProgress(child1, 0L)), 100)).ConfigureAwait(false));
            Assert.AreEqual(
                Arrays.asList(
                    root,
                    child1,
                    child1Child,
                    child2),
                allocationCompletionTracker.getUnfinishedAllocations());

            // Adds 50 to child1Child and 100 to child2 to finish it up.
            CollectionAssert.AreEqual(
                Enumerable.Repeat(true, 150), await MultithreadedExecutor.invokeAllAsync(callables).ConfigureAwait(false));
            CollectionAssert.AreEqual(
                new List<Allocation>(), allocationCompletionTracker.getUnfinishedAllocations());
        }

        [Test]
        public void testGetUnfinishedLeafTasks()
        {
            AllocationCompletionTracker tracker = new AllocationCompletionTracker();
            tracker.updateProgress(root, 0);
            Assert.AreEqual(Arrays.asList("root"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child1, 0);
            Assert.AreEqual(Arrays.asList("child1"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child2, 0);
            Assert.AreEqual(Arrays.asList("child1", "child2"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child1Child, 0);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child1Child, 50);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child2, 100);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child2, 100);
            Assert.AreEqual(Arrays.asList("child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child1Child, 50);
            Assert.AreEqual(new List<string>(), tracker.getUnfinishedLeafTasks());
        }

        [Test]
        public void testGetUnfinishedLeafTasks_differentUpdateOrder()
        {
            AllocationCompletionTracker tracker = new AllocationCompletionTracker();
            tracker.updateProgress(root, 0);
            Assert.AreEqual(Arrays.asList("root"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child2, 0);
            Assert.AreEqual(Arrays.asList("child2"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child1, 0);
            Assert.AreEqual(Arrays.asList("child2", "child1"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child1Child, 0);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child1Child, 50);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child2, 100);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child1Child, 50);
            Assert.AreEqual(Arrays.asList("child2"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(child2, 100);
            Assert.AreEqual(new List<string>(), tracker.getUnfinishedLeafTasks());
        }
    }
}
