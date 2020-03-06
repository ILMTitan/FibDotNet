// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using Fib.Net.Core.Events.Progress;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fib.Net.Core.Unit.Tests.Events.Progress
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
            root = Allocation.NewRoot("root", 2);
            child1 = root.NewChild("child1", 1);
            child1Child = child1.NewChild("child1Child", 100);
            child2 = root.NewChild("child2", 200);
        }

        [Test]
        public void TestGetUnfinishedAllocations_singleThread()
        {
            AllocationCompletionTracker allocationCompletionTracker = new AllocationCompletionTracker();

            Assert.IsTrue(allocationCompletionTracker.UpdateProgress(root, 0L));
            Assert.AreEqual(
                new List<Allocation> { root },
                allocationCompletionTracker.GetUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.UpdateProgress(child1, 0L));
            Assert.AreEqual(
                new []{root, child1},
                allocationCompletionTracker.GetUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.UpdateProgress(child1Child, 0L));
            Assert.AreEqual(
                new []{root, child1, child1Child},
                allocationCompletionTracker.GetUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.UpdateProgress(child1Child, 50L));
            Assert.AreEqual(
                new []{root, child1, child1Child},
                allocationCompletionTracker.GetUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.UpdateProgress(child1Child, 50L));
            Assert.AreEqual(
                new List<Allocation> { root },
                allocationCompletionTracker.GetUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.UpdateProgress(child2, 100L));
            Assert.AreEqual(
                new []{root, child2},
                allocationCompletionTracker.GetUnfinishedAllocations());

            Assert.IsTrue(allocationCompletionTracker.UpdateProgress(child2, 100L));
            Assert.AreEqual(
                new List<Allocation>(), allocationCompletionTracker.GetUnfinishedAllocations());

            Assert.IsFalse(allocationCompletionTracker.UpdateProgress(child2, 0L));
            Assert.AreEqual(
                new List<Allocation>(), allocationCompletionTracker.GetUnfinishedAllocations());

            try
            {
                allocationCompletionTracker.UpdateProgress(child1, 1L);
                Assert.Fail();
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Progress exceeds max for 'child1': 1 more beyond 1", ex.Message);
            }
        }

        [Test]
        public async Task TestGetUnfinishedAllocations_multipleThreadsAsync()
        {
            AllocationCompletionTracker allocationCompletionTracker = new AllocationCompletionTracker();

            // Adds root, child1, and child1Child.
            Assert.AreEqual(
                true,
                await MultithreadedExecutor.InvokeAsync(
                    () => allocationCompletionTracker.UpdateProgress(root, 0L)).ConfigureAwait(false));
            Assert.AreEqual(
                true,
                await MultithreadedExecutor.InvokeAsync(
                    () => allocationCompletionTracker.UpdateProgress(child1, 0L)).ConfigureAwait(false));
            Assert.AreEqual(
                true,
                await MultithreadedExecutor.InvokeAsync(
                    () => allocationCompletionTracker.UpdateProgress(child1Child, 0L)).ConfigureAwait(false));
            Assert.AreEqual(
                new []{root, child1, child1Child},
                allocationCompletionTracker.GetUnfinishedAllocations());

            // Adds 50 to child1Child and 100 to child2.
            List<Func<bool>> callables = new List<Func<bool>>(150);
            callables.AddRange(
                Enumerable.Repeat((Func<bool>)(() => allocationCompletionTracker.UpdateProgress(child1Child, 1L)), 50));
            callables.AddRange(
                Enumerable.Repeat((Func<bool>)(() => allocationCompletionTracker.UpdateProgress(child2, 1L)), 100));

            CollectionAssert.AreEqual(
                Enumerable.Repeat(true, 150), await MultithreadedExecutor.InvokeAllAsync(callables).ConfigureAwait(false));
            Assert.AreEqual(
                new[]
                {
                    root,
                    child1,
                    child1Child,
                    child2
                },
                allocationCompletionTracker.GetUnfinishedAllocations());

            // 0 progress doesn't do anything.
            Assert.AreEqual(
                Enumerable.Repeat(false, 100),
                await MultithreadedExecutor.InvokeAllAsync(
                    Enumerable.Repeat((Func<bool>)(() => allocationCompletionTracker.UpdateProgress(child1, 0L)), 100)).ConfigureAwait(false));
            Assert.AreEqual(
                new[]
                {
                    root,
                    child1,
                    child1Child,
                    child2
                },
                allocationCompletionTracker.GetUnfinishedAllocations());

            // Adds 50 to child1Child and 100 to child2 to finish it up.
            CollectionAssert.AreEqual(
                Enumerable.Repeat(true, 150), await MultithreadedExecutor.InvokeAllAsync(callables).ConfigureAwait(false));
            CollectionAssert.AreEqual(
                new List<Allocation>(), allocationCompletionTracker.GetUnfinishedAllocations());
        }

        [Test]
        public void TestGetUnfinishedLeafTasks()
        {
            AllocationCompletionTracker tracker = new AllocationCompletionTracker();
            tracker.UpdateProgress(root, 0);
            Assert.AreEqual(new []{"root"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child1, 0);
            Assert.AreEqual(new []{"child1"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child2, 0);
            Assert.AreEqual(new []{"child1", "child2"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child1Child, 0);
            Assert.AreEqual(new []{"child2", "child1Child"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child1Child, 50);
            Assert.AreEqual(new []{"child2", "child1Child"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child2, 100);
            Assert.AreEqual(new []{"child2", "child1Child"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child2, 100);
            Assert.AreEqual(new []{"child1Child"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child1Child, 50);
            Assert.AreEqual(new List<string>(), tracker.GetUnfinishedLeafTasks());
        }

        [Test]
        public void TestGetUnfinishedLeafTasks_differentUpdateOrder()
        {
            AllocationCompletionTracker tracker = new AllocationCompletionTracker();
            tracker.UpdateProgress(root, 0);
            Assert.AreEqual(new []{"root"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child2, 0);
            Assert.AreEqual(new []{"child2"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child1, 0);
            Assert.AreEqual(new []{"child2", "child1"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child1Child, 0);
            Assert.AreEqual(new []{"child2", "child1Child"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child1Child, 50);
            Assert.AreEqual(new []{"child2", "child1Child"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child2, 100);
            Assert.AreEqual(new []{"child2", "child1Child"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child1Child, 50);
            Assert.AreEqual(new []{"child2"}, tracker.GetUnfinishedLeafTasks());

            tracker.UpdateProgress(child2, 100);
            Assert.AreEqual(new List<string>(), tracker.GetUnfinishedLeafTasks());
        }
    }
}
