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
using Fib.Net.Core.Configuration;
using System;

namespace Fib.Net.Core.Events.Progress
{
    public static class ProgressEventDispatcherFactoryExtensions
    {
        /**
         * Creates the {@link ProgressEventDispatcher} with an associated {@link Allocation}.
         *
         * @param description user-facing description of what the allocation represents
         * @param allocationUnits number of allocation units
         * @return the new {@link ProgressEventDispatcher}
         */
        public static ProgressEventDispatcher Create(this ProgressEventDispatcher.Factory f, string description, long allocationUnits)
        {
            f = f ?? throw new ArgumentNullException(nameof(f));
            return f(description, allocationUnits);
        }
    }

    /**
     * Dispatches {@link ProgressEvent}s associated with a managed {@link Allocation}. Keeps track of
     * the allocation units that are remaining so that it can emit the remaining progress units upon
     * {@link #close}.
     *
     * <p>This class is <em>not</em> thread-safe. Only use a single instance per thread and create child
     * instances with {@link #newChildProducer}.
     */
    public sealed class ProgressEventDispatcher : IDisposable
    {
        /**
         * Creates a new {@link ProgressEventDispatcher} based off an existing {@link
         * ProgressEventDispatcher}. {@link #create} should only be called once.
         */
        public delegate ProgressEventDispatcher Factory(string description, long allocationUnits);

        /**
         * Creates a new {@link ProgressEventDispatcher} with a root {@link Allocation}.
         *
         * @param eventHandlers the {@link EventHandlers}
         * @param description user-facing description of what the allocation represents
         * @param allocationUnits number of allocation units
         * @return a new {@link ProgressEventDispatcher}
         */
        public static ProgressEventDispatcher NewRoot(
            IEventHandlers eventHandlers, string description, long allocationUnits)
        {
            return NewProgressEventDispatcher(
                eventHandlers, Allocation.NewRoot(description, allocationUnits));
        }

        /**
         * Creates a new {@link ProgressEventDispatcher} and dispatches a new {@link ProgressEvent} with
         * progress 0 for {@code allocation}.
         *
         * @param eventHandlers the {@link EventHandlers}
         * @param allocation the {@link Allocation} to manage
         * @return a new {@link ProgressEventDispatcher}
         */
        private static ProgressEventDispatcher NewProgressEventDispatcher(
            IEventHandlers eventHandlers, Allocation allocation)
        {
            ProgressEventDispatcher progressEventDispatcher =
                new ProgressEventDispatcher(eventHandlers, allocation);
            progressEventDispatcher.DispatchProgress(0);
            return progressEventDispatcher;
        }

        private readonly IEventHandlers eventHandlers;
        private readonly Allocation allocation;

        private long remainingAllocationUnits;
        private bool closed = false;

        private ProgressEventDispatcher(IEventHandlers eventHandlers, Allocation allocation)
        {
            this.eventHandlers = eventHandlers;
            this.allocation = allocation;

            remainingAllocationUnits = allocation.GetAllocationUnits();
        }

        /**
         * Creates a new {@link Factory} for a {@link ProgressEventDispatcher} that manages a child {@link
         * Allocation}. Since each child {@link Allocation} accounts for 1 allocation unit of its parent,
         * this method decrements the {@link #remainingAllocationUnits} by {@code 1}.
         *
         * @return a new {@link Factory}
         */
        public Factory NewChildProducer()
        {
            DecrementRemainingAllocationUnits(1);

            bool used = false;
            return (description, allocationUnits) =>
            {
                if (used)
                {
                    throw new InvalidOperationException("Dispatcher factory may not be reused.");
                }
                used = true;
                return NewProgressEventDispatcher(eventHandlers, allocation.NewChild(description, allocationUnits));
            };
        }

        /** Emits the remaining allocation units as progress units in a {@link ProgressEvent}. */

        public void Dispose()
        {
            if (remainingAllocationUnits > 0)
            {
                DispatchProgress(remainingAllocationUnits);
            }
            closed = true;
        }

        /**
         * Dispatches a {@link ProgressEvent} representing {@code progressUnits} of progress on the
         * managed {@link #allocation}.
         *
         * @param progressUnits units of progress
         */
        public void DispatchProgress(long progressUnits)
        {
            long unitsDecremented = DecrementRemainingAllocationUnits(progressUnits);
            eventHandlers.Dispatch(new ProgressEvent(allocation, unitsDecremented));
        }

        /**
         * Decrements remaining allocation units by {@code units} but no more than the remaining
         * allocation units (which may be 0). Returns the actual units decremented, which never exceeds
         * {@code units}.
         *
         * @param units units to decrement
         * @return units actually decremented
         */
        private long DecrementRemainingAllocationUnits(long units)
        {
            if (closed)
            {
                throw new ObjectDisposedException(nameof(ProgressEventDispatcher));
            }

            if (remainingAllocationUnits > units)
            {
                remainingAllocationUnits -= units;
                return units;
            }

            long actualDecrement = remainingAllocationUnits;
            remainingAllocationUnits = 0;
            return actualDecrement;
        }
    }
}
