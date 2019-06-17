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
using com.google.cloud.tools.jib.async;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.image;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using Moq;
using NodaTime;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static com.google.cloud.tools.jib.builder.steps.PullBaseImageStep;

namespace com.google.cloud.tools.jib.builder.steps
{






















    /** Tests for {@link BuildImageStep}. */
    [RunWith(typeof(MockitoJUnitRunner))]
    public class BuildImageStepTest
    {
        private IEventHandlers mockEventHandlers = Mock.Of<IEventHandlers>();
        private IBuildConfiguration mockBuildConfiguration = Mock.Of<IBuildConfiguration>();
        private IContainerConfiguration mockContainerConfiguration = Mock.Of<IContainerConfiguration>();
        private AsyncStep<BaseImageWithAuthorization> mockPullBaseImageStep = Mock.Of<AsyncStep<BaseImageWithAuthorization>>();
        private AsyncStep<IReadOnlyList<AsyncStep<ICachedLayer>>> mockPullAndCacheBaseImageLayersStep = Mock.Of<AsyncStep<IReadOnlyList<AsyncStep<ICachedLayer>>>>();
        private AsyncStep<ICachedLayer> mockPullAndCacheBaseImageLayerStep = Mock.Of<AsyncStep<ICachedLayer>>();
        private AsyncStep<ICachedLayer> mockBuildAndCacheApplicationLayerStepDependencies = Mock.Of<AsyncStep<ICachedLayer>> ();
        private AsyncStep<ICachedLayer> mockBuildAndCacheApplicationLayerStepResources = Mock.Of<AsyncStep<ICachedLayer>>();
        private AsyncStep<ICachedLayer> mockBuildAndCacheApplicationLayerStepClasses = Mock.Of<AsyncStep<ICachedLayer>>();
        private AsyncStep<ICachedLayer> mockBuildAndCacheApplicationLayerStepExtraFiles = Mock.Of<AsyncStep<ICachedLayer>>();
        private ICachedLayer mockCachedLayer = Mock.Of<ICachedLayer>();
        private DescriptorDigest testDescriptorDigest;
        private HistoryEntry nonEmptyLayerHistory;
        private HistoryEntry emptyLayerHistory;

