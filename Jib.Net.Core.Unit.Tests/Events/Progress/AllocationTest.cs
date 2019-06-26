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

using Jib.Net.Core.Events.Progress;
using NUnit.Framework;

namespace Jib.Net.Core.Unit.Tests.Events.Progress
{
    /** Tests for {@link Allocation}. */
    public class AllocationTest
    {
        /** Error margin for checking equality of two doubles. */
        private const double DOUBLE_ERROR_MARGIN = 1e-10;

        [Test]
        public void TestSmoke_linear()
        {
            Allocation root = Allocation.NewRoot("root", 1);
            Allocation node1 = root.NewChild("node1", 2);
            Allocation node2 = node1.NewChild("node2", 3);

            Assert.AreEqual("node2", node2.GetDescription());
            Assert.AreEqual(1.0 / 2 / 3, node2.GetFractionOfRoot(), DOUBLE_ERROR_MARGIN);
            Assert.IsTrue(node2.GetParent().IsPresent());
            Assert.AreEqual(node1, node2.GetParent().Get());

            Assert.AreEqual("node1", node1.GetDescription());
            Assert.IsTrue(node1.GetParent().IsPresent());
            Assert.AreEqual(root, node1.GetParent().Get());
            Assert.AreEqual(1.0 / 2, node1.GetFractionOfRoot(), DOUBLE_ERROR_MARGIN);

            Assert.AreEqual("root", root.GetDescription());
            Assert.AreEqual(1, root.GetAllocationUnits());
            Assert.IsFalse(root.GetParent().IsPresent());
            Assert.AreEqual(1.0, root.GetFractionOfRoot(), DOUBLE_ERROR_MARGIN);
        }

        [Test]
        public void TestFractionOfRoot_tree_partial()
        {
            Allocation root = Allocation.NewRoot("ignored", 10);
            Allocation left = root.NewChild("ignored", 2);
            Allocation right = root.NewChild("ignored", 4);
            Allocation leftDown = left.NewChild("ignored", 20);
            Allocation rightLeft = right.NewChild("ignored", 20);
            Allocation rightRight = right.NewChild("ignored", 100);
            Allocation rightRightDown = rightRight.NewChild("ignored", 200);

            Assert.AreEqual(1.0 / 10, root.GetFractionOfRoot(), DOUBLE_ERROR_MARGIN);
            Assert.AreEqual(1.0 / 10 / 2, left.GetFractionOfRoot(), DOUBLE_ERROR_MARGIN);
            Assert.AreEqual(1.0 / 10 / 4, right.GetFractionOfRoot(), DOUBLE_ERROR_MARGIN);
            Assert.AreEqual(1.0 / 10 / 2 / 20, leftDown.GetFractionOfRoot(), DOUBLE_ERROR_MARGIN);
            Assert.AreEqual(1.0 / 10 / 4 / 20, rightLeft.GetFractionOfRoot(), DOUBLE_ERROR_MARGIN);
            Assert.AreEqual(1.0 / 10 / 4 / 100, rightRight.GetFractionOfRoot(), DOUBLE_ERROR_MARGIN);
            Assert.AreEqual(
                1.0 / 10 / 4 / 100 / 200, rightRightDown.GetFractionOfRoot(), DOUBLE_ERROR_MARGIN);
        }

        [Test]
        public void TestFractionOfRoot_tree_complete()
        {
            Allocation root = Allocation.NewRoot("ignored", 2);

            Allocation left = root.NewChild("ignored", 3);
            Allocation leftLeft = left.NewChild("ignored", 1);
            Allocation leftLeftDown = leftLeft.NewChild("ignored", 100);
            Allocation leftMiddle = left.NewChild("ignored", 100);
            Allocation leftRight = left.NewChild("ignored", 100);

            Allocation right = root.NewChild("ignored", 1);
            Allocation rightDown = right.NewChild("ignored", 100);

            // Checks that the leaf allocations add up to a full 1.0.
            double total =
                leftLeftDown.GetFractionOfRoot() * leftLeftDown.GetAllocationUnits()
                    + leftMiddle.GetFractionOfRoot() * leftMiddle.GetAllocationUnits()
                    + leftRight.GetFractionOfRoot() * leftRight.GetAllocationUnits()
                    + rightDown.GetFractionOfRoot() * rightDown.GetAllocationUnits();
            Assert.AreEqual(1.0, total, DOUBLE_ERROR_MARGIN);
        }
    }
}
