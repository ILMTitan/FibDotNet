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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using Jib.Net.Core.Api;
using Jib.Net.Core.BuildSteps;
using Jib.Net.Core.Cache;
using Jib.Net.Core.Events.Progress;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images;
using Jib.Net.Test.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using IBlob = com.google.cloud.tools.jib.blob.IBlob;

namespace com.google.cloud.tools.jib.builder.steps
{
    /** Tests for {@link BuildAndCacheApplicationLayerStep}. */
    public class BuildAndCacheApplicationLayerStepTest : IDisposable
    {
        // TODO: Consolidate with BuildStepsIntegrationTest.
        private static readonly AbsoluteUnixPath EXTRACTION_PATH_ROOT =
            AbsoluteUnixPath.Get("/some/extraction/path/");

        private static readonly AbsoluteUnixPath EXTRA_FILES_LAYER_EXTRACTION_PATH =
            AbsoluteUnixPath.Get("/extra");

        /**
         * Lists the files in the {@code resourcePath} resources directory and creates a {@link
         * LayerConfiguration} with entries from those files.
         */
        private static ILayerConfiguration MakeLayerConfiguration(
            string resourcePath, AbsoluteUnixPath extractionPath)
        {
            IEnumerable<SystemPath> fileStream =
                Files.List(Paths.Get(TestResources.GetResource(resourcePath).ToURI()));
            {
                LayerConfiguration.Builder layerConfigurationBuilder = LayerConfiguration.CreateBuilder();
                    layerConfigurationBuilder.SetName(Path.GetFileName(resourcePath));
                foreach (SystemPath i in fileStream)
                {
                    ((Func<SystemPath, LayerConfiguration.Builder>)(sourceFile =>
                        layerConfigurationBuilder.AddEntry(
                            sourceFile, extractionPath.Resolve(sourceFile.GetFileName()))))(i);
                }
                return layerConfigurationBuilder.Build();
            }
        }

        private static async Task AssertBlobsEqualAsync(IBlob expectedBlob, IBlob blob)
        {
            CollectionAssert.AreEqual(await Blobs.WriteToByteArrayAsync(expectedBlob).ConfigureAwait(false), await Blobs.WriteToByteArrayAsync(blob).ConfigureAwait(false));
        }

        private TemporaryFolder temporaryFolder;

        private readonly IBuildConfiguration mockBuildConfiguration = Mock.Of<IBuildConfiguration>();

        private Cache cache;
        private readonly IEventHandlers mockEventHandlers = Mock.Of<IEventHandlers>();

        private ILayerConfiguration fakeDependenciesLayerConfiguration;
        private ILayerConfiguration fakeSnapshotDependenciesLayerConfiguration;
        private ILayerConfiguration fakeResourcesLayerConfiguration;
        private ILayerConfiguration fakeClassesLayerConfiguration;
        private ILayerConfiguration fakeExtraFilesLayerConfiguration;
        private ILayerConfiguration emptyLayerConfiguration;

        [OneTimeSetUp]
        public void SetUpFixture()
        {
            temporaryFolder = new TemporaryFolder();
        }

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            fakeDependenciesLayerConfiguration =
                MakeLayerConfiguration(
                    "core/application/dependencies", EXTRACTION_PATH_ROOT.Resolve("libs"));
            fakeSnapshotDependenciesLayerConfiguration =
                MakeLayerConfiguration(
                    "core/application/snapshot-dependencies", EXTRACTION_PATH_ROOT.Resolve("libs"));
            fakeResourcesLayerConfiguration =
                MakeLayerConfiguration(
                    "core/application/resources", EXTRACTION_PATH_ROOT.Resolve("resources"));
            fakeClassesLayerConfiguration =
                MakeLayerConfiguration("core/application/classes", EXTRACTION_PATH_ROOT.Resolve("classes"));
            fakeExtraFilesLayerConfiguration =
                LayerConfiguration.CreateBuilder()
                    .AddEntry(
                        Paths.Get(TestResources.GetResource("core/fileA").ToURI()),
                        EXTRA_FILES_LAYER_EXTRACTION_PATH.Resolve("fileA"))
                    .AddEntry(
                        Paths.Get(TestResources.GetResource("core/fileB").ToURI()),
                        EXTRA_FILES_LAYER_EXTRACTION_PATH.Resolve("fileB"))
                    .Build();
            emptyLayerConfiguration = LayerConfiguration.CreateBuilder().Build();