        [SetUp]
        public void setUp()
        {
            testDescriptorDigest =
                DescriptorDigest.fromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            Mock.Get(mockBuildConfiguration).Setup(m => m.getEventHandlers()).Returns(mockEventHandlers);

            Mock.Get(mockBuildConfiguration).Setup(m => m.getContainerConfiguration()).Returns(mockContainerConfiguration);

            Mock.Get(mockBuildConfiguration).Setup(m => m.getToolName()).Returns(() => null);

            Mock.Get(mockContainerConfiguration).Setup(m => m.getCreationTime()).Returns(Instant.FromUnixTimeSeconds(0));

            Mock.Get(mockContainerConfiguration).Setup(m => m.getEnvironmentMap()).Returns(ImmutableDictionary.Create<string, string>());

            Mock.Get(mockContainerConfiguration).Setup(m => m.getProgramArguments()).Returns(ImmutableArray.Create<string>());

            Mock.Get(mockContainerConfiguration).Setup(m => m.getExposedPorts()).Returns(ImmutableHashSet.Create<Port>());

            Mock.Get(mockContainerConfiguration).Setup(m => m.getEntrypoint()).Returns(ImmutableArray.Create<string>());

            Mock.Get(mockContainerConfiguration).Setup(m => m.getUser()).Returns("root");

            Mock.Get(mockCachedLayer).Setup(m => m.getBlobDescriptor()).Returns(new BlobDescriptor(0, testDescriptorDigest));

            nonEmptyLayerHistory =
                HistoryEntry.builder()
                    .setCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .setAuthor("JibBase")
                    .setCreatedBy("jib-test")
                    .build();
            emptyLayerHistory =
                HistoryEntry.builder()
                    .setCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .setAuthor("JibBase")
                    .setCreatedBy("jib-test")
                    .setEmptyLayer(true)
                    .build();

            Image baseImage =
                Image.builder(ManifestFormat.V22)
                    .setArchitecture("wasm")
                    .setOs("js")
                    .addEnvironment(ImmutableDic.of("BASE_ENV", "BASE_ENV_VALUE", "BASE_ENV_2", "DEFAULT"))
                    .addLabel("base.label", "base.label.value")
                    .addLabel("base.label.2", "default")
                    .setWorkingDirectory("/base/working/directory")
                    .setEntrypoint(ImmutableArray.Create("baseImageEntrypoint"))
                    .setProgramArguments(ImmutableArray.Create("catalina.sh", "run"))
                    .setHealthCheck(
                        DockerHealthCheck.fromCommand(ImmutableArray.Create("CMD-SHELL", "echo hi"))
                            .setInterval(Duration.FromSeconds(3))
                            .setTimeout(Duration.FromSeconds(2))
                            .setStartPeriod(Duration.FromSeconds(1))
                            .setRetries(20)
                            .build())
                    .addExposedPorts(ImmutableHashSet.Create(Port.tcp(1000), Port.udp(2000)))
                    .addVolumes(
                        ImmutableHashSet.Create(
                            AbsoluteUnixPath.get("/base/path1"), AbsoluteUnixPath.get("/base/path2")))
                    .addHistory(nonEmptyLayerHistory)
                    .addHistory(emptyLayerHistory)
                    .addHistory(emptyLayerHistory)
                    .build();
            Mock.Get(mockPullAndCacheBaseImageLayerStep).Setup(m => m.getFuture()).Returns(Futures.immediateFutureAsync(mockCachedLayer));

            Mock.Get(mockPullAndCacheBaseImageLayersStep).Setup(m => m.getFuture()).Returns(
                    Futures.immediateFutureAsync<IReadOnlyList<AsyncStep<ICachedLayer>>>(
                        ImmutableArray.Create(
                            mockPullAndCacheBaseImageLayerStep,
                            mockPullAndCacheBaseImageLayerStep,
                            mockPullAndCacheBaseImageLayerStep)));

            Mock.Get(mockPullBaseImageStep).Setup(m => m.getFuture()).Returns(
                    Futures.immediateFutureAsync(
                        new PullBaseImageStep.BaseImageWithAuthorization(baseImage, null)));
            
            Mock.Get(mockBuildAndCacheApplicationLayerStepClasses)
                .Setup(l => l.getFuture())
                .Returns(Futures.immediateFutureAsync<ICachedLayer>(new CachedLayerWithType(mockCachedLayer, "classes")));

            Mock.Get(mockBuildAndCacheApplicationLayerStepDependencies)
                .Setup(l => l.getFuture())
                .Returns(Futures.immediateFutureAsync<ICachedLayer>(new CachedLayerWithType(mockCachedLayer, "dependencies")));

            Mock.Get(mockBuildAndCacheApplicationLayerStepExtraFiles)
                .Setup(l => l.getFuture())
                .Returns(Futures.immediateFutureAsync<ICachedLayer>(new CachedLayerWithType(mockCachedLayer, "extra files")));

            Mock.Get(mockBuildAndCacheApplicationLayerStepResources)
                .Setup(l => l.getFuture())
                .Returns(Futures.immediateFutureAsync<ICachedLayer>(new CachedLayerWithType(mockCachedLayer, "resources")));
        }

        [Test]
        public async Task test_validateAsyncDependenciesAsync()
        {
            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    ImmutableArray.Create(
                        mockBuildAndCacheApplicationLayerStepDependencies,
                        mockBuildAndCacheApplicationLayerStepResources,
                        mockBuildAndCacheApplicationLayerStepClasses));
            Image image = await (await buildImageStep.getFuture()).getFuture();
            Assert.AreEqual(
                testDescriptorDigest, image.getLayers().asList().get(0).getBlobDescriptor().getDigest());
        }

