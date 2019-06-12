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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.image;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Blob = com.google.cloud.tools.jib.blob.Blob;

namespace com.google.cloud.tools.jib.builder.steps {


































/** Tests for {@link BuildAndCacheApplicationLayerStep}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class BuildAndCacheApplicationLayerStepTest {

  // TODO: Consolidate with BuildStepsIntegrationTest.
  private static readonly AbsoluteUnixPath EXTRACTION_PATH_ROOT =
      AbsoluteUnixPath.get("/some/extraction/path/");

  private static readonly AbsoluteUnixPath EXTRA_FILES_LAYER_EXTRACTION_PATH =
      AbsoluteUnixPath.get("/extra");

  /**
   * Lists the files in the {@code resourcePath} resources directory and creates a {@link
   * LayerConfiguration} with entries from those files.
   */
  private static LayerConfiguration makeLayerConfiguration(
      string resourcePath, AbsoluteUnixPath extractionPath) {
            IEnumerable<SystemPath> fileStream =
                Files.list(Paths.get(Resources.getResource(resourcePath).toURI()));
            {
                LayerConfiguration.Builder layerConfigurationBuilder = LayerConfiguration.builder();
                fileStream.forEach(
                    sourceFile =>
                        layerConfigurationBuilder.addEntry(
                            sourceFile, extractionPath.resolve(sourceFile.getFileName())));
                return layerConfigurationBuilder.build();
            }
  }

  private static void assertBlobsEqual(Blob expectedBlob, Blob blob) {
    CollectionAssert.AreEqual(Blobs.writeToByteArray(expectedBlob), Blobs.writeToByteArray(blob));
  }

  public TemporaryFolder temporaryFolder = new TemporaryFolder();

  private BuildConfiguration mockBuildConfiguration = Mock.Of<BuildConfiguration>();

  private Cache cache;
  private EventHandlers mockEventHandlers = Mock.Of<EventHandlers>();

  private LayerConfiguration fakeDependenciesLayerConfiguration;
  private LayerConfiguration fakeSnapshotDependenciesLayerConfiguration;
  private LayerConfiguration fakeResourcesLayerConfiguration;
  private LayerConfiguration fakeClassesLayerConfiguration;
  private LayerConfiguration fakeExtraFilesLayerConfiguration;
  private LayerConfiguration emptyLayerConfiguration;

  [SetUp]
  public void setUp() {
    fakeDependenciesLayerConfiguration =
        makeLayerConfiguration(
            "core/application/dependencies", EXTRACTION_PATH_ROOT.resolve("libs"));
    fakeSnapshotDependenciesLayerConfiguration =
        makeLayerConfiguration(
            "core/application/snapshot-dependencies", EXTRACTION_PATH_ROOT.resolve("libs"));
    fakeResourcesLayerConfiguration =
        makeLayerConfiguration(
            "core/application/resources", EXTRACTION_PATH_ROOT.resolve("resources"));
    fakeClassesLayerConfiguration =
        makeLayerConfiguration("core/application/classes", EXTRACTION_PATH_ROOT.resolve("classes"));
    fakeExtraFilesLayerConfiguration =
        LayerConfiguration.builder()
            .addEntry(
                Paths.get(Resources.getResource("core/fileA").toURI()),
                EXTRA_FILES_LAYER_EXTRACTION_PATH.resolve("fileA"))
            .addEntry(
                Paths.get(Resources.getResource("core/fileB").toURI()),
                EXTRA_FILES_LAYER_EXTRACTION_PATH.resolve("fileB"))
            .build();
    emptyLayerConfiguration = LayerConfiguration.builder().build();

    cache = Cache.withDirectory(temporaryFolder.newFolder().toPath());

    Mock.Get(mockBuildConfiguration).Setup(m => m.getEventHandlers()).Returns(mockEventHandlers);

    Mock.Get(mockBuildConfiguration).Setup(m => m.getApplicationLayersCache()).Returns(cache);

  }

  private ImageLayers buildFakeLayersToCache() {
    ImageLayers.Builder applicationLayersBuilder = ImageLayers.builder();

    ImmutableArray<BuildAndCacheApplicationLayerStep> buildAndCacheApplicationLayerSteps =
        BuildAndCacheApplicationLayerStep.makeList(
            mockBuildConfiguration,
            ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer());

    foreach (BuildAndCacheApplicationLayerStep buildAndCacheApplicationLayerStep in buildAndCacheApplicationLayerSteps)

    {
      applicationLayersBuilder.add(NonBlockingSteps.get(buildAndCacheApplicationLayerStep));
    }

    return applicationLayersBuilder.build();
  }

