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

using Jib.Net.Core.Configuration;
using Jib.Net.Core.Events;
using Jib.Net.Core.Events.Progress;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jib.Net.Core.Unit.Tests.Events.Progress
{
    /** Tests for {@link ProgressEventHandler}. */
    public class ProgressEventHandlerTest
    {
        /** The root node. */
        private Allocation root;

        /** First child of the root node. */
        private Allocation child1;

        /** Child of the first child of the root node. */
        private Allocation child1Child;

        /** Second child of the root node. */
        private Allocation child2;

        private const double DOUBLE_ERROR_MARGIN = 1e-10;

        [SetUp]
        public void SetUp()
        {
            root = Allocation.NewRoot("root", 2);
            child1 = root.NewChild("child1", 1);
            child1Child = child1.NewChild("child1Child", 100);
            child2 = root.NewChild("child2", 200);
        }

        [Test]
        public async Task TestAcceptAsync()
        {
            using (DoubleAccumulator maxProgress = new DoubleAccumulator(0))
            {
                ProgressEventHandler progressEventHandler =
                    new ProgressEventHandler(update => maxProgress.Accumulate(update.GetProgress()));
                EventHandlers eventHandlers =
                    EventHandlers.CreateBuilder().Add<ProgressEvent>(progressEventHandler.Accept).Build();

                // Adds root, child1, and child1Child.
                await MultithreadedExecutor.InvokeAsync(() => eventHandlers.Dispatch(new ProgressEvent(root, 0L)))
                    .ConfigureAwait(false);
                await MultithreadedExecutor.InvokeAsync(() => eventHandlers.Dispatch(new ProgressEvent(child1, 0L)))
                    .ConfigureAwait(false);
                await MultithreadedExecutor
                    .InvokeAsync(() => eventHandlers.Dispatch(new ProgressEvent(child1Child, 0L)))
                    .ConfigureAwait(false);
                Assert.AreEqual(0.0, maxProgress.Get(), DOUBLE_ERROR_MARGIN);

                // Adds 50 to child1Child and 100 to child2.
                List<Action> callables = new List<Action>(150);
                callables.AddRange(
                    Enumerable.Repeat((Action)(() => eventHandlers.Dispatch(new ProgressEvent(child1Child, 1L))), 50));
                callables.AddRange(
                    Enumerable.Repeat((Action)(() => eventHandlers.Dispatch(new ProgressEvent(child2, 1L))), 100));

                await MultithreadedExecutor.InvokeAllAsync(callables).ConfigureAwait(false);

                Assert.AreEqual(
                    (1.0 / 2 / 100 * 50) + (1.0 / 2 / 200 * 100), maxProgress.Get(), DOUBLE_ERROR_MARGIN);

                // 0 progress doesn't do anything.
                await MultithreadedExecutor
                    .InvokeAllAsync(Enumerable.Repeat((Action)(() =>
                        eventHandlers.Dispatch(new ProgressEvent(child1, 0L))), 100))
                    .ConfigureAwait(false);
                Assert.AreEqual(
                    (1.0 / 2 / 100 * 50) + (1.0 / 2 / 200 * 100), maxProgress.Get(), DOUBLE_ERROR_MARGIN);

                // Adds 50 to child1Child and 100 to child2 to finish it up.
                await MultithreadedExecutor.InvokeAllAsync(callables).ConfigureAwait(false);
                Assert.AreEqual(1.0, maxProgress.Get(), DOUBLE_ERROR_MARGIN);
            }
        }
    }
}
