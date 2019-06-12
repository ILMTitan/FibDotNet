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
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.@event.events;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.@event.progress {













/** Tests for {@link ProgressEventHandler}. */
public class ProgressEventHandlerTest {

  /** {@link Allocation} tree for testing. */
  private class AllocationTree {

    /** The root node. */
    public static readonly Allocation root = Allocation.newRoot("root", 2);

    /** First child of the root node. */
    public static readonly Allocation child1 = root.newChild("child1", 1);
    /** Child of the first child of the root node. */
    public static readonly Allocation child1Child = child1.newChild("child1Child", 100);

    /** Second child of the root node. */
    public static readonly Allocation child2 = root.newChild("child2", 200);

    private AllocationTree() {}
  }

  private static readonly double DOUBLE_ERROR_MARGIN = 1e-10;

  [Test]
  public void testAccept() {
    using (MultithreadedExecutor multithreadedExecutor = new MultithreadedExecutor()) {
      DoubleAccumulator maxProgress = new DoubleAccumulator(Double.MaxValue, 0);

      ProgressEventHandler progressEventHandler =
          new ProgressEventHandler(update => maxProgress.accumulate(update.getProgress()));
      EventHandlers eventHandlers =
          EventHandlers.builder().add<ProgressEvent>( progressEventHandler).build();

      // Adds root, child1, and child1Child.
      multithreadedExecutor.invoke(
          () => {
            eventHandlers.dispatch(new ProgressEvent(AllocationTree.root, 0L));
          });
      multithreadedExecutor.invoke(
          () => {
            eventHandlers.dispatch(new ProgressEvent(AllocationTree.child1, 0L));
          });
      multithreadedExecutor.invoke(
          () => {
            eventHandlers.dispatch(new ProgressEvent(AllocationTree.child1Child, 0L));
          });
      Assert.AreEqual(0.0, maxProgress.get(), DOUBLE_ERROR_MARGIN);

      // Adds 50 to child1Child and 100 to child2.
      IList<Action> callables = new List<Action>(150);
      callables.addAll(
          Collections.nCopies<Action>(
              50,
              () => {
                eventHandlers.dispatch(new ProgressEvent(AllocationTree.child1Child, 1L));
              }));
      callables.addAll(
          Collections.nCopies<Action>(
              100,
              () => {
                eventHandlers.dispatch(new ProgressEvent(AllocationTree.child2, 1L));
              }));

      multithreadedExecutor.invokeAll(callables);

      Assert.AreEqual(
          1.0 / 2 / 100 * 50 + 1.0 / 2 / 200 * 100, maxProgress.get(), DOUBLE_ERROR_MARGIN);

      // 0 progress doesn't do anything.
      multithreadedExecutor.invokeAll(
          Collections.nCopies<Action>(
              100,
              () => {
                eventHandlers.dispatch(new ProgressEvent(AllocationTree.child1, 0L));
              }));
      Assert.AreEqual(
          1.0 / 2 / 100 * 50 + 1.0 / 2 / 200 * 100, maxProgress.get(), DOUBLE_ERROR_MARGIN);

      // Adds 50 to child1Child and 100 to child2 to finish it up.
      multithreadedExecutor.invokeAll(callables);
      Assert.AreEqual(1.0, maxProgress.get(), DOUBLE_ERROR_MARGIN);
    }
  }
}
}
