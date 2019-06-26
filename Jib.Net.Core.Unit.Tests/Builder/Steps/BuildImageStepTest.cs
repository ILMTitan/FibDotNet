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
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Builder.Steps;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images;
using Jib.Net.Core.Images.Json;
using Moq;
using NodaTime;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static Jib.Net.Core.Builder.Steps.PullBaseImageStep;

namespace com.google.cloud.tools.jib.builder.steps
{
    /** Tests for {@link BuildImageStep}. */
    public class BuildImageStepTest
    {
        private readonly IEventHandlers mockEventHandlers = Mock.Of<IEventHandlers>();
        private readonly IBuildConfiguration mockBuildConfiguration = Mock.Of<IBuildConfiguration>();
        private readonly IContainerConfiguration mockContainerConfiguration = Mock.Of<IContainerConfiguration>();
        private readonly IAsyncStep<BaseImageWithAuthorization> mockPullBaseImageStep = Mock.Of<IAsyncStep<BaseImageWithAuthorization>>();
        private readonly IAsyncStep<IReadOnlyList<ICachedLayer>> mockPullAndCacheBaseImageLayersStep = Mock.Of<IAsyncStep<IReadOnlyList<ICachedLayer>>>();
        private readonly IAsyncStep<ICachedLayer> mockPullAndCacheBaseImageLayerStep = Mock.Of<IAsyncStep<ICachedLayer>>();
        private ICachedLayer mockClassesLayer;
        private ICachedLayer mockDependenciesLayer;
        private ICachedLayer mockResourcesLayer;
        private ICachedLayer mockExtraFilesLayer;
        private readonly ICachedLayer mockCachedLayer = Mock.Of<ICachedLayer>();
        private DescriptorDigest testDescriptorDigest;
        private HistoryEntry nonEmptyLayerHistory;
        private HistoryEntry emptyLayerHistory;

        private readonly IAsyncStep<IReadOnlyList<ICachedLayer>> mockBuildAndCacheApplicationLayersStep =
            Mock.Of<IAsyncStep<IReadOnlyList<ICachedLayer>>>();