        [Test]
        public async Task test_propagateBaseImageConfigurationAsync()
        {
            Mock.Get(mockContainerConfiguration).Setup(m => m.getEnvironmentMap()).Returns(ImmutableDic.of("MY_ENV", "MY_ENV_VALUE", "BASE_ENV_2", "NEW_VALUE"));

            Mock.Get(mockContainerConfiguration).Setup(m => m.getLabels()).Returns(ImmutableDic.of("my.label", "my.label.value", "base.label.2", "new.value"));

            Mock.Get(mockContainerConfiguration).Setup(m => m.getExposedPorts()).Returns(ImmutableHashSet.Create(Port.tcp(3000), Port.udp(4000)));

            Mock.Get(mockContainerConfiguration).Setup(m => m.getVolumes()).Returns(
                    ImmutableHashSet.Create(
                        AbsoluteUnixPath.get("/new/path1"), AbsoluteUnixPath.get("/new/path2")));

            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    ImmutableArray.Create(
                        mockBuildAndCacheApplicationLayerStepDependencies,
                        mockBuildAndCacheApplicationLayerStepResources,
                        mockBuildAndCacheApplicationLayerStepClasses));
            Image image = await (await buildImageStep.getFuture()).getFuture();
            Assert.AreEqual("wasm", image.getArchitecture());
            Assert.AreEqual("js", image.getOs());
            Assert.AreEqual(
                ImmutableDic.of(
                    "BASE_ENV", "BASE_ENV_VALUE", "MY_ENV", "MY_ENV_VALUE", "BASE_ENV_2", "NEW_VALUE"),
                image.getEnvironment());
            Assert.AreEqual(
                ImmutableDic.of(
                    "base.label",
                    "base.label.value",
                    "my.label",
                    "my.label.value",
                    "base.label.2",
                    "new.value"),
                image.getLabels());
            Assert.IsNotNull(image.getHealthCheck());
            CollectionAssert.AreEqual(
                ImmutableArray.Create("CMD-SHELL", "echo hi"), image.getHealthCheck().getCommand());
            Assert.IsTrue(image.getHealthCheck().getInterval().isPresent());
            Assert.AreEqual(Duration.FromSeconds(3), image.getHealthCheck().getInterval().get());
            Assert.IsTrue(image.getHealthCheck().getTimeout().isPresent());
            Assert.AreEqual(Duration.FromSeconds(2), image.getHealthCheck().getTimeout().get());
            Assert.IsTrue(image.getHealthCheck().getStartPeriod().isPresent());
            Assert.AreEqual(Duration.FromSeconds(1), image.getHealthCheck().getStartPeriod().get());
            Assert.IsTrue(image.getHealthCheck().getRetries().isPresent());
            Assert.AreEqual(20, (int)image.getHealthCheck().getRetries().get());
            Assert.AreEqual(
                ImmutableHashSet.Create(Port.tcp(1000), Port.udp(2000), Port.tcp(3000), Port.udp(4000)),
                image.getExposedPorts());
            Assert.AreEqual(
                ImmutableHashSet.Create(
                    AbsoluteUnixPath.get("/base/path1"),
                    AbsoluteUnixPath.get("/base/path2"),
                    AbsoluteUnixPath.get("/new/path1"),
                    AbsoluteUnixPath.get("/new/path2")),
                image.getVolumes());
            Assert.AreEqual("/base/working/directory", image.getWorkingDirectory());
            Assert.AreEqual("root", image.getUser());

