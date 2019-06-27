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
        public void TestBuilder()
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
                new List<CredentialRetriever> { () => Maybe.Of(Credential.From("username", "password")) };
            Instant expectedCreationTime = Instant.FromUnixTimeSeconds(10000);
            IList<string> expectedEntrypoint = new []{"some", "entrypoint"};
            IList<string> expectedProgramArguments = new []{"arg1", "arg2"};
            IDictionary<string, string> expectedEnvironment = ImmutableDic.Of("key", "value");
            ImmutableHashSet<Port> expectedExposedPorts = ImmutableHashSet.Create(Port.Tcp(1000), Port.Tcp(2000));
            IDictionary<string, string> expectedLabels = ImmutableDic.Of("key1", "value1", "key2", "value2");
            const ManifestFormat expectedTargetFormat = ManifestFormat.OCI;
            SystemPath expectedApplicationLayersCacheDirectory = Paths.Get("application/layers");
            SystemPath expectedBaseImageLayersCacheDirectory = Paths.Get("base/image/layers");
            IList<ILayerConfiguration> expectedLayerConfigurations =
                new List<ILayerConfiguration> {                     LayerConfiguration.CreateBuilder()
                        .AddEntry(Paths.Get("sourceFile"), AbsoluteUnixPath.Get("/path/in/container"))
                        .Build()};
            const string expectedCreatedBy = "createdBy";

            ImageConfiguration baseImageConfiguration =
                ImageConfiguration.CreateBuilder(
                        ImageReference.Of(
                            expectedBaseImageServerUrl, expectedBaseImageName, expectedBaseImageTag))
                    .Build();
            ImageConfiguration targetImageConfiguration =
                ImageConfiguration.CreateBuilder(
                        ImageReference.Of(
                            expectedTargetServerUrl, expectedTargetImageName, expectedTargetTag))
                    .SetCredentialRetrievers(credentialRetrievers)
                    .Build();
            ContainerConfiguration containerConfiguration =
                ContainerConfiguration.CreateBuilder()
                    .SetCreationTime(expectedCreationTime)
                    .SetEntrypoint(expectedEntrypoint)
                    .SetProgramArguments(expectedProgramArguments)
                    .SetEnvironment(expectedEnvironment)
                    .SetExposedPorts(expectedExposedPorts)
                    .SetLabels(expectedLabels)
                    .Build();
            BuildConfiguration.Builder buildConfigurationBuilder =
                BuildConfiguration.CreateBuilder()
                    .SetBaseImageConfiguration(baseImageConfiguration)
                    .SetTargetImageConfiguration(targetImageConfiguration)
                    .SetAdditionalTargetImageTags(additionalTargetImageTags)
                    .SetContainerConfiguration(containerConfiguration)
                    .SetApplicationLayersCacheDirectory(expectedApplicationLayersCacheDirectory)
                    .SetBaseImageLayersCacheDirectory(expectedBaseImageLayersCacheDirectory)
                    .SetTargetFormat(ImageFormat.OCI)
                    .SetAllowInsecureRegistries(true)
                    .SetLayerConfigurations(expectedLayerConfigurations)
                    .SetToolName(expectedCreatedBy);
            BuildConfiguration buildConfiguration = buildConfigurationBuilder.Build();

            Assert.IsNotNull(buildConfiguration.GetContainerConfiguration());
            Assert.AreEqual(
                expectedCreationTime, buildConfiguration.GetContainerConfiguration().GetCreationTime());
            Assert.AreEqual(
                expectedBaseImageServerUrl,
                buildConfiguration.GetBaseImageConfiguration().GetImageRegistry());
            Assert.AreEqual(
                expectedBaseImageName, buildConfiguration.GetBaseImageConfiguration().GetImageRepository());
            Assert.AreEqual(
                expectedBaseImageTag, buildConfiguration.GetBaseImageConfiguration().GetImageTag());
            Assert.AreEqual(
                expectedTargetServerUrl,
                buildConfiguration.GetTargetImageConfiguration().GetImageRegistry());
            Assert.AreEqual(
                expectedTargetImageName,
                buildConfiguration.GetTargetImageConfiguration().GetImageRepository());
            Assert.AreEqual(
                expectedTargetTag, buildConfiguration.GetTargetImageConfiguration().GetImageTag());
            Assert.AreEqual(expectedTargetImageTags, buildConfiguration.GetAllTargetImageTags());
            Assert.AreEqual(
                Credential.From("username", "password"),
                buildConfiguration
                    .GetTargetImageConfiguration()
                    .GetCredentialRetrievers()
[0]
                    .Retrieve()
                    .OrElseThrow(() => new AssertionException("")));
            Assert.AreEqual(
                expectedProgramArguments,
                buildConfiguration.GetContainerConfiguration().GetProgramArguments());
            Assert.AreEqual(
                expectedEnvironment, buildConfiguration.GetContainerConfiguration().GetEnvironmentMap());
            Assert.AreEqual(
                expectedExposedPorts, buildConfiguration.GetContainerConfiguration().GetExposedPorts());
            Assert.AreEqual(expectedLabels, buildConfiguration.GetContainerConfiguration().GetLabels());
            Assert.AreEqual(expectedTargetFormat, buildConfiguration.GetTargetFormat());
            Assert.AreEqual(
                expectedApplicationLayersCacheDirectory,
                buildConfigurationBuilder.GetApplicationLayersCacheDirectory());
            Assert.AreEqual(
                expectedBaseImageLayersCacheDirectory,
                buildConfigurationBuilder.GetBaseImageLayersCacheDirectory());
            Assert.IsTrue(buildConfiguration.GetAllowInsecureRegistries());
            Assert.AreEqual(expectedLayerConfigurations, buildConfiguration.GetLayerConfigurations());
            Assert.AreEqual(
                expectedEntrypoint, buildConfiguration.GetContainerConfiguration().GetEntrypoint());
            Assert.AreEqual(expectedCreatedBy, buildConfiguration.GetToolName());
        }

        [Test]
        public void TestBuilder_default()
        {
            // These are required and don't have defaults.
            const string expectedBaseImageServerUrl = "someserver";
            const string expectedBaseImageName = "baseimage";
            const string expectedBaseImageTag = "baseimagetag";
            const string expectedTargetServerUrl = "someotherserver";
            const string expectedTargetImageName = "targetimage";
            const string expectedTargetTag = "targettag";

            ImageConfiguration baseImageConfiguration =
                ImageConfiguration.CreateBuilder(
                        ImageReference.Of(
                            expectedBaseImageServerUrl, expectedBaseImageName, expectedBaseImageTag))
                    .Build();
            ImageConfiguration targetImageConfiguration =
                ImageConfiguration.CreateBuilder(
                        ImageReference.Of(
                            expectedTargetServerUrl, expectedTargetImageName, expectedTargetTag))
                    .Build();
            BuildConfiguration.Builder buildConfigurationBuilder =
                BuildConfiguration.CreateBuilder()
                    .SetBaseImageConfiguration(baseImageConfiguration)
                    .SetTargetImageConfiguration(targetImageConfiguration)
                    .SetBaseImageLayersCacheDirectory(Paths.Get("ignored"))
                    .SetApplicationLayersCacheDirectory(Paths.Get("ignored"));
            BuildConfiguration buildConfiguration = buildConfigurationBuilder.Build();

            Assert.AreEqual(ImmutableHashSet.Create("targettag"), buildConfiguration.GetAllTargetImageTags());
            Assert.AreEqual(ManifestFormat.V22, buildConfiguration.GetTargetFormat());
            Assert.IsNotNull(buildConfigurationBuilder.GetApplicationLayersCacheDirectory());
            Assert.AreEqual(
                Paths.Get("ignored"), buildConfigurationBuilder.GetApplicationLayersCacheDirectory());
            Assert.IsNotNull(buildConfigurationBuilder.GetBaseImageLayersCacheDirectory());
            Assert.AreEqual(
                Paths.Get("ignored"), buildConfigurationBuilder.GetBaseImageLayersCacheDirectory());
            Assert.IsNull(buildConfiguration.GetContainerConfiguration());
            Assert.IsFalse(buildConfiguration.GetAllowInsecureRegistries());
            Assert.AreEqual(new List<LayerConfiguration>(), buildConfiguration.GetLayerConfigurations());
            Assert.AreEqual(null, buildConfiguration.GetToolName());
            Assert.AreEqual(null, buildConfiguration.GetToolVersion());
        }

        [Test]
        public void TestBuilder_missingValues()
        {
            // Target image is missing
            try
            {
                BuildConfiguration.CreateBuilder()
                    .SetBaseImageConfiguration(
                        ImageConfiguration.CreateBuilder(Mock.Of<IImageReference>()).Build())
                    .SetBaseImageLayersCacheDirectory(Paths.Get("ignored"))
                    .SetApplicationLayersCacheDirectory(Paths.Get("ignored"))
                    .Build();
                Assert.Fail("Build configuration should not be built with missing values");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("target image configuration is required but not set", ex.GetMessage());
            }

            // Two required fields missing
            try
            {
                BuildConfiguration.CreateBuilder()
                    .SetBaseImageLayersCacheDirectory(Paths.Get("ignored"))
                    .SetApplicationLayersCacheDirectory(Paths.Get("ignored"))
                    .Build();
                Assert.Fail("Build configuration should not be built with missing values");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(
                    "base image configuration and target image configuration are required but not set",
                    ex.GetMessage());
            }

            // All required fields missing
            try
            {
                BuildConfiguration.CreateBuilder().Build();
                Assert.Fail("Build configuration should not be built with missing values");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(
                    "base image configuration, target image configuration, base image layers cache directory, and "
                        + "application layers cache directory are required but not set",
                    ex.GetMessage());
            }
        }
    }
}
