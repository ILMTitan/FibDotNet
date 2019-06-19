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
using Iesi.Collections.Generic;
using Jib.Net.Core.Global;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace com.google.cloud.tools.jib.@event.progress
{
    /**
     * Keeps track of the progress for {@link Allocation}s as well as their order in which they appear.
     *
     * <p>This implementation is thread-safe.
     */
    internal class AllocationCompletionTracker
    {
        /**
         * Holds the progress units remaining along with a creation order (index starting from 0). This is
         * used as the value of the {@link #completionMap}.
         */
        private class IndexedRemainingUnits : IComparable<IndexedRemainingUnits>
        {
            /** Monotonically-increasing source for {@link #index}. */
            private static readonly AtomicInteger currentIndex = new AtomicInteger();

            /** The creation order that monotonically increases. */
            private readonly int index = currentIndex.getAndIncrement();

            /**
             * Remaining progress units until completion. This can be shared across multiple threads and
             * should be updated atomically.
             */
            public readonly AtomicLong remainingUnits;

            public readonly Allocation allocation;

            public IndexedRemainingUnits(Allocation allocation)
            {
                this.allocation = allocation;
                remainingUnits = new AtomicLong(allocation.getAllocationUnits());
            }

            public bool isUnfinished()
            {
                return remainingUnits.get() != 0;
            }

            public int CompareTo(IndexedRemainingUnits otherIndexedRemainingUnits)
            {
                return index - otherIndexedRemainingUnits.index;
            }

            public override bool Equals(object obj)
            {
                return obj is IndexedRemainingUnits units &&
                       index == units.index;
            }

            public override int GetHashCode()
            {
                return -1982729373 + index.GetHashCode();
            }

            public override string ToString()
            {
                return $"{allocation}: {remainingUnits}";
            }
        }

        /**
         * Maps from {@link Allocation} to 1) the number of progress units remaining in that {@link
         * Allocation}; as well as 2) the insertion order of the key.
         */
        private readonly ConcurrentDictionary<Allocation, IndexedRemainingUnits> completionMap =
            new ConcurrentDictionary<Allocation, IndexedRemainingUnits>();

        /**
         * Updates the progress for {@link Allocation} atomically relative to the {@code allocation}.
         *
         * <p>For any {@link Allocation}, this method <em>must</em> have been called on all of its parents
         * beforehand.
         *
         * @param allocation the {@link Allocation} to update progress for
         * @param units the units of progress
         * @return {@code true} if the map was updated
         */
        public bool updateProgress(Allocation allocation, long units)
        {
            IndexedRemainingUnits newValue = new IndexedRemainingUnits(allocation);
            var finalValue = completionMap.GetOrAdd(allocation, newValue);
                updateIndexedRemainingUnits(finalValue, units);
            return newValue == finalValue || units != 0;
        }

        /**
         * Gets a list of the unfinished {@link Allocation}s in the order in which those {@link
         * Allocation}s were encountered. This can be used to display, for example, currently executing
         * tasks. The order helps to keep the displayed tasks in a deterministic order (new subtasks
         * appear below older ones) and not jumbled together in some random order.
         *
         * @return a list of unfinished {@link Allocation}s
         */

        public IList<Allocation> getUnfinishedAllocations()
        {
            return completionMap
                .values()
                .stream()
                .filter(u => u.isUnfinished())
                .sorted()
                .map(remainingUnits => remainingUnits.allocation)
                .ToList();
        }

        /**
         * Helper method for {@link #updateProgress(Allocation, long)}. Subtract {@code units} from {@code
         * indexedRemainingUnits}. Updates {@link IndexedRemainingUnits} for parent {@link Allocation}s if
         * remaining units becomes 0. This method is <em>not</em> thread-safe for the {@code
         * indexedRemainingUnits} and should be called atomically relative to the {@code
         * indexedRemainingUnits}.
         *
         * @param indexedRemainingUnits the {@link IndexedRemainingUnits} to update progress for
         * @param units the units of progress
         */
        private void updateIndexedRemainingUnits(
            IndexedRemainingUnits indexedRemainingUnits, long units)
        {
            if(units == 0)
            {
                return;
            }

            Allocation allocation = indexedRemainingUnits.allocation;

            long newUnits = indexedRemainingUnits.remainingUnits.addAndGet(-units);
            if (newUnits < 0L)
            {
                throw new InvalidOperationException(
                    "Progress exceeds max for '"
                        + allocation.getDescription()
                        + "': "
                        + -newUnits
                        + " more beyond "
                        + allocation.getAllocationUnits());
            }

            // Updates the parent allocations if this allocation completed.
            if (newUnits == 0L)
            {
                allocation
                    .getParent()
                    .ifPresent(
                        parentAllocation =>
                            updateIndexedRemainingUnits(
                                Preconditions.checkNotNull(completionMap.get(parentAllocation)), 1L));
            }
        }

        public ImmutableArray<string> getUnfinishedLeafTasks()
        {
            IList<Allocation> allUnfinished = getUnfinishedAllocations();
            ISet<Allocation> unfinishedLeaves = new LinkedHashSet<Allocation>(allUnfinished); // preserves order

            foreach (Allocation allocation in allUnfinished)

            {
                Optional<Allocation> parent = allocation.getParent();

                while (parent.isPresent())
                {
                    unfinishedLeaves.remove(parent.get());
                    parent = parent.get().getParent();
                }
            }

            return ImmutableArray.CreateRange(
                unfinishedLeaves.stream().map(a => a.getDescription()).ToList());
        }
    }
}
