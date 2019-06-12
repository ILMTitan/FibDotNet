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

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link JibContainerBuilder}. */

    [RunWith(typeof(MockitoJUnitRunner))]
    public class JibContainerBuilderTest
    {
        private BuildConfiguration.Builder spyBuildConfigurationBuilder = Mock.Of<BuildConfiguration.Builder>();
        private LayerConfiguration mockLayerConfiguration1 = Mock.Of<LayerConfiguration>();
        private LayerConfiguration mockLayerConfiguration2 = Mock.Of<LayerConfiguration>();
        private CredentialRetriever mockCredentialRetriever = Mock.Of<CredentialRetriever>();
        private ExecutorService mockExecutorService = Mock.Of<ExecutorService>();
        private Action<JibEvent> mockJibEventConsumer = Mock.Of<Action<JibEvent>>();
        private JibEvent mockJibEvent = Mock.Of<JibEvent>();

        [Test]
        public void testToBuildConfiguration_containerConfigurationSet()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.named("base/image"), spyBuildConfigurationBuilder)
                    .setEntrypoint(Arrays.asList("entry", "point"))
                    .setEnvironment(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["name"] = "value" }))
                    .setExposedPorts(ImmutableHashSet.Create(Port.tcp(1234), Port.udp(5678)))
                    .setLabels(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["key"] = "value" }))
                    .setProgramArguments(Arrays.asList("program", "arguments"))
                    .setCreationTime(Instant.FromUnixTimeMilliseconds(1000))
                    .setUser("user")
                    .setWorkingDirectory(AbsoluteUnixPath.get("/working/directory"));

            BuildConfiguration buildConfiguration =
                jibContainerBuilder.toBuildConfiguration(
                    Containerizer.to(RegistryImage.named("target/image")));
            ContainerConfiguration containerConfiguration = buildConfiguration.getContainerConfiguration();
            Assert.AreEqual(Arrays.asList("entry", "point"), containerConfiguration.getEntrypoint());
            Assert.AreEqual(
                ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["name"]= "value" }), containerConfiguration.getEnvironmentMap());
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
                new JibContainerBuilder(RegistryImage.named("base/image"), spyBuildConfigurationBuilder)
                    .setEntrypoint("entry", "point")
                    .setEnvironment(ImmutableDic.of("name", "value"))
                    .addEnvironmentVariable("environment", "variable")
                    .setExposedPorts(Port.tcp(1234), Port.udp(5678))
                    .addExposedPort(Port.tcp(1337))
                    .setLabels(ImmutableDic.of("key", "value"))
                    .addLabel("added", "label")
                    .setProgramArguments("program", "arguments");

            BuildConfiguration buildConfiguration =
                jibContainerBuilder.toBuildConfiguration(
                    Containerizer.to(RegistryImage.named("target/image")));
            ContainerConfiguration containerConfiguration = buildConfiguration.getContainerConfiguration();
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
                Containerizer.to(targetImage)
                    .setBaseImageLayersCache(Paths.get("base/image/layers"))
                    .setApplicationLayersCache(Paths.get("application/layers"))
                    .addEventHandler<JibEvent>(mockJibEventConsumer);

            RegistryImage baseImage =
                RegistryImage.named("base/image").addCredentialRetriever(mockCredentialRetriever);
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(baseImage, spyBuildConfigurationBuilder)
                    .setLayers(Arrays.asList(mockLayerConfiguration1, mockLayerConfiguration2));
            BuildConfiguration buildConfiguration =
                jibContainerBuilder.toBuildConfiguration(
                    containerizer);

            Assert.AreEqual(
                spyBuildConfigurationBuilder.build().getContainerConfiguration(),
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
                    .orElseThrow(() => new AssertionException("")));

            Assert.AreEqual(ImmutableHashSet.Create("latest"), buildConfiguration.getAllTargetImageTags());

            Mock.Get(spyBuildConfigurationBuilder).Verify(s => s.setBaseImageLayersCacheDirectory(Paths.get("base/image/layers")));

            Mock.Get(spyBuildConfigurationBuilder).Verify(s => s.setApplicationLayersCacheDirectory(Paths.get("application/layers")));

            Assert.AreEqual(
                Arrays.asList(mockLayerConfiguration1, mockLayerConfiguration2),
                buildConfiguration.getLayerConfigurations());
            
            buildConfiguration.getEventHandlers().dispatch(mockJibEvent);
            Mock.Get(mockJibEventConsumer).Verify(m => m(mockJibEvent));

            Assert.AreEqual("jib-core", buildConfiguration.getToolName());

            Assert.AreSame(typeof(V22ManifestTemplate), buildConfiguration.getTargetFormat());

            Assert.AreEqual("jib-core", buildConfiguration.getToolName());

            // Changes jibContainerBuilder.
            buildConfiguration =
                jibContainerBuilder
                    .setFormat(ImageFormat.OCI)
                    .toBuildConfiguration(
                        containerizer
                            .withAdditionalTag("tag1")
                            .withAdditionalTag("tag2")
                            .setToolName("toolName"));
            Assert.AreSame(typeof(OCIManifestTemplate), buildConfiguration.getTargetFormat());
            Assert.AreEqual(
                ImmutableHashSet.Create("latest", "tag1", "tag2"), buildConfiguration.getAllTargetImageTags());
            Assert.AreEqual("toolName", buildConfiguration.getToolName());
        }

        /** Verify that an internally-created ExecutorService is shutdown. */

        [Test]
        public void testContainerize_executorCreated()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.named("base/image"), spyBuildConfigurationBuilder)
                    .setEntrypoint(Arrays.asList("entry", "point"))
                    .setEnvironment(ImmutableDic.of("name", "value"))
                    .setExposedPorts(ImmutableHashSet.Create(Port.tcp(1234), Port.udp(5678)))
                    .setLabels(ImmutableDic.of("key", "value"))
                    .setProgramArguments(Arrays.asList("program", "arguments"))
                    .setCreationTime(Instant.FromUnixTimeMilliseconds(1000))
                    .setUser("user")
                    .setWorkingDirectory(AbsoluteUnixPath.get("/working/directory"));

            Containerizer mockContainerizer = createMockContainerizer();

            jibContainerBuilder.containerize(mockContainerizer);

        }

        /** Verify that a provided ExecutorService is not shutdown. */

        [Test]
        public void testContainerize_configuredExecutor()
        {
            JibContainerBuilder jibContainerBuilder =
                new JibContainerBuilder(RegistryImage.named("base/image"), spyBuildConfigurationBuilder)
                    .setEntrypoint(Arrays.asList("entry", "point"))
                    .setEnvironment(ImmutableDic.of("name", "value"))
                    .setExposedPorts(ImmutableHashSet.Create(Port.tcp(1234), Port.udp(5678)))
                    .setLabels(ImmutableDic.of("key", "value"))
                    .setProgramArguments(Arrays.asList("program", "arguments"))
                    .setCreationTime(Instant.FromUnixTimeMilliseconds(1000))
                    .setUser("user")
                    .setWorkingDirectory(AbsoluteUnixPath.get("/working/directory"));
            Containerizer mockContainerizer = createMockContainerizer();

            jibContainerBuilder.containerize(mockContainerizer);
        }

        private Containerizer createMockContainerizer()
        {
            ImageReference targetImage = ImageReference.parse("target-image");
            Containerizer mockContainerizer = Mock.Of<Containerizer>();
            StepsRunner stepsRunner = Mock.Of<StepsRunner>();
            BuildResult mockBuildResult = Mock.Of<BuildResult>();

            Mock.Get(mockContainerizer).Setup(m => m.getImageConfiguration()).Returns(ImageConfiguration.builder(targetImage).build());

            Mock.Get(mockContainerizer).Setup(m => m.createStepsRunner(It.IsAny<BuildConfiguration>())).Returns(stepsRunner);

            Mock.Get(stepsRunner).Setup(s => s.run()).Returns(mockBuildResult);

            Mock.Get(mockBuildResult).Setup(m => m.getImageDigest()).Returns(
                    DescriptorDigest.fromHash(
                        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));

            Mock.Get(mockBuildResult).Setup(m => m.getImageId()).Returns(
                    DescriptorDigest.fromHash(
                        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));

            Mock.Get(mockContainerizer).Setup(m => m.getAdditionalTags()).Returns(Collections.emptySet<string>());

            Mock.Get(mockContainerizer).Setup(m => m.getBaseImageLayersCacheDirectory()).Returns(Paths.get("/"));

            Mock.Get(mockContainerizer).Setup(m => m.getApplicationLayersCacheDirectory()).Returns(Paths.get("/"));

            Mock.Get(mockContainerizer).Setup(m => m.getAllowInsecureRegistries()).Returns(false);

            Mock.Get(mockContainerizer).Setup(m => m.getToolName()).Returns("mocktool");
            
            Mock.Get(mockContainerizer).Setup(m => m.buildEventHandlers()).Returns(EventHandlers.NONE);

            return mockContainerizer;
        }
    }
}