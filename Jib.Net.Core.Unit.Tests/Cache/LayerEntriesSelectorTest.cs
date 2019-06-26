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
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using static com.google.cloud.tools.jib.cache.LayerEntriesSelector;

namespace com.google.cloud.tools.jib.cache
{
    /** Tests for {@link LayerEntriesSelector}. */
    public class LayerEntriesSelectorTest : IDisposable
    {
        private static LayerEntry defaultLayerEntry(SystemPath source, AbsoluteUnixPath destination)
        {
            return new LayerEntry(
                source,
                destination,
                LayerConfiguration.DefaultFilePermissionsProvider.apply(source, destination),
                LayerConfiguration.DefaultModifiedTime);
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();
        private ImmutableArray<LayerEntry> outOfOrderLayerEntries;
        private ImmutableArray<LayerEntry> inOrderLayerEntries;

        private static ImmutableArray<LayerEntryTemplate> toLayerEntryTemplates(
            ImmutableArray<LayerEntry> layerEntries)
        {
            ImmutableArray<LayerEntryTemplate>.Builder builder = ImmutableArray.CreateBuilder<LayerEntryTemplate>();
            foreach (LayerEntry layerEntry in layerEntries)
            {
                builder.add(new LayerEntryTemplate(layerEntry));
            }
            return builder.build();
        }

        [SetUp]
        public void setUp()
        {
            SystemPath folder = temporaryFolder.newFolder().toPath();
            SystemPath file1 = Files.createDirectory(folder.Resolve("files"));
            SystemPath file2 = Files.createFile(folder.Resolve("files").Resolve("two"));
            SystemPath file3 = Files.createFile(folder.Resolve("gile"));

            LayerEntry testLayerEntry1 = defaultLayerEntry(file1, AbsoluteUnixPath.get("/extraction/path"));
            LayerEntry testLayerEntry2 = defaultLayerEntry(file2, AbsoluteUnixPath.get("/extraction/path"));
            LayerEntry testLayerEntry3 = defaultLayerEntry(file3, AbsoluteUnixPath.get("/extraction/path"));
            LayerEntry testLayerEntry4 =
                new LayerEntry(
                    file3,
                    AbsoluteUnixPath.get("/extraction/path"),
                    FilePermissions.fromOctalString("755"),
                    LayerConfiguration.DefaultModifiedTime);
            LayerEntry testLayerEntry5 =
                defaultLayerEntry(file3, AbsoluteUnixPath.get("/extraction/patha"));
            LayerEntry testLayerEntry6 =
                new LayerEntry(
                    file3,
                    AbsoluteUnixPath.get("/extraction/patha"),
                    FilePermissions.fromOctalString("755"),
                    LayerConfiguration.DefaultModifiedTime);

            outOfOrderLayerEntries =
                ImmutableArray.Create(
                    testLayerEntry4,
                    testLayerEntry2,
                    testLayerEntry6,
                    testLayerEntry3,
                    testLayerEntry1,
                    testLayerEntry5);
            inOrderLayerEntries =
                ImmutableArray.Create(
                    testLayerEntry1,
                    testLayerEntry2,
                    testLayerEntry3,
                    testLayerEntry4,
                    testLayerEntry5,
                    testLayerEntry6);
        }

        public void Dispose()
        {
            temporaryFolder.Dispose();
        }

        [Test]
        public void testLayerEntryTemplate_compareTo()
        {
            CollectionAssert.AreEqual(
                toLayerEntryTemplates(inOrderLayerEntries),
                ImmutableArray.CreateRange(toLayerEntryTemplates(outOfOrderLayerEntries).sorted()));
        }

        [Test]
        public void testToSortedJsonTemplates()
        {
            Assert.AreEqual(
                toLayerEntryTemplates(inOrderLayerEntries),
                LayerEntriesSelector.toSortedJsonTemplates(outOfOrderLayerEntries));
        }

        [Test]
        public async Task testGenerateSelector_emptyAsync()
        {
            DescriptorDigest expectedSelector =
                await Digests.computeJsonDigestAsync(ImmutableArray.Create<object>()).ConfigureAwait(false);
            Assert.AreEqual(
                expectedSelector, await LayerEntriesSelector.generateSelectorAsync(ImmutableArray.Create<LayerEntry>()).ConfigureAwait(false));
        }

        [Test]
        public async Task testGenerateSelectorAsync()
        {
            DescriptorDigest expectedSelector =
                await Digests.computeJsonDigestAsync(toLayerEntryTemplates(inOrderLayerEntries)).ConfigureAwait(false);
            Assert.AreEqual(
                expectedSelector, await LayerEntriesSelector.generateSelectorAsync(outOfOrderLayerEntries).ConfigureAwait(false));
        }

        [Test]
        public async Task testGenerateSelector_fileModifiedAsync()
        {
            SystemPath layerFile = temporaryFolder.newFolder("testFolder").toPath().Resolve("file");
            Files.write(layerFile, "hello".getBytes(Encoding.UTF8));
            Files.setLastModifiedTime(layerFile, FileTime.from(Instant.FromUnixTimeSeconds(0)));
            LayerEntry layerEntry = defaultLayerEntry(layerFile, AbsoluteUnixPath.get("/extraction/path"));
            DescriptorDigest expectedSelector =
                await LayerEntriesSelector.generateSelectorAsync(ImmutableArray.Create(layerEntry)).ConfigureAwait(false);

            // Verify that changing modified time generates a different selector
            Files.setLastModifiedTime(layerFile, FileTime.from(Instant.FromUnixTimeSeconds(1)));
            Assert.AreNotEqual(
                expectedSelector, await LayerEntriesSelector.generateSelectorAsync(ImmutableArray.Create(layerEntry)).ConfigureAwait(false));

            // Verify that changing modified time back generates same selector
            Files.setLastModifiedTime(layerFile, FileTime.from(Instant.FromUnixTimeSeconds(0)));
            Assert.AreEqual(
                expectedSelector, 
                await LayerEntriesSelector.generateSelectorAsync(ImmutableArray.Create(layerEntry)).ConfigureAwait(false));
        }

        [Test]
        public void testGenerateSelector_permissionsModified()
        {
            SystemPath layerFile = temporaryFolder.newFolder("testFolder").toPath().Resolve("file");
            Files.write(layerFile, "hello".getBytes(Encoding.UTF8));
            LayerEntry layerEntry111 =
                new LayerEntry(
                    layerFile,
                    AbsoluteUnixPath.get("/extraction/path"),
                    FilePermissions.fromOctalString("111"),
                    LayerConfiguration.DefaultModifiedTime);
            LayerEntry layerEntry222 =
                new LayerEntry(
                    layerFile,
                    AbsoluteUnixPath.get("/extraction/path"),
                    FilePermissions.fromOctalString("222"),
                    LayerConfiguration.DefaultModifiedTime);

            // Verify that changing permissions generates a different selector
            Assert.AreNotEqual(
                LayerEntriesSelector.generateSelectorAsync(ImmutableArray.Create(layerEntry111)),
                LayerEntriesSelector.generateSelectorAsync(ImmutableArray.Create(layerEntry222)));
        }
    }
}
