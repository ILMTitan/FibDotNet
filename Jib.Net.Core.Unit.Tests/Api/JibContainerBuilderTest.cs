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

using Jib.Net.Core.Api;
using Jib.Net.Core.BuildSteps;
using Jib.Net.Core.Configuration;
using Jib.Net.Core.Global;
using Jib.Net.Test.Common;
using Moq;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Jib.Net.Core.Unit.Tests.Api
{
    /** Tests for {@link JibContainerBuilder}. */
    public class JibContainerBuilderTest
    {
        private readonly BuildConfiguration.Builder buildConfigurationBuilder = new BuildConfiguration.Builder();
        private readonly ILayerConfiguration mockLayerConfiguration1 = Mock.Of<ILayerConfiguration>();
        private readonly ILayerConfiguration mockLayerConfiguration2 = Mock.Of<ILayerConfiguration>();
        private readonly CredentialRetriever mockCredentialRetriever = Mock.Of<CredentialRetriever>();
        private readonly Action<IJibEvent> mockJibEventConsumer = Mock.Of<Action<IJibEvent>>();
        private readonly IJibEvent mockJibEvent = Mock.Of<IJibEvent>();

        [Test]
        public void TestToBuildConfiguration_containerConfigurationSet()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.Named("base/image"), buildConfigurationBuilder)
                    .SetEntrypoint(new[] { "entry", "point" })
                    .SetEnvironment(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["name"] = "value" }))
                    .SetExposedPorts(ImmutableHashSet.Create(Port.Tcp(1234), Port.Udp(5678)))
                    .SetLabels(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["key"] = "value" }))
                    .SetProgramArguments(new[] { "program", "arguments" })
                    .SetCreationTime(DateTimeOffset.FromUnixTimeMilliseconds(1000))
                    .SetUser("user")
                    .SetWorkingDirectory(AbsoluteUnixPath.Get("/working/directory"));

            BuildConfiguration buildConfiguration =
                jibContainerBuilder.ToBuildConfiguration(
                    Containerizer.To(RegistryImage.Named("target/image")));
            IContainerConfiguration containerConfiguration = buildConfiguration.GetContainerConfiguration();
            Assert.AreEqual(new[] { "entry", "point" }, containerConfiguration.GetEntrypoint());
            Assert.AreEqual(
                ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["name"] = "value" }), containerConfiguration.GetEnvironmentMap());
            Assert.AreEqual(
                ImmutableHashSet.Create(Port.Tcp(1234), Port.Udp(5678)), containerConfiguration.GetExposedPorts());
            Assert.AreEqual(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["key"] = "value" }), containerConfiguration.GetLabels());
            Assert.AreEqual(
                new[] { "program", "arguments" }, containerConfiguration.GetProgramArguments());
            Assert.AreEqual(Instant.FromUnixTimeMilliseconds(1000), containerConfiguration.GetCreationTime());
            Assert.AreEqual("user", containerConfiguration.GetUser());
            Assert.AreEqual(
                AbsoluteUnixPath.Get("/working/directory"), containerConfiguration.GetWorkingDirectory());
        }

        [Test]
        public void TestToBuildConfiguration_containerConfigurationAdd()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.Named("base/image"), buildConfigurationBuilder)
                    .SetEntrypoint("entry", "point")
                    .SetEnvironment(ImmutableDic.Of("name", "value"))
                    .AddEnvironmentVariable("environment", "variable")
                    .SetExposedPorts(Port.Tcp(1234), Port.Udp(5678))
                    .AddExposedPort(Port.Tcp(1337))
                    .SetLabels(ImmutableDic.Of("key", "value"))
                    .AddLabel("added", "label")
                    .SetProgramArguments("program", "arguments");

            BuildConfiguration buildConfiguration =
                jibContainerBuilder.ToBuildConfiguration(
                    Containerizer.To(RegistryImage.Named("target/image")));
            IContainerConfiguration containerConfiguration = buildConfiguration.GetContainerConfiguration();
            Assert.AreEqual(new[] { "entry", "point" }, containerConfiguration.GetEntrypoint());
            Assert.AreEqual(
                ImmutableDic.Of("name", "value", "environment", "variable"),
                containerConfiguration.GetEnvironmentMap());
            Assert.AreEqual(
                ImmutableHashSet.Create(Port.Tcp(1234), Port.Udp(5678), Port.Tcp(1337)),
                containerConfiguration.GetExposedPorts());
            Assert.AreEqual(
                ImmutableDic.Of("key", "value", "added", "label"), containerConfiguration.GetLabels());
            Assert.AreEqual(
                new[] { "program", "arguments" }, containerConfiguration.GetProgramArguments());
            Assert.AreEqual(Instant.FromUnixTimeSeconds(0), containerConfiguration.GetCreationTime());
        }

        [Test]
        public void TestToBuildConfiguration()
        {
            RegistryImage targetImage =
                RegistryImage.Named(ImageReference.Of("gcr.io", "my-project/my-app", null))
                    .AddCredential("username", "password");
            IContainerizer containerizer =
                Containerizer.To(targetImage)
                    .SetBaseImageLayersCache(Paths.Get("base/image/layers"))
                    .SetApplicationLayersCache(Paths.Get("application/layers"))
                    .AddEventHandler(mockJibEventConsumer);

            RegistryImage baseImage =
                RegistryImage.Named("base/image").AddCredentialRetriever(mockCredentialRetriever);
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(baseImage, buildConfigurationBuilder)
                    .SetLayers(new[] { mockLayerConfiguration1, mockLayerConfiguration2 });
            BuildConfiguration buildConfiguration =
                jibContainerBuilder.ToBuildConfiguration(
                    containerizer);

            Assert.AreEqual(
                buildConfigurationBuilder.Build().GetContainerConfiguration(),
                buildConfiguration.GetContainerConfiguration());

            Assert.AreEqual(
                "base/image", buildConfiguration.GetBaseImageConfiguration().GetImage().ToString());
            Assert.AreEqual(
                new[] { mockCredentialRetriever },
                buildConfiguration.GetBaseImageConfiguration().GetCredentialRetrievers());

            Assert.AreEqual(
                "gcr.io/my-project/my-app",
                buildConfiguration.GetTargetImageConfiguration().GetImage().ToString());
            Assert.AreEqual(
                1, buildConfiguration.GetTargetImageConfiguration().GetCredentialRetrievers().Length);
            Assert.AreEqual(
                Credential.From("username", "password"),
                buildConfiguration
                    .GetTargetImageConfiguration()
                    .GetCredentialRetrievers()
[0]
                    .Retrieve()
                    .OrElseThrow(() => new AssertionException("")));

            Assert.AreEqual(ImmutableHashSet.Create("latest"), buildConfiguration.GetAllTargetImageTags());

            Assert.AreEqual(
                new[] { mockLayerConfiguration1, mockLayerConfiguration2 },
                buildConfiguration.GetLayerConfigurations());

            buildConfiguration.GetEventHandlers().Dispatch(mockJibEvent);
            Mock.Get(mockJibEventConsumer).Verify(m => m(mockJibEvent));

            Assert.AreEqual("jib-core", buildConfiguration.GetToolName());

            Assert.AreEqual(ManifestFormat.V22, buildConfiguration.GetTargetFormat());

            Assert.AreEqual("jib-core", buildConfiguration.GetToolName());

            // Changes jibContainerBuilder.
            buildConfiguration =
                jibContainerBuilder
                    .SetFormat(ImageFormat.OCI)
                    .ToBuildConfiguration(
                        containerizer
                            .WithAdditionalTag("tag1")
                            .WithAdditionalTag("tag2")
                            .SetToolName("toolName"));
            Assert.AreEqual(ManifestFormat.OCI, buildConfiguration.GetTargetFormat());
            Assert.AreEqual(
                ImmutableHashSet.Create("latest", "tag1", "tag2"), buildConfiguration.GetAllTargetImageTags());
            Assert.AreEqual("toolName", buildConfiguration.GetToolName());
        }

        /** Verify that an internally-created ExecutorService is shutdown. */

        [Test]
        public async Task TestContainerize_executorCreatedAsync()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.Named("base/image"), buildConfigurationBuilder)
                    .SetEntrypoint(new[] { "entry", "point" })
                    .SetEnvironment(ImmutableDic.Of("name", "value"))
                    .SetExposedPorts(ImmutableHashSet.Create(Port.Tcp(1234), Port.Udp(5678)))
                    .SetLabels(ImmutableDic.Of("key", "value"))
                    .SetProgramArguments(new[] { "program", "arguments" })
                    .SetCreationTime(DateTimeOffset.FromUnixTimeMilliseconds(1000))
                    .SetUser("user")
                    .SetWorkingDirectory(AbsoluteUnixPath.Get("/working/directory"));

            IContainerizer mockContainerizer = CreateMockContainerizer();

            await jibContainerBuilder.ContainerizeAsync(mockContainerizer).ConfigureAwait(false);
        }

        /** Verify that a provided ExecutorService is not shutdown. */

        [Test]
        public async Task TestContainerize_configuredExecutorAsync()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.Named("base/image"), buildConfigurationBuilder)
                    .SetEntrypoint(new[] { "entry", "point" })
                    .SetEnvironment(ImmutableDic.Of("name", "value"))
                    .SetExposedPorts(ImmutableHashSet.Create(Port.Tcp(1234), Port.Udp(5678)))
                    .SetLabels(ImmutableDic.Of("key", "value"))
                    .SetProgramArguments(new[] { "program", "arguments" })
                    .SetCreationTime(DateTimeOffset.FromUnixTimeMilliseconds(1000))
                    .SetUser("user")
                    .SetWorkingDirectory(AbsoluteUnixPath.Get("/working/directory"));
            IContainerizer mockContainerizer = CreateMockContainerizer();

            await jibContainerBuilder.ContainerizeAsync(mockContainerizer).ConfigureAwait(false);
        }

        private IContainerizer CreateMockContainerizer()
        {
            ImageReference targetImage = ImageReference.Parse("target-image");
            IContainerizer mockContainerizer = Mock.Of<IContainerizer>();
            IStepsRunner stepsRunner = Mock.Of<IStepsRunner>();
            IBuildResult mockBuildResult = Mock.Of<IBuildResult>();

            Mock.Get(mockContainerizer).Setup(m => m.GetImageConfiguration()).Returns(ImageConfiguration.CreateBuilder(targetImage).Build());

            Mock.Get(mockContainerizer).Setup(m => m.CreateStepsRunner(It.IsAny<BuildConfiguration>())).Returns(stepsRunner);

            Mock.Get(stepsRunner).Setup(s => s.RunAsync()).Returns(Task.FromResult(mockBuildResult));

            Mock.Get(mockBuildResult).Setup(m => m.GetImageDigest()).Returns(
                    DescriptorDigest.FromHash(
                        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));

            Mock.Get(mockBuildResult).Setup(m => m.GetImageId()).Returns(
                    DescriptorDigest.FromHash(
                        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));

            Mock.Get(mockContainerizer).Setup(m => m.GetAdditionalTags()).Returns(new HashSet<string>());

            Mock.Get(mockContainerizer).Setup(m => m.GetBaseImageLayersCacheDirectory()).Returns(Paths.Get("/"));

            Mock.Get(mockContainerizer).Setup(m => m.GetApplicationLayersCacheDirectory()).Returns(Paths.Get("/"));

            Mock.Get(mockContainerizer).Setup(m => m.GetAllowInsecureRegistries()).Returns(false);

            Mock.Get(mockContainerizer).Setup(m => m.GetToolName()).Returns("mocktool");

            Mock.Get(mockContainerizer).Setup(m => m.BuildEventHandlers()).Returns(EventHandlers.NONE);

            return mockContainerizer;
        }
    }
}