        [SetUp]
        public void SetUp()
        {
            testDescriptorDigest =
                DescriptorDigest.FromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            Mock.Get(mockBuildConfiguration).Setup(m => m.GetEventHandlers()).Returns(mockEventHandlers);

            Mock.Get(mockBuildConfiguration).Setup(m => m.GetContainerConfiguration()).Returns(mockContainerConfiguration);

            Mock.Get(mockBuildConfiguration).Setup(m => m.GetToolName()).Returns(() => null);

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetCreationTime()).Returns(Instant.FromUnixTimeSeconds(0));

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetEnvironmentMap()).Returns(ImmutableDictionary.Create<string, string>());

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetProgramArguments()).Returns(ImmutableArray.Create<string>());

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetExposedPorts()).Returns(ImmutableHashSet.Create<Port>());

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetEntrypoint()).Returns(ImmutableArray.Create<string>());

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetUser()).Returns("root");

            Mock.Get(mockCachedLayer).Setup(m => m.GetBlobDescriptor()).Returns(new BlobDescriptor(0, testDescriptorDigest));

            nonEmptyLayerHistory =
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .SetAuthor("JibBase")
                    .SetCreatedBy("jib-test")
                    .Build();
            emptyLayerHistory =
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .SetAuthor("JibBase")
                    .SetCreatedBy("jib-test")
                    .SetEmptyLayer(true)
                    .Build();

            Image baseImage =
                Image.CreateBuilder(ManifestFormat.V22)
                    .SetArchitecture("wasm")
                    .SetOs("js")
                    .AddEnvironment(ImmutableDic.Of("BASE_ENV", "BASE_ENV_VALUE", "BASE_ENV_2", "DEFAULT"))
                    .AddLabel("base.label", "base.label.value")
                    .AddLabel("base.label.2", "default")
                    .SetWorkingDirectory("/base/working/directory")
                    .SetEntrypoint(ImmutableArray.Create("baseImageEntrypoint"))
                    .SetProgramArguments(ImmutableArray.Create("catalina.sh", "run"))
                    .SetHealthCheck(
                        DockerHealthCheck.FromCommand(ImmutableArray.Create("CMD-SHELL", "echo hi"))
                            .SetInterval(Duration.FromSeconds(3))
                            .SetTimeout(Duration.FromSeconds(2))
                            .SetStartPeriod(Duration.FromSeconds(1))
                            .SetRetries(20)
                            .Build())
                    .AddExposedPorts(ImmutableHashSet.Create(Port.Tcp(1000), Port.Udp(2000)))
                    .AddVolumes(
                        ImmutableHashSet.Create(
                            AbsoluteUnixPath.Get("/base/path1"), AbsoluteUnixPath.Get("/base/path2")))
                    .AddHistory(nonEmptyLayerHistory)
                    .AddHistory(emptyLayerHistory)
                    .AddHistory(emptyLayerHistory)
                    .Build();
            Mock.Get(mockPullAndCacheBaseImageLayerStep).Setup(m => m.GetFuture()).Returns(Futures.ImmediateFutureAsync(mockCachedLayer));

            Mock.Get(mockPullAndCacheBaseImageLayersStep).Setup(m => m.GetFuture()).Returns(
                    Futures.ImmediateFutureAsync<IReadOnlyList<ICachedLayer>>(
                        ImmutableArray.Create(
                            mockCachedLayer,
                            mockCachedLayer,
                            mockCachedLayer)));

            Mock.Get(mockPullBaseImageStep).Setup(m => m.GetFuture()).Returns(
                    Futures.ImmediateFutureAsync(
                        new PullBaseImageStep.BaseImageWithAuthorization(baseImage, null)));

            mockClassesLayer = new CachedLayerWithType(mockCachedLayer, "classes");

            mockDependenciesLayer = new CachedLayerWithType(mockCachedLayer, "dependencies");

            mockExtraFilesLayer = new CachedLayerWithType(mockCachedLayer, "extra files");

            mockResourcesLayer = new CachedLayerWithType(mockCachedLayer, "resources");
        }

        [Test]
        public async Task Test_validateAsyncDependenciesAsync()
        {
            Mock.Get(mockBuildAndCacheApplicationLayersStep).Setup(s => s.GetFuture()).Returns(Task.FromResult<IReadOnlyList<ICachedLayer>>(ImmutableArray.Create(
                        mockDependenciesLayer,
                        mockResourcesLayer,
                        mockClassesLayer)));
            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    mockBuildAndCacheApplicationLayersStep);
            Image image = await buildImageStep.GetFuture().ConfigureAwait(false);
            Assert.AreEqual(
                testDescriptorDigest, image.GetLayers()[0].GetBlobDescriptor().GetDigest());
        }

        [Test]
        public async Task Test_propagateBaseImageConfigurationAsync()
        {
            Mock.Get(mockContainerConfiguration).Setup(m => m.GetEnvironmentMap()).Returns(ImmutableDic.Of("MY_ENV", "MY_ENV_VALUE", "BASE_ENV_2", "NEW_VALUE"));

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetLabels()).Returns(ImmutableDic.Of("my.label", "my.label.value", "base.label.2", "new.value"));

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetExposedPorts()).Returns(ImmutableHashSet.Create(Port.Tcp(3000), Port.Udp(4000)));

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetVolumes()).Returns(
                    ImmutableHashSet.Create(
                        AbsoluteUnixPath.Get("/new/path1"), AbsoluteUnixPath.Get("/new/path2")));
            Mock.Get(mockBuildAndCacheApplicationLayersStep).Setup(s => s.GetFuture()).Returns(Task.FromResult<IReadOnlyList<ICachedLayer>>(ImmutableArray.Create(
                        mockDependenciesLayer,
                        mockResourcesLayer,
                        mockClassesLayer)));

            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    mockBuildAndCacheApplicationLayersStep);
            Image image = await buildImageStep.GetFuture().ConfigureAwait(false);
            Assert.AreEqual("wasm", image.GetArchitecture());
            Assert.AreEqual("js", image.GetOs());
            Assert.AreEqual(
                new Dictionary<string, string>
                {
                    ["BASE_ENV"] = "BASE_ENV_VALUE",
                    ["MY_ENV"] = "MY_ENV_VALUE",
                    ["BASE_ENV_2"] = "NEW_VALUE"
                }.ToImmutableDictionary(),
                image.GetEnvironment());
            Assert.AreEqual(
                new Dictionary<string, string>
                {
                    ["base.label"] = "base.label.value",
                    ["my.label"] = "my.label.value",
                    ["base.label.2"] = "new.value"
                }.ToImmutableDictionary(),
                image.GetLabels());
            Assert.IsNotNull(image.GetHealthCheck());
            CollectionAssert.AreEqual(
                ImmutableArray.Create("CMD-SHELL", "echo hi"), image.GetHealthCheck().GetCommand());
            Assert.IsTrue(image.GetHealthCheck().GetInterval().IsPresent());
            Assert.AreEqual(Duration.FromSeconds(3), image.GetHealthCheck().GetInterval().Get());
            Assert.IsTrue(image.GetHealthCheck().GetTimeout().IsPresent());
            Assert.AreEqual(Duration.FromSeconds(2), image.GetHealthCheck().GetTimeout().Get());
            Assert.IsTrue(image.GetHealthCheck().GetStartPeriod().IsPresent());
            Assert.AreEqual(Duration.FromSeconds(1), image.GetHealthCheck().GetStartPeriod().Get());
            Assert.IsTrue(image.GetHealthCheck().GetRetries().IsPresent());
            Assert.AreEqual(20, image.GetHealthCheck().GetRetries().Get());
            Assert.AreEqual(
                ImmutableHashSet.Create(Port.Tcp(1000), Port.Udp(2000), Port.Tcp(3000), Port.Udp(4000)),
                image.GetExposedPorts());
            Assert.AreEqual(
                ImmutableHashSet.Create(
                    AbsoluteUnixPath.Get("/base/path1"),
                    AbsoluteUnixPath.Get("/base/path2"),
                    AbsoluteUnixPath.Get("/new/path1"),
                    AbsoluteUnixPath.Get("/new/path2")),
                image.GetVolumes());
            Assert.AreEqual("/base/working/directory", image.GetWorkingDirectory());
            Assert.AreEqual("root", image.GetUser());

            Assert.AreEqual(image.GetHistory()[0], nonEmptyLayerHistory);
            Assert.AreEqual(image.GetHistory()[1], emptyLayerHistory);
            Assert.AreEqual(image.GetHistory()[2], emptyLayerHistory);
            Assert.AreEqual(ImmutableArray.Create<string>(), image.GetEntrypoint());
            Assert.AreEqual(ImmutableArray.Create<string>(), image.GetProgramArguments());
        }

        [Test]
        public async Task TestOverrideWorkingDirectoryAsync()
        {
            Mock.Get(mockContainerConfiguration).Setup(m => m.GetWorkingDirectory()).Returns(AbsoluteUnixPath.Get("/my/directory"));
            Mock.Get(mockBuildAndCacheApplicationLayersStep).Setup(s => s.GetFuture()).Returns(Task.FromResult<IReadOnlyList<ICachedLayer>>(ImmutableArray.Create(
                        mockDependenciesLayer,
                        mockResourcesLayer,
                        mockClassesLayer)));

            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    mockBuildAndCacheApplicationLayersStep);
            Image image = await buildImageStep.GetFuture().ConfigureAwait(false);

            Assert.AreEqual("/my/directory", image.GetWorkingDirectory());
        }

        [Test]
        public async Task Test_inheritedEntrypointAsync()
        {
            Mock.Get(mockContainerConfiguration).Setup(m => m.GetEntrypoint()).Returns(() => null);

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetProgramArguments()).Returns(ImmutableArray.Create("test"));
            Mock.Get(mockBuildAndCacheApplicationLayersStep).Setup(s => s.GetFuture()).Returns(Task.FromResult<IReadOnlyList<ICachedLayer>>(ImmutableArray.Create(
                        mockDependenciesLayer,
                        mockResourcesLayer,
                        mockClassesLayer)));

            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    mockBuildAndCacheApplicationLayersStep);
            Image image = await buildImageStep.GetFuture().ConfigureAwait(false);

            CollectionAssert.AreEqual(ImmutableArray.Create("baseImageEntrypoint"), image.GetEntrypoint());
            CollectionAssert.AreEqual(ImmutableArray.Create("test"), image.GetProgramArguments());
        }

        [Test]
        public async Task Test_inheritedEntrypointAndProgramArgumentsAsync()
        {
            Mock.Get(mockContainerConfiguration).Setup(m => m.GetEntrypoint()).Returns(() => null);

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetProgramArguments()).Returns(() => null);
            Mock.Get(mockBuildAndCacheApplicationLayersStep).Setup(s => s.GetFuture()).Returns(Task.FromResult<IReadOnlyList<ICachedLayer>>(ImmutableArray.Create(
                        mockDependenciesLayer,
                        mockResourcesLayer,
                        mockClassesLayer)));

            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    mockBuildAndCacheApplicationLayersStep);
            Image image = await buildImageStep.GetFuture().ConfigureAwait(false);

            CollectionAssert.AreEqual(ImmutableArray.Create("baseImageEntrypoint"), image.GetEntrypoint());
            CollectionAssert.AreEqual(ImmutableArray.Create("catalina.sh", "run"), image.GetProgramArguments());
        }

        [Test]
        public async Task Test_notInheritedProgramArgumentsAsync()
        {
            Mock.Get(mockContainerConfiguration).Setup(m => m.GetEntrypoint()).Returns(ImmutableArray.Create("myEntrypoint"));

            Mock.Get(mockContainerConfiguration).Setup(m => m.GetProgramArguments()).Returns(() => null);
            Mock.Get(mockBuildAndCacheApplicationLayersStep).Setup(s => s.GetFuture()).Returns(Task.FromResult<IReadOnlyList<ICachedLayer>>(ImmutableArray.Create(
                        mockDependenciesLayer,
                        mockResourcesLayer,
                        mockClassesLayer)));

            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    mockBuildAndCacheApplicationLayersStep);
            Image image = await buildImageStep.GetFuture().ConfigureAwait(false);

            CollectionAssert.AreEqual(ImmutableArray.Create("myEntrypoint"), image.GetEntrypoint());
            Assert.IsNull(image.GetProgramArguments());
        }

        [Test]
        public async Task Test_generateHistoryObjectsAsync()
        {
            Mock.Get(mockBuildAndCacheApplicationLayersStep).Setup(s => s.GetFuture()).Returns(Task.FromResult<IReadOnlyList<ICachedLayer>>(ImmutableArray.Create(
                        mockDependenciesLayer,
                        mockResourcesLayer,
                        mockClassesLayer,
                        mockExtraFilesLayer)));
            string createdBy = "jib:" + ProjectInfo.VERSION;
            BuildImageStep buildImageStep =
                new BuildImageStep(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer(),
                    mockPullBaseImageStep,
                    mockPullAndCacheBaseImageLayersStep,
                    mockBuildAndCacheApplicationLayersStep);
            Image image = await buildImageStep.GetFuture().ConfigureAwait(false);

            // Make sure history is as expected
            HistoryEntry expectedAddedBaseLayerHistory =
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .SetComment("auto-generated by Jib")
                    .Build();

            HistoryEntry expectedApplicationLayerHistoryDependencies =
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .SetAuthor("Jib")
                    .SetCreatedBy(createdBy)
                    .SetComment("dependencies")
                    .Build();

            HistoryEntry expectedApplicationLayerHistoryResources =
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .SetAuthor("Jib")
                    .SetCreatedBy(createdBy)
                    .SetComment("resources")
                    .Build();

            HistoryEntry expectedApplicationLayerHistoryClasses =
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .SetAuthor("Jib")
                    .SetCreatedBy(createdBy)
                    .SetComment("classes")
                    .Build();

            HistoryEntry expectedApplicationLayerHistoryExtrafiles =
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .SetAuthor("Jib")
                    .SetCreatedBy(createdBy)
                    .SetComment("extra files")
                    .Build();

            // Base layers (1 non-empty propagated, 2 empty propagated, 2 non-empty generated)
            Assert.AreEqual(nonEmptyLayerHistory, image.GetHistory()[0]);
            Assert.AreEqual(emptyLayerHistory, image.GetHistory()[1]);
            Assert.AreEqual(emptyLayerHistory, image.GetHistory()[2]);
            Assert.AreEqual(expectedAddedBaseLayerHistory, image.GetHistory()[3]);
            Assert.AreEqual(expectedAddedBaseLayerHistory, image.GetHistory()[4]);

            // Application layers (4 generated)
            Assert.AreEqual(expectedApplicationLayerHistoryDependencies, image.GetHistory()[5]);
            Assert.AreEqual(expectedApplicationLayerHistoryResources, image.GetHistory()[6]);
            Assert.AreEqual(expectedApplicationLayerHistoryClasses, image.GetHistory()[7]);
            Assert.AreEqual(expectedApplicationLayerHistoryExtrafiles, image.GetHistory()[8]);

            // Should be exactly 9 total
            Assert.AreEqual(9, image.GetHistory().Length);
        }
    }
}
