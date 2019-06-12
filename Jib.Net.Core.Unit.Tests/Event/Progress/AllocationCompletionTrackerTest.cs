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
using Jib.Net.Core;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.@event.progress
{

    /** Tests for {@link AllocationCompletionTracker}. */
    public class AllocationCompletionTrackerTest
    {
        /** {@link Allocation} tree for testing. */
        private class AllocationTree
        {
            /** The root node. */
            public static readonly Allocation root = Allocation.newRoot("root", 2);

            /** First child of the root node. */
            public static readonly Allocation child1 = root.newChild("child1", 1);

            /** Child of the first child of the root node. */
            public static readonly Allocation child1Child = child1.newChild("child1Child", 100);

            /** Second child of the root node. */
            public static readonly Allocation child2 = root.newChild("child2", 200);

            private AllocationTree() { }
        }

        [Test]
        public void testGetUnfinishedAllocations_singleThread()
        {
            AllocationCompletionTracker allocationCompletionTracker = new AllocationCompletionTracker();

            Assert.IsTrue(allocationCompletionTracker.updateProgress(AllocationTree.root, 0L));
            Assert.AreEqual(
                Collections.singletonList(AllocationTree.root),
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(AllocationTree.child1, 0L));
            Assert.AreEqual(
                Arrays.asList(AllocationTree.root, AllocationTree.child1),
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(AllocationTree.child1Child, 0L));
            Assert.AreEqual(
                Arrays.asList(AllocationTree.root, AllocationTree.child1, AllocationTree.child1Child),
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(AllocationTree.child1Child, 50L));
            Assert.AreEqual(
                Arrays.asList(AllocationTree.root, AllocationTree.child1, AllocationTree.child1Child),
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(AllocationTree.child1Child, 50L));
            Assert.AreEqual(
                Collections.singletonList(AllocationTree.root),
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(AllocationTree.child2, 100L));
            Assert.AreEqual(
                Arrays.asList(AllocationTree.root, AllocationTree.child2),
                allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.updateProgress(AllocationTree.child2, 100L));
            Assert.AreEqual(
                Collections.emptyList<Allocation>(), allocationCompletionTracker.getUnfinishedAllocations());

            Assert.IsFalse(allocationCompletionTracker.updateProgress(AllocationTree.child2, 0L));
            Assert.AreEqual(
                Collections.emptyList<Allocation>(), allocationCompletionTracker.getUnfinishedAllocations());

            try
            {
                allocationCompletionTracker.updateProgress(AllocationTree.child1, 1L);
                Assert.Fail();
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Progress exceeds max for 'child1': 1 more beyond 1", ex.getMessage());
            }
        }

        [Test]
        public void testGetUnfinishedAllocations_multipleThreads()
        {
            using (MultithreadedExecutor multithreadedExecutor = new MultithreadedExecutor())
            {
                AllocationCompletionTracker allocationCompletionTracker = new AllocationCompletionTracker();

                // Adds root, child1, and child1Child.
                Assert.AreEqual(
                    true,
                    multithreadedExecutor.invoke(
                        () => allocationCompletionTracker.updateProgress(AllocationTree.root, 0L)));
                Assert.AreEqual(
                    true,
                    multithreadedExecutor.invoke(
                        () => allocationCompletionTracker.updateProgress(AllocationTree.child1, 0L)));
                Assert.AreEqual(
                    true,
                    multithreadedExecutor.invoke(
                        () => allocationCompletionTracker.updateProgress(AllocationTree.child1Child, 0L)));
                Assert.AreEqual(
                    Arrays.asList(AllocationTree.root, AllocationTree.child1, AllocationTree.child1Child),
                    allocationCompletionTracker.getUnfinishedAllocations());

                // Adds 50 to child1Child and 100 to child2.
                IList<Callable<bool>> callables = new List<Callable<bool>>(150);
                callables.addAll(
                    Collections.nCopies<Callable<bool>>(
                        50,
                        () => allocationCompletionTracker.updateProgress(AllocationTree.child1Child, 1L)));
                callables.addAll(
                    Collections.nCopies<Callable<bool>>(
                        100, () => allocationCompletionTracker.updateProgress(AllocationTree.child2, 1L)));

                Assert.AreEqual(
                    Collections.nCopies(150, true), multithreadedExecutor.invokeAll(callables));
                Assert.AreEqual(
                    Arrays.asList(
                        AllocationTree.root,
                        AllocationTree.child1,
                        AllocationTree.child1Child,
                        AllocationTree.child2),
                    allocationCompletionTracker.getUnfinishedAllocations());

                // 0 progress doesn't do anything.
                Assert.AreEqual(
                    Collections.nCopies(100, false),
                    multithreadedExecutor.invokeAll(
                        Collections.nCopies<Callable<bool>>(
                            100,
                            () => allocationCompletionTracker.updateProgress(AllocationTree.child1, 0L))));
                Assert.AreEqual(
                    Arrays.asList(
                        AllocationTree.root,
                        AllocationTree.child1,
                        AllocationTree.child1Child,
                        AllocationTree.child2),
                    allocationCompletionTracker.getUnfinishedAllocations());

                // Adds 50 to child1Child and 100 to child2 to finish it up.
                Assert.AreEqual(
                    Collections.nCopies(150, true), multithreadedExecutor.invokeAll(callables));
                Assert.AreEqual(
                    Collections.emptyList<Allocation>(), allocationCompletionTracker.getUnfinishedAllocations());
            }
        }

        [Test]
        public void testGetUnfinishedLeafTasks()
        {
            AllocationCompletionTracker tracker = new AllocationCompletionTracker();
            tracker.updateProgress(AllocationTree.root, 0);
            Assert.AreEqual(Arrays.asList("root"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child1, 0);
            Assert.AreEqual(Arrays.asList("child1"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child2, 0);
            Assert.AreEqual(Arrays.asList("child1", "child2"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child1Child, 0);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child1Child, 50);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child2, 100);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child2, 100);
            Assert.AreEqual(Arrays.asList("child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child1Child, 50);
            Assert.AreEqual(Collections.emptyList<string>(), tracker.getUnfinishedLeafTasks());
        }

        [Test]
        public void testGetUnfinishedLeafTasks_differentUpdateOrder()
        {
            AllocationCompletionTracker tracker = new AllocationCompletionTracker();
            tracker.updateProgress(AllocationTree.root, 0);
            Assert.AreEqual(Arrays.asList("root"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child2, 0);
            Assert.AreEqual(Arrays.asList("child2"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child1, 0);
            Assert.AreEqual(Arrays.asList("child2", "child1"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child1Child, 0);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child1Child, 50);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child2, 100);
            Assert.AreEqual(Arrays.asList("child2", "child1Child"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child1Child, 50);
            Assert.AreEqual(Arrays.asList("child2"), tracker.getUnfinishedLeafTasks());

            tracker.updateProgress(AllocationTree.child2, 100);
            Assert.AreEqual(Collections.emptyList<string>(), tracker.getUnfinishedLeafTasks());
        }
    }
}