  [Test]
  public void testRun()
      {
    ImmutableArray<LayerConfiguration> fakeLayerConfigurations =
        ImmutableArray.Create(
            fakeDependenciesLayerConfiguration,
            fakeSnapshotDependenciesLayerConfiguration,
            fakeResourcesLayerConfiguration,
            fakeClassesLayerConfiguration,
            fakeExtraFilesLayerConfiguration);
    Mock.Get(mockBuildConfiguration).Setup(m => m.getLayerConfigurations()).Returns(fakeLayerConfigurations);

    // Populates the cache.
    ImageLayers applicationLayers = buildFakeLayersToCache();
    Assert.AreEqual(5, applicationLayers.size());

    ImmutableArray<LayerEntry> dependenciesLayerEntries =
        fakeLayerConfigurations.get(0).getLayerEntries();
    ImmutableArray<LayerEntry> snapshotDependenciesLayerEntries =
        fakeLayerConfigurations.get(1).getLayerEntries();
    ImmutableArray<LayerEntry> resourcesLayerEntries =
        fakeLayerConfigurations.get(2).getLayerEntries();
    ImmutableArray<LayerEntry> classesLayerEntries =
        fakeLayerConfigurations.get(3).getLayerEntries();
    ImmutableArray<LayerEntry> extraFilesLayerEntries =
        fakeLayerConfigurations.get(4).getLayerEntries();

    CachedLayer dependenciesCachedLayer =
        cache.retrieve(dependenciesLayerEntries).orElseThrow(() => new AssertionException(""));
    CachedLayer snapshotDependenciesCachedLayer =
        cache.retrieve(snapshotDependenciesLayerEntries).orElseThrow(() => new AssertionException(""));
    CachedLayer resourcesCachedLayer =
        cache.retrieve(resourcesLayerEntries).orElseThrow(() => new AssertionException(""));
    CachedLayer classesCachedLayer =
        cache.retrieve(classesLayerEntries).orElseThrow(() => new AssertionException(""));
    CachedLayer extraFilesCachedLayer =
        cache.retrieve(extraFilesLayerEntries).orElseThrow(() => new AssertionException(""));

    // Verifies that the cached layers are up-to-date.
    Assert.AreEqual(
        applicationLayers.get(0).getBlobDescriptor().getDigest(),
        dependenciesCachedLayer.getDigest());
    Assert.AreEqual(
        applicationLayers.get(1).getBlobDescriptor().getDigest(),
        snapshotDependenciesCachedLayer.getDigest());
    Assert.AreEqual(
        applicationLayers.get(2).getBlobDescriptor().getDigest(), resourcesCachedLayer.getDigest());
    Assert.AreEqual(
        applicationLayers.get(3).getBlobDescriptor().getDigest(), classesCachedLayer.getDigest());
    Assert.AreEqual(
        applicationLayers.get(4).getBlobDescriptor().getDigest(),
        extraFilesCachedLayer.getDigest());

    // Verifies that the cache reader gets the same layers as the newest application layers.
    assertBlobsEqual(applicationLayers.get(0).getBlob(), dependenciesCachedLayer.getBlob());
    assertBlobsEqual(applicationLayers.get(1).getBlob(), snapshotDependenciesCachedLayer.getBlob());
    assertBlobsEqual(applicationLayers.get(2).getBlob(), resourcesCachedLayer.getBlob());
    assertBlobsEqual(applicationLayers.get(3).getBlob(), classesCachedLayer.getBlob());
    assertBlobsEqual(applicationLayers.get(4).getBlob(), extraFilesCachedLayer.getBlob());
  }

  [Test]
  public void testRun_emptyLayersIgnored()
      {
    ImmutableArray<LayerConfiguration> fakeLayerConfigurations =
        ImmutableArray.Create(
            fakeDependenciesLayerConfiguration,
            emptyLayerConfiguration,
            fakeResourcesLayerConfiguration,
            fakeClassesLayerConfiguration,
            emptyLayerConfiguration);
    Mock.Get(mockBuildConfiguration).Setup(m => m.getLayerConfigurations()).Returns(fakeLayerConfigurations);

    // Populates the cache.
    ImageLayers applicationLayers = buildFakeLayersToCache();
    Assert.AreEqual(3, applicationLayers.size());

    ImmutableArray<LayerEntry> dependenciesLayerEntries =
        fakeLayerConfigurations.get(0).getLayerEntries();
    ImmutableArray<LayerEntry> resourcesLayerEntries =
        fakeLayerConfigurations.get(2).getLayerEntries();
    ImmutableArray<LayerEntry> classesLayerEntries =
        fakeLayerConfigurations.get(3).getLayerEntries();

    CachedLayer dependenciesCachedLayer =
        cache.retrieve(dependenciesLayerEntries).orElseThrow(() => new AssertionException(""));
    CachedLayer resourcesCachedLayer =
        cache.retrieve(resourcesLayerEntries).orElseThrow(() => new AssertionException(""));
    CachedLayer classesCachedLayer =
        cache.retrieve(classesLayerEntries).orElseThrow(() => new AssertionException(""));

    // Verifies that the cached layers are up-to-date.
    Assert.AreEqual(
        applicationLayers.get(0).getBlobDescriptor().getDigest(),
        dependenciesCachedLayer.getDigest());
    Assert.AreEqual(
        applicationLayers.get(1).getBlobDescriptor().getDigest(), resourcesCachedLayer.getDigest());
    Assert.AreEqual(
        applicationLayers.get(2).getBlobDescriptor().getDigest(), classesCachedLayer.getDigest());

    // Verifies that the cache reader gets the same layers as the newest application layers.
    assertBlobsEqual(applicationLayers.get(0).getBlob(), dependenciesCachedLayer.getBlob());
    assertBlobsEqual(applicationLayers.get(1).getBlob(), resourcesCachedLayer.getBlob());
    assertBlobsEqual(applicationLayers.get(2).getBlob(), classesCachedLayer.getBlob());
  }
}
}