            cache = Cache.WithDirectory(temporaryFolder.NewFolder().ToPath());

            Mock.Get(mockBuildConfiguration).Setup(m => m.GetEventHandlers()).Returns(mockEventHandlers);

            Mock.Get(mockBuildConfiguration).Setup(m => m.GetApplicationLayersCache()).Returns(cache);
        }

        private async Task<ImageLayers> BuildFakeLayersToCacheAsync()
        {
            ImageLayers.Builder applicationLayersBuilder = ImageLayers.CreateBuilder();

            IAsyncStep<IReadOnlyList<ICachedLayer>> buildAndCacheApplicationLayersStep =
                BuildAndCacheApplicationLayerStep.MakeList(
                    mockBuildConfiguration,
                    ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer());

            foreach (ICachedLayer applicationLayer in await buildAndCacheApplicationLayersStep.GetFuture().ConfigureAwait(false))

            {
                applicationLayersBuilder.Add(applicationLayer);
            }

            return applicationLayersBuilder.Build();
        }

        [Test]
        public async Task TestRunAsync()
        {
            ImmutableArray<ILayerConfiguration> fakeLayerConfigurations =
                ImmutableArray.Create(
                    fakeDependenciesLayerConfiguration,
                    fakeSnapshotDependenciesLayerConfiguration,
                    fakeResourcesLayerConfiguration,
                    fakeClassesLayerConfiguration,
                    fakeExtraFilesLayerConfiguration);
            Mock.Get(mockBuildConfiguration).Setup(m => m.GetLayerConfigurations()).Returns(fakeLayerConfigurations);

            // Populates the cache.
            ImageLayers applicationLayers = await BuildFakeLayersToCacheAsync().ConfigureAwait(false);
            Assert.AreEqual(5, applicationLayers.Size());

            ImmutableArray<LayerEntry> dependenciesLayerEntries =
                fakeLayerConfigurations[0].GetLayerEntries();
            ImmutableArray<LayerEntry> snapshotDependenciesLayerEntries =
                fakeLayerConfigurations[1].GetLayerEntries();
            ImmutableArray<LayerEntry> resourcesLayerEntries =
                fakeLayerConfigurations[2].GetLayerEntries();
            ImmutableArray<LayerEntry> classesLayerEntries =
                fakeLayerConfigurations[3].GetLayerEntries();
            ImmutableArray<LayerEntry> extraFilesLayerEntries =
                fakeLayerConfigurations[4].GetLayerEntries();

            CachedLayer dependenciesCachedLayer =
                await cache.RetrieveAsync(dependenciesLayerEntries).OrElseThrowAsync(() => new AssertionException("")).ConfigureAwait(false);
            CachedLayer snapshotDependenciesCachedLayer =
                await cache.RetrieveAsync(snapshotDependenciesLayerEntries).OrElseThrowAsync(() => new AssertionException("")).ConfigureAwait(false);
            CachedLayer resourcesCachedLayer =
                await cache.RetrieveAsync(resourcesLayerEntries).OrElseThrowAsync(() => new AssertionException("")).ConfigureAwait(false);
            CachedLayer classesCachedLayer =
                await cache.RetrieveAsync(classesLayerEntries).OrElseThrowAsync(() => new AssertionException("")).ConfigureAwait(false);
            CachedLayer extraFilesCachedLayer =
                await cache.RetrieveAsync(extraFilesLayerEntries).OrElseThrowAsync(() => new AssertionException("")).ConfigureAwait(false);

            // Verifies that the cached layers are up-to-date.
            Assert.AreEqual(
                applicationLayers.Get(0).GetBlobDescriptor().GetDigest(),
                dependenciesCachedLayer.GetDigest());
            Assert.AreEqual(
                applicationLayers.Get(1).GetBlobDescriptor().GetDigest(),
                snapshotDependenciesCachedLayer.GetDigest());
            Assert.AreEqual(
                applicationLayers.Get(2).GetBlobDescriptor().GetDigest(), resourcesCachedLayer.GetDigest());
            Assert.AreEqual(
                applicationLayers.Get(3).GetBlobDescriptor().GetDigest(), classesCachedLayer.GetDigest());
            Assert.AreEqual(
                applicationLayers.Get(4).GetBlobDescriptor().GetDigest(),
                extraFilesCachedLayer.GetDigest());

            // Verifies that the cache reader gets the same layers as the newest application layers.
            await AssertBlobsEqualAsync(applicationLayers.Get(0).GetBlob(), dependenciesCachedLayer.GetBlob()).ConfigureAwait(false);
            await AssertBlobsEqualAsync(applicationLayers.Get(1).GetBlob(), snapshotDependenciesCachedLayer.GetBlob()).ConfigureAwait(false);
            await AssertBlobsEqualAsync(applicationLayers.Get(2).GetBlob(), resourcesCachedLayer.GetBlob()).ConfigureAwait(false);
            await AssertBlobsEqualAsync(applicationLayers.Get(3).GetBlob(), classesCachedLayer.GetBlob()).ConfigureAwait(false);
            await AssertBlobsEqualAsync(applicationLayers.Get(4).GetBlob(), extraFilesCachedLayer.GetBlob()).ConfigureAwait(false);
        }

