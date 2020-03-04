/*
 * Copyright 2018 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Jib.Net.Core.Api;
using Jib.Net.Core.BuildSteps;
using Jib.Net.Core.Configuration;
using Jib.Net.Core.Images;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Jib.Net.Core.Unit.Tests.BuildSteps
{
    /** Tests for {@link BuildResult}. */
    public class BuildResultTest
    {
        private DescriptorDigest digest1;
        private DescriptorDigest digest2;
        private DescriptorDigest id;

        [SetUp]
        public void SetUp()
        {
            digest1 =
                DescriptorDigest.FromDigest(
                    "sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789");
            digest2 =
                DescriptorDigest.FromDigest(
                    "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
            id =
                DescriptorDigest.FromDigest(
                    "sha256:9876543210fedcba9876543210fedcba9876543210fedcba9876543210fedcba");
        }

        [Test]
        public void TestCreated()
        {
            BuildResult container = new BuildResult(digest1, id);
            Assert.AreEqual(digest1, container.GetImageDigest());
            Assert.AreEqual(id, container.GetImageId());
        }

        [Test]
        public void TestEquality()
        {
            BuildResult container1 = new BuildResult(digest1, id);
            BuildResult container2 = new BuildResult(digest1, id);
            BuildResult container3 = new BuildResult(digest2, id);

            Assert.AreEqual(container1, container2);
            Assert.AreEqual(container1.GetHashCode(), container2.GetHashCode());
            Assert.AreNotEqual(container1, container3);
        }

        [Test]
        public async Task TestFromImageAsync()
        {
            Image image1 = Image.CreateBuilder(ManifestFormat.V22).SetUser("user").Build();
            Image image2 = Image.CreateBuilder(ManifestFormat.V22).SetUser("user").Build();
            Image image3 = Image.CreateBuilder(ManifestFormat.V22).SetUser("anotherUser").Build();
            Assert.AreEqual(
                await BuildResult.FromImageAsync(image1, ManifestFormat.V22).ConfigureAwait(false),
                await BuildResult.FromImageAsync(image2, ManifestFormat.V22).ConfigureAwait(false));
            Assert.AreNotEqual(
                await BuildResult.FromImageAsync(image1, ManifestFormat.V22).ConfigureAwait(false),
                await BuildResult.FromImageAsync(image3, ManifestFormat.V22).ConfigureAwait(false));
        }
    }
}
