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
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.@event.events;
using com.google.cloud.tools.jib.@event.progress;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
            root = Allocation.newRoot("root", 2);
            child1 = root.newChild("child1", 1);
            child1Child = child1.newChild("child1Child", 100);
            child2 = root.newChild("child2", 200);
        }

        [Test]
        public async Task testAcceptAsync()
        {
            using (DoubleAccumulator maxProgress = new DoubleAccumulator(0))
            {
                ProgressEventHandler progressEventHandler =
                    new ProgressEventHandler(update => maxProgress.accumulate(update.getProgress()));
                EventHandlers eventHandlers =
                    EventHandlers.builder().add<ProgressEvent>(progressEventHandler).build();

                // Adds root, child1, and child1Child.
                await MultithreadedExecutor.invokeAsync(() => eventHandlers.dispatch(new ProgressEvent(root, 0L)))
                    .ConfigureAwait(false);
                await MultithreadedExecutor.invokeAsync(() => eventHandlers.dispatch(new ProgressEvent(child1, 0L)))
                    .ConfigureAwait(false);
                await MultithreadedExecutor
                    .invokeAsync(() => eventHandlers.dispatch(new ProgressEvent(child1Child, 0L)))
                    .ConfigureAwait(false);
                Assert.AreEqual(0.0, maxProgress.get(), DOUBLE_ERROR_MARGIN);

                // Adds 50 to child1Child and 100 to child2.
                IList<Action> callables = new List<Action>(150);
                callables.addAll(
                    Collections.nCopies<Action>(
                        50,
                        () => eventHandlers.dispatch(new ProgressEvent(child1Child, 1L))));
                callables.addAll(
                    Collections.nCopies<Action>(
                        100,
                        () => eventHandlers.dispatch(new ProgressEvent(child2, 1L))));

                await MultithreadedExecutor.invokeAllAsync(callables).ConfigureAwait(false);

                Assert.AreEqual(
                    1.0 / 2 / 100 * 50 + 1.0 / 2 / 200 * 100, maxProgress.get(), DOUBLE_ERROR_MARGIN);

                // 0 progress doesn't do anything.
                await MultithreadedExecutor
                    .invokeAllAsync(Collections.nCopies<Action>(100, () =>
                        eventHandlers.dispatch(new ProgressEvent(child1, 0L))))
                    .ConfigureAwait(false);
                Assert.AreEqual(
                    1.0 / 2 / 100 * 50 + 1.0 / 2 / 200 * 100, maxProgress.get(), DOUBLE_ERROR_MARGIN);

                // Adds 50 to child1Child and 100 to child2 to finish it up.
                await MultithreadedExecutor.invokeAllAsync(callables).ConfigureAwait(false);
                Assert.AreEqual(1.0, maxProgress.get(), DOUBLE_ERROR_MARGIN);
            }
        }
    }
}