            Assert.AreEqual(image.getHistory().get(0), nonEmptyLayerHistory);
            Assert.AreEqual(image.getHistory().get(1), emptyLayerHistory);
            Assert.AreEqual(image.getHistory().get(2), emptyLayerHistory);
            Assert.AreEqual(ImmutableArray.Create<string>(), image.getEntrypoint());
            Assert.AreEqual(ImmutableArray.Create<string>(), image.getProgramArguments());
        }

        [Test]
        public async Task testOverrideWorkingDirectoryAsync()
        {
            Mock.Get(mockContainerConfiguration).Setup(m => m.getWorkingDirectory()).Returns(AbsoluteUnixPath.get("/my/directory"));

            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    ImmutableArray.Create(
                        mockBuildAndCacheApplicationLayerStepDependencies,
                        mockBuildAndCacheApplicationLayerStepResources,
                        mockBuildAndCacheApplicationLayerStepClasses));
            Image image = await (await buildImageStep.getFuture()).getFuture();

            Assert.AreEqual("/my/directory", image.getWorkingDirectory());
        }

        [Test]
        public async Task test_inheritedEntrypointAsync()
        {
            Mock.Get(mockContainerConfiguration).Setup(m => m.getEntrypoint()).Returns(() => null);

            Mock.Get(mockContainerConfiguration).Setup(m => m.getProgramArguments()).Returns(ImmutableArray.Create("test"));

            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    ImmutableArray.Create(
                        mockBuildAndCacheApplicationLayerStepDependencies,
                        mockBuildAndCacheApplicationLayerStepResources,
                        mockBuildAndCacheApplicationLayerStepClasses));
            Image image = await (await buildImageStep.getFuture()).getFuture();

            CollectionAssert.AreEqual(ImmutableArray.Create("baseImageEntrypoint"), image.getEntrypoint());
            CollectionAssert.AreEqual(ImmutableArray.Create("test"), image.getProgramArguments());
        }

        [Test]
        public async Task test_inheritedEntrypointAndProgramArgumentsAsync()
        {
            Mock.Get(mockContainerConfiguration).Setup(m => m.getEntrypoint()).Returns(() => null);

            Mock.Get(mockContainerConfiguration).Setup(m => m.getProgramArguments()).Returns(() => null);

            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    ImmutableArray.Create(
                        mockBuildAndCacheApplicationLayerStepDependencies,
                        mockBuildAndCacheApplicationLayerStepResources,
                        mockBuildAndCacheApplicationLayerStepClasses));
            Image image = await (await buildImageStep.getFuture()).getFuture();

            CollectionAssert.AreEqual(ImmutableArray.Create("baseImageEntrypoint"), image.getEntrypoint());
            CollectionAssert.AreEqual(ImmutableArray.Create("catalina.sh", "run"), image.getProgramArguments());
        }

        [Test]
        public async Task test_notInheritedProgramArgumentsAsync()
        {
            Mock.Get(mockContainerConfiguration).Setup(m => m.getEntrypoint()).Returns(ImmutableArray.Create("myEntrypoint"));

            Mock.Get(mockContainerConfiguration).Setup(m => m.getProgramArguments()).Returns(() => null);

            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    ImmutableArray.Create(
                        mockBuildAndCacheApplicationLayerStepDependencies,
                        mockBuildAndCacheApplicationLayerStepResources,
                        mockBuildAndCacheApplicationLayerStepClasses));
            Image image = await (await buildImageStep.getFuture()).getFuture();

            CollectionAssert.AreEqual(ImmutableArray.Create("myEntrypoint"), image.getEntrypoint());
            Assert.IsNull(image.getProgramArguments());
        }

        [Test]
        public async Task test_generateHistoryObjectsAsync()
        {
            string createdBy = "jib:" + ProjectInfo.VERSION;
            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    ImmutableArray.Create(
                        mockBuildAndCacheApplicationLayerStepDependencies,
                        mockBuildAndCacheApplicationLayerStepResources,
                        mockBuildAndCacheApplicationLayerStepClasses,
                        mockBuildAndCacheApplicationLayerStepExtraFiles));
            Image image = await (await buildImageStep.getFuture()).getFuture();

            // Make sure history is as expected
            HistoryEntry expectedAddedBaseLayerHistory =
                HistoryEntry.builder()
                    .setCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .setComment("auto-generated by Jib")
                    .build();

            HistoryEntry expectedApplicationLayerHistoryDependencies =
                HistoryEntry.builder()
                    .setCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .setAuthor("Jib")
                    .setCreatedBy(createdBy)
                    .setComment("dependencies")
                    .build();

            HistoryEntry expectedApplicationLayerHistoryResources =
                HistoryEntry.builder()
                    .setCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .setAuthor("Jib")
                    .setCreatedBy(createdBy)
                    .setComment("resources")
                    .build();

            HistoryEntry expectedApplicationLayerHistoryClasses =
                HistoryEntry.builder()
                    .setCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .setAuthor("Jib")
                    .setCreatedBy(createdBy)
                    .setComment("classes")
                    .build();

            HistoryEntry expectedApplicationLayerHistoryExtrafiles =
                HistoryEntry.builder()
                    .setCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .setAuthor("Jib")
                    .setCreatedBy(createdBy)
                    .setComment("extra files")
                    .build();

            // Base layers (1 non-empty propagated, 2 empty propagated, 2 non-empty generated)
            Assert.AreEqual(nonEmptyLayerHistory, image.getHistory().get(0));
            Assert.AreEqual(emptyLayerHistory, image.getHistory().get(1));
            Assert.AreEqual(emptyLayerHistory, image.getHistory().get(2));
            Assert.AreEqual(expectedAddedBaseLayerHistory, image.getHistory().get(3));
            Assert.AreEqual(expectedAddedBaseLayerHistory, image.getHistory().get(4));

            // Application layers (4 generated)
            Assert.AreEqual(expectedApplicationLayerHistoryDependencies, image.getHistory().get(5));
            Assert.AreEqual(expectedApplicationLayerHistoryResources, image.getHistory().get(6));
            Assert.AreEqual(expectedApplicationLayerHistoryClasses, image.getHistory().get(7));
            Assert.AreEqual(expectedApplicationLayerHistoryExtrafiles, image.getHistory().get(8));

            // Should be exactly 9 total
            Assert.AreEqual(9, image.getHistory().size());
        }
    }
}
