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

using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Moq;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.api
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
        public void testToBuildConfiguration_containerConfigurationSet()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.named("base/image"), buildConfigurationBuilder)
                    .SetEntrypoint(Arrays.asList("entry", "point"))
                    .SetEnvironment(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["name"] = "value" }))
                    .SetExposedPorts(ImmutableHashSet.Create(Port.tcp(1234), Port.udp(5678)))
                    .SetLabels(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["key"] = "value" }))
                    .SetProgramArguments(Arrays.asList("program", "arguments"))
                    .SetCreationTime(Instant.FromUnixTimeMilliseconds(1000))
                    .SetUser("user")
                    .SetWorkingDirectory(AbsoluteUnixPath.get("/working/directory"));

            BuildConfiguration buildConfiguration =
                jibContainerBuilder.toBuildConfiguration(
                    Containerizer.To(RegistryImage.named("target/image")));
            IContainerConfiguration containerConfiguration = buildConfiguration.getContainerConfiguration();
            Assert.AreEqual(Arrays.asList("entry", "point"), containerConfiguration.getEntrypoint());
            Assert.AreEqual(
                ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["name"] = "value" }), containerConfiguration.getEnvironmentMap());
            Assert.AreEqual(
                ImmutableHashSet.Create(Port.tcp(1234), Port.udp(5678)), containerConfiguration.getExposedPorts());
            Assert.AreEqual(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["key"] = "value" }), containerConfiguration.getLabels());
            Assert.AreEqual(
                Arrays.asList("program", "arguments"), containerConfiguration.getProgramArguments());
            Assert.AreEqual(Instant.FromUnixTimeMilliseconds(1000), containerConfiguration.getCreationTime());
            Assert.AreEqual("user", containerConfiguration.getUser());
            Assert.AreEqual(
                AbsoluteUnixPath.get("/working/directory"), containerConfiguration.getWorkingDirectory());
        }

        [Test]
        public void testToBuildConfiguration_containerConfigurationAdd()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.named("base/image"), buildConfigurationBuilder)
                    .SetEntrypoint("entry", "point")
                    .SetEnvironment(ImmutableDic.of("name", "value"))
                    .AddEnvironmentVariable("environment", "variable")
                    .SetExposedPorts(Port.tcp(1234), Port.udp(5678))
                    .AddExposedPort(Port.tcp(1337))
                    .SetLabels(ImmutableDic.of("key", "value"))
                    .AddLabel("added", "label")
                    .SetProgramArguments("program", "arguments");

            BuildConfiguration buildConfiguration =
                jibContainerBuilder.toBuildConfiguration(
                    Containerizer.To(RegistryImage.named("target/image")));
            IContainerConfiguration containerConfiguration = buildConfiguration.getContainerConfiguration();
            Assert.AreEqual(Arrays.asList("entry", "point"), containerConfiguration.getEntrypoint());
            Assert.AreEqual(
                ImmutableDic.of("name", "value", "environment", "variable"),
                containerConfiguration.getEnvironmentMap());
            Assert.AreEqual(
                ImmutableHashSet.Create(Port.tcp(1234), Port.udp(5678), Port.tcp(1337)),
                containerConfiguration.getExposedPorts());
            Assert.AreEqual(
                ImmutableDic.of("key", "value", "added", "label"), containerConfiguration.getLabels());
            Assert.AreEqual(
                Arrays.asList("program", "arguments"), containerConfiguration.getProgramArguments());
            Assert.AreEqual(Instant.FromUnixTimeSeconds(0), containerConfiguration.getCreationTime());
        }

        [Test]
        public void testToBuildConfiguration()
        {
            RegistryImage targetImage =
                RegistryImage.named(ImageReference.of("gcr.io", "my-project/my-app", null))
                    .addCredential("username", "password");
            Containerizer containerizer =
                Containerizer.To(targetImage)
                    .setBaseImageLayersCache(Paths.get("base/image/layers"))
                    .setApplicationLayersCache(Paths.get("application/layers"))
                    .addEventHandler(mockJibEventConsumer);

            RegistryImage baseImage =
                RegistryImage.named("base/image").addCredentialRetriever(mockCredentialRetriever);
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(baseImage, buildConfigurationBuilder)
                    .SetLayers(Arrays.asList(mockLayerConfiguration1, mockLayerConfiguration2));
            BuildConfiguration buildConfiguration =
                jibContainerBuilder.toBuildConfiguration(
                    containerizer);

            Assert.AreEqual(
                buildConfigurationBuilder.build().getContainerConfiguration(),
                buildConfiguration.getContainerConfiguration());

            Assert.AreEqual(
                "base/image", buildConfiguration.getBaseImageConfiguration().getImage().toString());
            Assert.AreEqual(
                Arrays.asList(mockCredentialRetriever),
                buildConfiguration.getBaseImageConfiguration().getCredentialRetrievers());

            Assert.AreEqual(
                "gcr.io/my-project/my-app",
                buildConfiguration.getTargetImageConfiguration().getImage().toString());
            Assert.AreEqual(
                1, buildConfiguration.getTargetImageConfiguration().getCredentialRetrievers().size());
            Assert.AreEqual(
                Credential.from("username", "password"),
                buildConfiguration
                    .getTargetImageConfiguration()
                    .getCredentialRetrievers()
                    .get(0)
                    .retrieve()
                    .OrElseThrow(() => new AssertionException("")));

            Assert.AreEqual(ImmutableHashSet.Create("latest"), buildConfiguration.getAllTargetImageTags());

            Assert.AreEqual(
                Arrays.asList(mockLayerConfiguration1, mockLayerConfiguration2),
                buildConfiguration.getLayerConfigurations());

            buildConfiguration.getEventHandlers().Dispatch(mockJibEvent);
            Mock.Get(mockJibEventConsumer).Verify(m => m(mockJibEvent));

            Assert.AreEqual("jib-core", buildConfiguration.getToolName());

            Assert.AreEqual(ManifestFormat.V22, buildConfiguration.getTargetFormat());

            Assert.AreEqual("jib-core", buildConfiguration.getToolName());

            // Changes jibContainerBuilder.
            buildConfiguration =
                jibContainerBuilder
                    .SetFormat(ImageFormat.OCI)
                    .toBuildConfiguration(
                        containerizer
                            .withAdditionalTag("tag1")
                            .withAdditionalTag("tag2")
                            .setToolName("toolName"));
            Assert.AreEqual(ManifestFormat.OCI, buildConfiguration.getTargetFormat());
            Assert.AreEqual(
                ImmutableHashSet.Create("latest", "tag1", "tag2"), buildConfiguration.getAllTargetImageTags());
            Assert.AreEqual("toolName", buildConfiguration.getToolName());
        }

        /** Verify that an internally-created ExecutorService is shutdown. */

        [Test]
        public async Task testContainerize_executorCreatedAsync()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.named("base/image"), buildConfigurationBuilder)
                    .SetEntrypoint(Arrays.asList("entry", "point"))
                    .SetEnvironment(ImmutableDic.of("name", "value"))
                    .SetExposedPorts(ImmutableHashSet.Create(Port.tcp(1234), Port.udp(5678)))
                    .SetLabels(ImmutableDic.of("key", "value"))
                    .SetProgramArguments(Arrays.asList("program", "arguments"))
                    .SetCreationTime(Instant.FromUnixTimeMilliseconds(1000))
                    .SetUser("user")
                    .SetWorkingDirectory(AbsoluteUnixPath.get("/working/directory"));

            IContainerizer mockContainerizer = createMockContainerizer();

            await jibContainerBuilder.containerizeAsync(mockContainerizer).ConfigureAwait(false);
        }

        /** Verify that a provided ExecutorService is not shutdown. */

        [Test]
        public async Task testContainerize_configuredExecutorAsync()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.named("base/image"), buildConfigurationBuilder)
                    .SetEntrypoint(Arrays.asList("entry", "point"))
                    .SetEnvironment(ImmutableDic.of("name", "value"))
                    .SetExposedPorts(ImmutableHashSet.Create(Port.tcp(1234), Port.udp(5678)))
                    .SetLabels(ImmutableDic.of("key", "value"))
                    .SetProgramArguments(Arrays.asList("program", "arguments"))
                    .SetCreationTime(Instant.FromUnixTimeMilliseconds(1000))
                    .SetUser("user")
                    .SetWorkingDirectory(AbsoluteUnixPath.get("/working/directory"));
            IContainerizer mockContainerizer = createMockContainerizer();

            await jibContainerBuilder.containerizeAsync(mockContainerizer).ConfigureAwait(false);
        }

        private IContainerizer createMockContainerizer()
        {
            ImageReference targetImage = ImageReference.parse("target-image");
            IContainerizer mockContainerizer = Mock.Of<IContainerizer>();
            IStepsRunner stepsRunner = Mock.Of<IStepsRunner>();
            IBuildResult mockBuildResult = Mock.Of<IBuildResult>();

            Mock.Get(mockContainerizer).Setup(m => m.getImageConfiguration()).Returns(ImageConfiguration.builder(targetImage).build());

            Mock.Get(mockContainerizer).Setup(m => m.createStepsRunner(It.IsAny<BuildConfiguration>())).Returns(stepsRunner);

            Mock.Get(stepsRunner).Setup(s => s.runAsync()).Returns(Task.FromResult(mockBuildResult));

            Mock.Get(mockBuildResult).Setup(m => m.getImageDigest()).Returns(
                    DescriptorDigest.fromHash(
                        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));

            Mock.Get(mockBuildResult).Setup(m => m.getImageId()).Returns(
                    DescriptorDigest.fromHash(
                        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));

            Mock.Get(mockContainerizer).Setup(m => m.getAdditionalTags()).Returns(new HashSet<string>());

            Mock.Get(mockContainerizer).Setup(m => m.getBaseImageLayersCacheDirectory()).Returns(Paths.get("/"));

            Mock.Get(mockContainerizer).Setup(m => m.getApplicationLayersCacheDirectory()).Returns(Paths.get("/"));

            Mock.Get(mockContainerizer).Setup(m => m.getAllowInsecureRegistries()).Returns(false);

            Mock.Get(mockContainerizer).Setup(m => m.getToolName()).Returns("mocktool");

            Mock.Get(mockContainerizer).Setup(m => m.buildEventHandlers()).Returns(EventHandlers.NONE);

            return mockContainerizer;
        }
    }
}