        [Test]
        public async Task TestRun_emptyLayersIgnoredAsync()
        {
            ImmutableArray<ILayerConfiguration> fakeLayerConfigurations =
                ImmutableArray.Create(
                    fakeDependenciesLayerConfiguration,
                    emptyLayerConfiguration,
                    fakeResourcesLayerConfiguration,
                    fakeClassesLayerConfiguration,
                    emptyLayerConfiguration);
            Mock.Get(mockBuildConfiguration).Setup(m => m.GetLayerConfigurations()).Returns(fakeLayerConfigurations);

            // Populates the cache.
            ImageLayers applicationLayers = await BuildFakeLayersToCacheAsync().ConfigureAwait(false);
            Assert.AreEqual(3, applicationLayers.Size());

            ImmutableArray<LayerEntry> dependenciesLayerEntries =
                fakeLayerConfigurations[0].GetLayerEntries();
            ImmutableArray<LayerEntry> resourcesLayerEntries =
                fakeLayerConfigurations[2].GetLayerEntries();
            ImmutableArray<LayerEntry> classesLayerEntries =
                fakeLayerConfigurations[3].GetLayerEntries();

            CachedLayer dependenciesCachedLayer =
                await cache.RetrieveAsync(dependenciesLayerEntries).OrElseThrowAsync(() => new AssertionException("")).ConfigureAwait(false);
            CachedLayer resourcesCachedLayer =
                await cache.RetrieveAsync(resourcesLayerEntries).OrElseThrowAsync(() => new AssertionException("")).ConfigureAwait(false);
            CachedLayer classesCachedLayer =
                await cache.RetrieveAsync(classesLayerEntries).OrElseThrowAsync(() => new AssertionException("")).ConfigureAwait(false);

            // Verifies that the cached layers are up-to-date.
            Assert.AreEqual(
                applicationLayers.Get(0).GetBlobDescriptor().GetDigest(),
                dependenciesCachedLayer.GetDigest());
            Assert.AreEqual(
                applicationLayers.Get(1).GetBlobDescriptor().GetDigest(), resourcesCachedLayer.GetDigest());
            Assert.AreEqual(
                applicationLayers.Get(2).GetBlobDescriptor().GetDigest(), classesCachedLayer.GetDigest());

            // Verifies that the cache reader gets the same layers as the newest application layers.
            await AssertBlobsEqualAsync(applicationLayers.Get(0).GetBlob(), dependenciesCachedLayer.GetBlob()).ConfigureAwait(false);
            await AssertBlobsEqualAsync(applicationLayers.Get(1).GetBlob(), resourcesCachedLayer.GetBlob()).ConfigureAwait(false);
            await AssertBlobsEqualAsync(applicationLayers.Get(2).GetBlob(), classesCachedLayer.GetBlob()).ConfigureAwait(false);
        }
    }
}
