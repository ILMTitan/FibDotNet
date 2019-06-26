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

using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link Containerizer}. */
    [TestFixture]
    public class ContainerizerTest
    {
        [Test]
        public void TestTo()
        {
            RegistryImage registryImage = RegistryImage.Named(ImageReference.Of(null, "repository", null));
            DockerDaemonImage dockerDaemonImage =
                DockerDaemonImage.Named(ImageReference.Of(null, "repository", null));
            TarImage tarImage =
                TarImage.Named(ImageReference.Of(null, "repository", null)).SaveTo(Paths.Get("ignored"));

            VerifyTo(Containerizer.To(registryImage));
            VerifyTo(Containerizer.To(dockerDaemonImage));
            VerifyTo(Containerizer.To(tarImage));
        }

        private void VerifyTo(Containerizer containerizer)
        {
            Assert.IsTrue(containerizer.GetAdditionalTags().IsEmpty());
            Assert.AreEqual(
                Containerizer.DefaultBaseCacheDirectory,
                containerizer.GetBaseImageLayersCacheDirectory());
            Assert.AreNotEqual(
                Containerizer.DefaultBaseCacheDirectory,
                containerizer.GetApplicationLayersCacheDirectory());
            Assert.IsFalse(containerizer.GetAllowInsecureRegistries());
            Assert.AreEqual("jib-core", containerizer.GetToolName());

            containerizer
                .WithAdditionalTag("tag1")
                .WithAdditionalTag("tag2")
                .SetBaseImageLayersCache(Paths.Get("base/image/layers"))
                .SetApplicationLayersCache(Paths.Get("application/layers"))
                .SetAllowInsecureRegistries(true)
                .SetToolName("tool");

            CollectionAssert.AreEquivalent(ImmutableHashSet.Create("tag1", "tag2"), containerizer.GetAdditionalTags());
            Assert.AreEqual(
                Paths.Get("base/image/layers"), containerizer.GetBaseImageLayersCacheDirectory());
            Assert.AreEqual(
                Paths.Get("application/layers"), containerizer.GetApplicationLayersCacheDirectory());
            Assert.IsTrue(containerizer.GetAllowInsecureRegistries());
            Assert.AreEqual("tool", containerizer.GetToolName());
        }

        [Test]
        public void TestWithAdditionalTag()
        {
            DockerDaemonImage dockerDaemonImage =
                DockerDaemonImage.Named(ImageReference.Of(null, "repository", null));
            Containerizer containerizer = Containerizer.To(dockerDaemonImage);

            containerizer.WithAdditionalTag("tag");
            try
            {
                containerizer.WithAdditionalTag("+invalid+");
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("invalid tag '+invalid+'", ex.GetMessage());
            }
        }

        [Test]
        public void TestGetImageConfiguration_registryImage()
        {
            CredentialRetriever credentialRetriever = Mock.Of<CredentialRetriever>();
            Containerizer containerizer =
                Containerizer.To(
                    RegistryImage.Named("registry/image").AddCredentialRetriever(credentialRetriever));

            ImageConfiguration imageConfiguration = containerizer.GetImageConfiguration();
            Assert.AreEqual("registry/image", JavaExtensions.ToString(imageConfiguration.GetImage()));
            Assert.AreEqual(
                Arrays.AsList(credentialRetriever), imageConfiguration.GetCredentialRetrievers());
        }

        [Test]
        public void TestGetImageConfiguration_dockerDaemonImage()
        {
            Containerizer containerizer = Containerizer.To(DockerDaemonImage.Named("docker/deamon/image"));

            ImageConfiguration imageConfiguration = containerizer.GetImageConfiguration();
            Assert.AreEqual("docker/deamon/image", JavaExtensions.ToString(imageConfiguration.GetImage()));
            Assert.AreEqual(0, imageConfiguration.GetCredentialRetrievers().Size());
        }

        [Test]
        public void TestGetImageConfiguration_tarImage()
        {
            Containerizer containerizer =
                Containerizer.To(TarImage.Named("tar/image").SaveTo(Paths.Get("output/file")));

            ImageConfiguration imageConfiguration = containerizer.GetImageConfiguration();
            Assert.AreEqual("tar/image", JavaExtensions.ToString(imageConfiguration.GetImage()));
            Assert.AreEqual(0, imageConfiguration.GetCredentialRetrievers().Size());
        }
    }
}