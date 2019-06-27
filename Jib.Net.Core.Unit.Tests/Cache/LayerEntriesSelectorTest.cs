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
using com.google.cloud.tools.jib.hash;
using Jib.Net.Core.Api;
using Jib.Net.Core.Caching;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Test.Common;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Jib.Net.Core.Caching.LayerEntriesSelector;

namespace com.google.cloud.tools.jib.cache
{
    /** Tests for {@link LayerEntriesSelector}. */
    public class LayerEntriesSelectorTest : IDisposable
    {
        private static LayerEntry DefaultLayerEntry(SystemPath source, AbsoluteUnixPath destination)
        {
            return new LayerEntry(
                source,
                destination,
                LayerConfiguration.DefaultFilePermissionsProvider.Apply(source, destination),
                LayerConfiguration.DefaultModifiedTime);
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();
        private ImmutableArray<LayerEntry> outOfOrderLayerEntries;
        private ImmutableArray<LayerEntry> inOrderLayerEntries;

        private static ImmutableArray<LayerEntryTemplate> ToLayerEntryTemplates(
            ImmutableArray<LayerEntry> layerEntries)
        {
            ImmutableArray<LayerEntryTemplate>.Builder builder = ImmutableArray.CreateBuilder<LayerEntryTemplate>();
            foreach (LayerEntry layerEntry in layerEntries)
            {
                builder.Add(new LayerEntryTemplate(layerEntry));
            }
            return builder.ToImmutable();
        }

        [SetUp]
        public void SetUp()
        {
            SystemPath folder = temporaryFolder.NewFolder().ToPath();
            SystemPath file1 = Files.CreateDirectory(folder.Resolve("files"));
            SystemPath file2 = Files.CreateFile(folder.Resolve("files").Resolve("two"));
            SystemPath file3 = Files.CreateFile(folder.Resolve("gile"));

            LayerEntry testLayerEntry1 = DefaultLayerEntry(file1, AbsoluteUnixPath.Get("/extraction/path"));
            LayerEntry testLayerEntry2 = DefaultLayerEntry(file2, AbsoluteUnixPath.Get("/extraction/path"));
            LayerEntry testLayerEntry3 = DefaultLayerEntry(file3, AbsoluteUnixPath.Get("/extraction/path"));
            LayerEntry testLayerEntry4 =
                new LayerEntry(
                    file3,
                    AbsoluteUnixPath.Get("/extraction/path"),
                    FilePermissions.FromOctalString("755"),
                    LayerConfiguration.DefaultModifiedTime);
            LayerEntry testLayerEntry5 =
                DefaultLayerEntry(file3, AbsoluteUnixPath.Get("/extraction/patha"));
            LayerEntry testLayerEntry6 =
                new LayerEntry(
                    file3,
                    AbsoluteUnixPath.Get("/extraction/patha"),
                    FilePermissions.FromOctalString("755"),
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
        public void TestLayerEntryTemplate_compareTo()
        {
            CollectionAssert.AreEqual(
                ToLayerEntryTemplates(inOrderLayerEntries),
                ImmutableArray.CreateRange(ToLayerEntryTemplates(outOfOrderLayerEntries).OrderBy(i => i)));
        }

        [Test]
        public void TestToSortedJsonTemplates()
        {
            Assert.AreEqual(
                ToLayerEntryTemplates(inOrderLayerEntries),
                LayerEntriesSelector.ToSortedJsonTemplates(outOfOrderLayerEntries));
        }

        [Test]
        public async Task TestGenerateSelector_emptyAsync()
        {
            DescriptorDigest expectedSelector =
                await Digests.ComputeJsonDigestAsync(ImmutableArray.Create<object>()).ConfigureAwait(false);
            Assert.AreEqual(
                expectedSelector, await LayerEntriesSelector.GenerateSelectorAsync(ImmutableArray.Create<LayerEntry>()).ConfigureAwait(false));
        }

        [Test]
        public async Task TestGenerateSelectorAsync()
        {
            DescriptorDigest expectedSelector =
                await Digests.ComputeJsonDigestAsync(ToLayerEntryTemplates(inOrderLayerEntries)).ConfigureAwait(false);
            Assert.AreEqual(
                expectedSelector, await LayerEntriesSelector.GenerateSelectorAsync(outOfOrderLayerEntries).ConfigureAwait(false));
        }

        [Test]
        public async Task TestGenerateSelector_fileModifiedAsync()
        {
            SystemPath layerFile = temporaryFolder.NewFolder("testFolder").ToPath().Resolve("file");
            Files.Write(layerFile, Encoding.UTF8.GetBytes("hello"));
            Files.SetLastModifiedTime(layerFile, FileTime.From(Instant.FromUnixTimeSeconds(0)));
            LayerEntry layerEntry = DefaultLayerEntry(layerFile, AbsoluteUnixPath.Get("/extraction/path"));
            DescriptorDigest expectedSelector =
                await LayerEntriesSelector.GenerateSelectorAsync(ImmutableArray.Create(layerEntry)).ConfigureAwait(false);

            // Verify that changing modified time generates a different selector
            Files.SetLastModifiedTime(layerFile, FileTime.From(Instant.FromUnixTimeSeconds(1)));
            Assert.AreNotEqual(
                expectedSelector, await LayerEntriesSelector.GenerateSelectorAsync(ImmutableArray.Create(layerEntry)).ConfigureAwait(false));

            // Verify that changing modified time back generates same selector
            Files.SetLastModifiedTime(layerFile, FileTime.From(Instant.FromUnixTimeSeconds(0)));
            Assert.AreEqual(
                expectedSelector, 
                await LayerEntriesSelector.GenerateSelectorAsync(ImmutableArray.Create(layerEntry)).ConfigureAwait(false));
        }

        [Test]
        public void TestGenerateSelector_permissionsModified()
        {
            SystemPath layerFile = temporaryFolder.NewFolder("testFolder").ToPath().Resolve("file");
            Files.Write(layerFile, Encoding.UTF8.GetBytes("hello"));
            LayerEntry layerEntry111 =
                new LayerEntry(
                    layerFile,
                    AbsoluteUnixPath.Get("/extraction/path"),
                    FilePermissions.FromOctalString("111"),
                    LayerConfiguration.DefaultModifiedTime);
            LayerEntry layerEntry222 =
                new LayerEntry(
                    layerFile,
                    AbsoluteUnixPath.Get("/extraction/path"),
                    FilePermissions.FromOctalString("222"),
                    LayerConfiguration.DefaultModifiedTime);

            // Verify that changing permissions generates a different selector
            Assert.AreNotEqual(
                LayerEntriesSelector.GenerateSelectorAsync(ImmutableArray.Create(layerEntry111)),
                LayerEntriesSelector.GenerateSelectorAsync(ImmutableArray.Create(layerEntry222)));
        }
    }
}
