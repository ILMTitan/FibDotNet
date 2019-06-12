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

using com.google.cloud.tools.jib.@event.events;
using Jib.Net.Core.Api;
using System;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.@event.progress
{
    /**
     * Handles {@link ProgressEvent}s by accumulating an overall progress and keeping track of which
     * {@link Allocation}s are complete.
     *
     * <p>This implementation is thread-safe.
     */
    public class ProgressEventHandler
    {
        public static implicit operator Action<ProgressEvent>(ProgressEventHandler h)
        {
            return h.accept;
        }

        /**
         * Contains the accumulated progress and which "leaf" tasks are not yet complete. Leaf tasks are
         * those that do not have sub-tasks.
         */
        public class Update
        {
            private readonly double progress;
            private readonly ImmutableArray<string> unfinishedLeafTasks;

            public Update(double progress, ImmutableArray<string> unfinishedLeafTasks)
            {
                this.progress = progress;
                this.unfinishedLeafTasks = unfinishedLeafTasks;
            }

            /**
             * Gets the overall progress, with {@code 1.0} meaning fully complete.
             *
             * @return the overall progress
             */
            public double getProgress()
            {
                return progress;
            }

            /**
             * Gets a list of the unfinished "leaf" tasks in the order in which those tasks were
             * encountered.
             *
             * @return a list of unfinished "leaf" tasks
             */
            public ImmutableArray<string> getUnfinishedLeafTasks()
            {
                return unfinishedLeafTasks;
            }
        }

        /** Keeps track of the progress for each {@link Allocation} encountered. */
        private readonly AllocationCompletionTracker completionTracker = new AllocationCompletionTracker();

        /** Accumulates an overall progress, with {@code 1.0} indicating full completion. */
        private readonly DoubleAdder progress = new DoubleAdder();

        /**
         * A callback to notify that {@link #progress} or {@link #completionTracker} could have changed.
         * Note that every change will be reported (though multiple could be reported together), and there
         * could be false positives.
         */
        private readonly Consumer<Update> updateNotifier;

        public ProgressEventHandler(Consumer<Update> updateNotifier)
        {
            this.updateNotifier = updateNotifier;
        }

        public void accept(ProgressEvent progressEvent)
        {
            Allocation allocation = progressEvent.getAllocation();
            long progressUnits = progressEvent.getUnits();
            double allocationFraction = allocation.getFractionOfRoot();

            if (progressUnits != 0)
            {
                progress.add(progressUnits * allocationFraction);
            }

            if (completionTracker.updateProgress(allocation, progressUnits))
            {
                // Note: Could produce false positives.
                updateNotifier.accept(new Update(progress.sum(), completionTracker.getUnfinishedLeafTasks()));
            }
        }
    }
}