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

namespace com.google.cloud.tools.jib.cache {




















/** Tests for {@link LayerEntriesSelector}. */
public class LayerEntriesSelectorTest {

  private static LayerEntry defaultLayerEntry(Path source, AbsoluteUnixPath destination) {
    return new LayerEntry(
        source,
        destination,
        LayerConfiguration.DEFAULT_FILE_PERMISSIONS_PROVIDER.apply(source, destination),
        LayerConfiguration.DEFAULT_MODIFIED_TIME);
  }

  [Rule] public final TemporaryFolder temporaryFolder = new TemporaryFolder();
  private ImmutableList<LayerEntry> outOfOrderLayerEntries;
  private ImmutableList<LayerEntry> inOrderLayerEntries;

  private static ImmutableList<LayerEntryTemplate> toLayerEntryTemplates(
      ImmutableList<LayerEntry> layerEntries) {
    ImmutableList.Builder<LayerEntryTemplate> builder = ImmutableList.builder();
    foreach (LayerEntry layerEntry in layerEntries)
    {
      builder.add(new LayerEntryTemplate(layerEntry));
    }
    return builder.build();
  }

  [TestInitialize]
  public void setUp() {
    Path folder = temporaryFolder.newFolder().toPath();
    Path file1 = Files.createDirectory(folder.resolve("files"));
    Path file2 = Files.createFile(folder.resolve("files").resolve("two"));
    Path file3 = Files.createFile(folder.resolve("gile"));

    LayerEntry testLayerEntry1 = defaultLayerEntry(file1, AbsoluteUnixPath.get("/extraction/path"));
    LayerEntry testLayerEntry2 = defaultLayerEntry(file2, AbsoluteUnixPath.get("/extraction/path"));
    LayerEntry testLayerEntry3 = defaultLayerEntry(file3, AbsoluteUnixPath.get("/extraction/path"));
    LayerEntry testLayerEntry4 =
        new LayerEntry(
            file3,
            AbsoluteUnixPath.get("/extraction/path"),
            FilePermissions.fromOctalString("755"),
            LayerConfiguration.DEFAULT_MODIFIED_TIME);
    LayerEntry testLayerEntry5 =
        defaultLayerEntry(file3, AbsoluteUnixPath.get("/extraction/patha"));
    LayerEntry testLayerEntry6 =
        new LayerEntry(
            file3,
            AbsoluteUnixPath.get("/extraction/patha"),
            FilePermissions.fromOctalString("755"),
            LayerConfiguration.DEFAULT_MODIFIED_TIME);

    outOfOrderLayerEntries =
        ImmutableList.of(
            testLayerEntry4,
            testLayerEntry2,
            testLayerEntry6,
            testLayerEntry3,
            testLayerEntry1,
            testLayerEntry5);
    inOrderLayerEntries =
        ImmutableList.of(
            testLayerEntry1,
            testLayerEntry2,
            testLayerEntry3,
            testLayerEntry4,
            testLayerEntry5,
            testLayerEntry6);
  }

  [TestMethod]
  public void testLayerEntryTemplate_compareTo() {
    Assert.assertEquals(
        toLayerEntryTemplates(inOrderLayerEntries),
        ImmutableList.sortedCopyOf(toLayerEntryTemplates(outOfOrderLayerEntries)));
  }

  [TestMethod]
  public void testToSortedJsonTemplates() {
    Assert.assertEquals(
        toLayerEntryTemplates(inOrderLayerEntries),
        LayerEntriesSelector.toSortedJsonTemplates(outOfOrderLayerEntries));
  }

  [TestMethod]
  public void testGenerateSelector_empty() {
    DescriptorDigest expectedSelector = Digests.computeJsonDigest(ImmutableList.of());
    Assert.assertEquals(
        expectedSelector, LayerEntriesSelector.generateSelector(ImmutableList.of()));
  }

  [TestMethod]
  public void testGenerateSelector() {
    DescriptorDigest expectedSelector =
        Digests.computeJsonDigest(toLayerEntryTemplates(inOrderLayerEntries));
    Assert.assertEquals(
        expectedSelector, LayerEntriesSelector.generateSelector(outOfOrderLayerEntries));
  }

  [TestMethod]
  public void testGenerateSelector_fileModified() {
    Path layerFile = temporaryFolder.newFolder("testFolder").toPath().resolve("file");
    Files.write(layerFile, "hello".getBytes(StandardCharsets.UTF_8));
    Files.setLastModifiedTime(layerFile, FileTime.from(Instant.EPOCH));
    LayerEntry layerEntry = defaultLayerEntry(layerFile, AbsoluteUnixPath.get("/extraction/path"));
    DescriptorDigest expectedSelector =
        LayerEntriesSelector.generateSelector(ImmutableList.of(layerEntry));

    // Verify that changing modified time generates a different selector
    Files.setLastModifiedTime(layerFile, FileTime.from(Instant.ofEpochSecond(1)));
    Assert.assertNotEquals(
        expectedSelector, LayerEntriesSelector.generateSelector(ImmutableList.of(layerEntry)));

    // Verify that changing modified time back generates same selector
    Files.setLastModifiedTime(layerFile, FileTime.from(Instant.EPOCH));
    Assert.assertEquals(
        expectedSelector, LayerEntriesSelector.generateSelector(ImmutableList.of(layerEntry)));
  }

  [TestMethod]
  public void testGenerateSelector_permissionsModified() {
    Path layerFile = temporaryFolder.newFolder("testFolder").toPath().resolve("file");
    Files.write(layerFile, "hello".getBytes(StandardCharsets.UTF_8));
    LayerEntry layerEntry111 =
        new LayerEntry(
            layerFile,
            AbsoluteUnixPath.get("/extraction/path"),
            FilePermissions.fromOctalString("111"),
            LayerConfiguration.DEFAULT_MODIFIED_TIME);
    LayerEntry layerEntry222 =
        new LayerEntry(
            layerFile,
            AbsoluteUnixPath.get("/extraction/path"),
            FilePermissions.fromOctalString("222"),
            LayerConfiguration.DEFAULT_MODIFIED_TIME);

    // Verify that changing permissions generates a different selector
    Assert.assertNotEquals(
        LayerEntriesSelector.generateSelector(ImmutableList.of(layerEntry111)),
        LayerEntriesSelector.generateSelector(ImmutableList.of(layerEntry222)));
  }
}
}
