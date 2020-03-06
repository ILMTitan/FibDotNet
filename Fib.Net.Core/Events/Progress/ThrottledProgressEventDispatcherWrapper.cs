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

using System;

namespace Fib.Net.Core.Events.Progress
{
    /**
     * Contains a {@link ProgressEventDispatcher} and throttles dispatching progress events with the
     * default delay used by {@link ThrottledConsumer}. This class is mutable and should only be used
     * within a local context.
     *
     * <p>This class is necessary because the total BLOb size (allocation units) is not known until the
     * response headers are received, only after which can the {@link ProgressEventDispatcher} be
     * created.
     */
    internal class ThrottledProgressEventDispatcherWrapper : IDisposable
    {
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;
        private readonly string description;
        private ProgressEventDispatcher progressEventDispatcher;
        private ThrottledAccumulatingConsumer throttledDispatcher;

        public ThrottledProgressEventDispatcherWrapper(
            ProgressEventDispatcher.Factory progressEventDispatcherFactory, string description)
        {
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.description = description;
        }

        public void DispatchProgress(long progressUnits)
        {
            Preconditions.CheckNotNull(throttledDispatcher);
            throttledDispatcher.Accept(progressUnits);
        }

        public void Dispose()
        {
            throttledDispatcher?.Dispose();
            progressEventDispatcher?.Dispose();
        }

        public void SetProgressTarget(long allocationUnits)
        {
            if(progressEventDispatcher != null)
            {
                throw new InvalidOperationException(Resources.ThrottledProgressEventDispatcherWrapperResetExceptionMessage);
            }
            progressEventDispatcher = progressEventDispatcherFactory.Create(description, allocationUnits);
            throttledDispatcher =
                new ThrottledAccumulatingConsumer(progressEventDispatcher.DispatchProgress);
        }
    }
}
