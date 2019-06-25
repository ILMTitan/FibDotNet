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
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Moq;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.configuration
{
    /** Tests for {@link BuildConfiguration}. */
    public class BuildConfigurationTest
    {
        [Test]
        public void testBuilder()
        {
            const string expectedBaseImageServerUrl = "someserver";
            const string expectedBaseImageName = "baseimage";
            const string expectedBaseImageTag = "baseimagetag";
            const string expectedTargetServerUrl = "someotherserver";
            const string expectedTargetImageName = "targetimage";
            const string expectedTargetTag = "targettag";
            ISet<string> additionalTargetImageTags = ImmutableHashSet.Create("tag1", "tag2", "tag3");
            ISet<string> expectedTargetImageTags = ImmutableHashSet.Create("targettag", "tag1", "tag2", "tag3");
            IList<CredentialRetriever> credentialRetrievers =
                Collections.singletonList<CredentialRetriever>(() => Option.of(Credential.from("username", "password")));
            Instant expectedCreationTime = Instant.FromUnixTimeSeconds(10000);
            IList<string> expectedEntrypoint = Arrays.asList("some", "entrypoint");
            IList<string> expectedProgramArguments = Arrays.asList("arg1", "arg2");
            IDictionary<string, string> expectedEnvironment = ImmutableDic.of("key", "value");
            ImmutableHashSet<Port> expectedExposedPorts = ImmutableHashSet.Create(Port.tcp(1000), Port.tcp(2000));
            IDictionary<string, string> expectedLabels = ImmutableDic.of("key1", "value1", "key2", "value2");
            const ManifestFormat expectedTargetFormat = ManifestFormat.OCI;
            SystemPath expectedApplicationLayersCacheDirectory = Paths.get("application/layers");
            SystemPath expectedBaseImageLayersCacheDirectory = Paths.get("base/image/layers");
            IList<ILayerConfiguration> expectedLayerConfigurations =
                Collections.singletonList(
                    LayerConfiguration.builder()
                        .addEntry(Paths.get("sourceFile"), AbsoluteUnixPath.get("/path/in/container"))
                        .build());
            const string expectedCreatedBy = "createdBy";

            ImageConfiguration baseImageConfiguration =
                ImageConfiguration.builder(
                        ImageReference.of(
                            expectedBaseImageServerUrl, expectedBaseImageName, expectedBaseImageTag))
                    .build();
            ImageConfiguration targetImageConfiguration =
                ImageConfiguration.builder(
                        ImageReference.of(
                            expectedTargetServerUrl, expectedTargetImageName, expectedTargetTag))
                    .setCredentialRetrievers(credentialRetrievers)
                    .build();
            ContainerConfiguration containerConfiguration =
                ContainerConfiguration.builder()
                    .setCreationTime(expectedCreationTime)
                    .setEntrypoint(expectedEntrypoint)
                    .setProgramArguments(expectedProgramArguments)
                    .setEnvironment(expectedEnvironment)
                    .setExposedPorts(expectedExposedPorts)
                    .setLabels(expectedLabels)
                    .build();
            BuildConfiguration.Builder buildConfigurationBuilder =
                BuildConfiguration.builder()
                    .setBaseImageConfiguration(baseImageConfiguration)
                    .setTargetImageConfiguration(targetImageConfiguration)
                    .setAdditionalTargetImageTags(additionalTargetImageTags)
                    .setContainerConfiguration(containerConfiguration)
                    .setApplicationLayersCacheDirectory(expectedApplicationLayersCacheDirectory)
                    .setBaseImageLayersCacheDirectory(expectedBaseImageLayersCacheDirectory)
                    .setTargetFormat(ImageFormat.OCI)
                    .setAllowInsecureRegistries(true)
                    .setLayerConfigurations(expectedLayerConfigurations)
                    .setToolName(expectedCreatedBy);
            BuildConfiguration buildConfiguration = buildConfigurationBuilder.build();

            Assert.IsNotNull(buildConfiguration.getContainerConfiguration());
            Assert.AreEqual(
                expectedCreationTime, buildConfiguration.getContainerConfiguration().getCreationTime());
            Assert.AreEqual(
                expectedBaseImageServerUrl,
                buildConfiguration.getBaseImageConfiguration().getImageRegistry());
            Assert.AreEqual(
                expectedBaseImageName, buildConfiguration.getBaseImageConfiguration().getImageRepository());
            Assert.AreEqual(
                expectedBaseImageTag, buildConfiguration.getBaseImageConfiguration().getImageTag());
            Assert.AreEqual(
                expectedTargetServerUrl,
                buildConfiguration.getTargetImageConfiguration().getImageRegistry());
            Assert.AreEqual(
                expectedTargetImageName,
                buildConfiguration.getTargetImageConfiguration().getImageRepository());
            Assert.AreEqual(
                expectedTargetTag, buildConfiguration.getTargetImageConfiguration().getImageTag());
            Assert.AreEqual(expectedTargetImageTags, buildConfiguration.getAllTargetImageTags());
            Assert.AreEqual(
                Credential.from("username", "password"),
                buildConfiguration
                    .getTargetImageConfiguration()
                    .getCredentialRetrievers()
                    .get(0)
                    .retrieve()
                    .orElseThrow(() => new AssertionException("")));
            Assert.AreEqual(
                expectedProgramArguments,
                buildConfiguration.getContainerConfiguration().getProgramArguments());
            Assert.AreEqual(
                expectedEnvironment, buildConfiguration.getContainerConfiguration().getEnvironmentMap());
            Assert.AreEqual(
                expectedExposedPorts, buildConfiguration.getContainerConfiguration().getExposedPorts());
            Assert.AreEqual(expectedLabels, buildConfiguration.getContainerConfiguration().getLabels());
            Assert.AreEqual(expectedTargetFormat, buildConfiguration.getTargetFormat());
            Assert.AreEqual(
                expectedApplicationLayersCacheDirectory,
                buildConfigurationBuilder.getApplicationLayersCacheDirectory());
            Assert.AreEqual(
                expectedBaseImageLayersCacheDirectory,
                buildConfigurationBuilder.getBaseImageLayersCacheDirectory());
            Assert.IsTrue(buildConfiguration.getAllowInsecureRegistries());
            Assert.AreEqual(expectedLayerConfigurations, buildConfiguration.getLayerConfigurations());
            Assert.AreEqual(
                expectedEntrypoint, buildConfiguration.getContainerConfiguration().getEntrypoint());
            Assert.AreEqual(expectedCreatedBy, buildConfiguration.getToolName());
        }

        [Test]
        public void testBuilder_default()
        {
            // These are required and don't have defaults.
            const string expectedBaseImageServerUrl = "someserver";
            const string expectedBaseImageName = "baseimage";
            const string expectedBaseImageTag = "baseimagetag";
            const string expectedTargetServerUrl = "someotherserver";
            const string expectedTargetImageName = "targetimage";
            const string expectedTargetTag = "targettag";

            ImageConfiguration baseImageConfiguration =
                ImageConfiguration.builder(
                        ImageReference.of(
                            expectedBaseImageServerUrl, expectedBaseImageName, expectedBaseImageTag))
                    .build();
            ImageConfiguration targetImageConfiguration =
                ImageConfiguration.builder(
                        ImageReference.of(
                            expectedTargetServerUrl, expectedTargetImageName, expectedTargetTag))
                    .build();
            BuildConfiguration.Builder buildConfigurationBuilder =
                BuildConfiguration.builder()
                    .setBaseImageConfiguration(baseImageConfiguration)
                    .setTargetImageConfiguration(targetImageConfiguration)
                    .setBaseImageLayersCacheDirectory(Paths.get("ignored"))
                    .setApplicationLayersCacheDirectory(Paths.get("ignored"));
            BuildConfiguration buildConfiguration = buildConfigurationBuilder.build();

            Assert.AreEqual(ImmutableHashSet.Create("targettag"), buildConfiguration.getAllTargetImageTags());
            Assert.AreEqual(ManifestFormat.V22, buildConfiguration.getTargetFormat());
            Assert.IsNotNull(buildConfigurationBuilder.getApplicationLayersCacheDirectory());
            Assert.AreEqual(
                Paths.get("ignored"), buildConfigurationBuilder.getApplicationLayersCacheDirectory());
            Assert.IsNotNull(buildConfigurationBuilder.getBaseImageLayersCacheDirectory());
            Assert.AreEqual(
                Paths.get("ignored"), buildConfigurationBuilder.getBaseImageLayersCacheDirectory());
            Assert.IsNull(buildConfiguration.getContainerConfiguration());
            Assert.IsFalse(buildConfiguration.getAllowInsecureRegistries());
            Assert.AreEqual(Collections.emptyList<LayerConfiguration>(), buildConfiguration.getLayerConfigurations());
            Assert.AreEqual(null, buildConfiguration.getToolName());
            Assert.AreEqual(null, buildConfiguration.getToolVersion());
        }

        [Test]
        public void testBuilder_missingValues()
        {
            // Target image is missing
            try
            {
                BuildConfiguration.builder()
                    .setBaseImageConfiguration(
                        ImageConfiguration.builder(Mock.Of<IImageReference>()).build())
                    .setBaseImageLayersCacheDirectory(Paths.get("ignored"))
                    .setApplicationLayersCacheDirectory(Paths.get("ignored"))
                    .build();
                Assert.Fail("Build configuration should not be built with missing values");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("target image configuration is required but not set", ex.getMessage());
            }

            // Two required fields missing
            try
            {
                BuildConfiguration.builder()
                    .setBaseImageLayersCacheDirectory(Paths.get("ignored"))
                    .setApplicationLayersCacheDirectory(Paths.get("ignored"))
                    .build();
                Assert.Fail("Build configuration should not be built with missing values");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(
                    "base image configuration and target image configuration are required but not set",
                    ex.getMessage());
            }

            // All required fields missing
            try
            {
                BuildConfiguration.builder().build();
                Assert.Fail("Build configuration should not be built with missing values");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(
                    "base image configuration, target image configuration, base image layers cache directory, and "
                        + "application layers cache directory are required but not set",
                    ex.getMessage());
            }
        }
    }
}
