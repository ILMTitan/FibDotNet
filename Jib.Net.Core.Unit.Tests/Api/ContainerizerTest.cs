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
        public void testTo()
        {
            RegistryImage registryImage = RegistryImage.named(ImageReference.of(null, "repository", null));
            DockerDaemonImage dockerDaemonImage =
                DockerDaemonImage.named(ImageReference.of(null, "repository", null));
            TarImage tarImage =
                TarImage.named(ImageReference.of(null, "repository", null)).saveTo(Paths.get("ignored"));

            verifyTo(Containerizer.to(registryImage));
            verifyTo(Containerizer.to(dockerDaemonImage));
            verifyTo(Containerizer.to(tarImage));
        }

        private void verifyTo(Containerizer containerizer)
        {
            Assert.IsTrue(containerizer.getAdditionalTags().isEmpty());
            Assert.AreEqual(
                Containerizer.DefaultBaseCacheDirectory,
                containerizer.getBaseImageLayersCacheDirectory());
            Assert.AreNotEqual(
                Containerizer.DefaultBaseCacheDirectory,
                containerizer.getApplicationLayersCacheDirectory());
            Assert.IsFalse(containerizer.getAllowInsecureRegistries());
            Assert.AreEqual("jib-core", containerizer.getToolName());

            containerizer
                .withAdditionalTag("tag1")
                .withAdditionalTag("tag2")
                .setBaseImageLayersCache(Paths.get("base/image/layers"))
                .setApplicationLayersCache(Paths.get("application/layers"))
                .setAllowInsecureRegistries(true)
                .setToolName("tool");

            CollectionAssert.AreEquivalent(ImmutableHashSet.Create("tag1", "tag2"), containerizer.getAdditionalTags());
            Assert.AreEqual(
                Paths.get("base/image/layers"), containerizer.getBaseImageLayersCacheDirectory());
            Assert.AreEqual(
                Paths.get("application/layers"), containerizer.getApplicationLayersCacheDirectory());
            Assert.IsTrue(containerizer.getAllowInsecureRegistries());
            Assert.AreEqual("tool", containerizer.getToolName());
        }

        [Test]
        public void testWithAdditionalTag()
        {
            DockerDaemonImage dockerDaemonImage =
                DockerDaemonImage.named(ImageReference.of(null, "repository", null));
            Containerizer containerizer = Containerizer.to(dockerDaemonImage);

            containerizer.withAdditionalTag("tag");
            try
            {
                containerizer.withAdditionalTag("+invalid+");
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("invalid tag '+invalid+'", ex.getMessage());
            }
        }

        [Test]
        public void testGetImageConfiguration_registryImage()
        {
            CredentialRetriever credentialRetriever = Mock.Of<CredentialRetriever>();
            Containerizer containerizer =
                Containerizer.to(
                    RegistryImage.named("registry/image").addCredentialRetriever(credentialRetriever));

            ImageConfiguration imageConfiguration = containerizer.getImageConfiguration();
            Assert.AreEqual("registry/image", imageConfiguration.getImage().toString());
            Assert.AreEqual(
                Arrays.asList(credentialRetriever), imageConfiguration.getCredentialRetrievers());
        }

        [Test]
        public void testGetImageConfiguration_dockerDaemonImage()
        {
            Containerizer containerizer = Containerizer.to(DockerDaemonImage.named("docker/deamon/image"));

            ImageConfiguration imageConfiguration = containerizer.getImageConfiguration();
            Assert.AreEqual("docker/deamon/image", imageConfiguration.getImage().toString());
            Assert.AreEqual(0, imageConfiguration.getCredentialRetrievers().size());
        }

        [Test]
        public void testGetImageConfiguration_tarImage()
        {
            Containerizer containerizer =
                Containerizer.to(TarImage.named("tar/image").saveTo(Paths.get("output/file")));

            ImageConfiguration imageConfiguration = containerizer.getImageConfiguration();
            Assert.AreEqual("tar/image", imageConfiguration.getImage().toString());
            Assert.AreEqual(0, imageConfiguration.getCredentialRetrievers().size());
        }
    }
}