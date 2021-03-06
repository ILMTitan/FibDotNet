// Copyright 2018 Google Inc.
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

using Fib.Net.Core.Api;
using NUnit.Framework;

namespace Fib.Net.Core.Unit.Tests.Api
{
    /** Tests for {@link FibContainer}. */

    public class FibContainerTest
    {
        private DescriptorDigest digest1;
        private DescriptorDigest digest2;
        private DescriptorDigest digest3;

        [SetUp]
        public void SetUp()
        {
            digest1 =
                DescriptorDigest.FromDigest(
                    "sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789");
            digest2 =
                DescriptorDigest.FromDigest(
                    "sha256:9876543210fedcba9876543210fedcba9876543210fedcba9876543210fedcba");
            digest3 =
                DescriptorDigest.FromDigest(
                    "sha256:fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210");
        }

        [Test]
        public void TestCreation()
        {
            FibContainer container = new FibContainer(digest1, digest2);

            Assert.AreEqual(digest1, container.GetDigest());
            Assert.AreEqual(digest2, container.GetImageId());
        }

        [Test]
        public void TestEquality()
        {
            FibContainer container1 = new FibContainer(digest1, digest2);
            FibContainer container2 = new FibContainer(digest1, digest2);
            FibContainer container3 = new FibContainer(digest2, digest3);

            Assert.AreEqual(container1, container2);
            Assert.AreEqual(container1.GetHashCode(), container2.GetHashCode());
            Assert.AreNotEqual(container1, container3);
        }
    }
}