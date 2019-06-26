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
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Test.Common;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link LayerConfiguration}. */

    public class LayerConfigurationTest
    {
        private static LayerEntry DefaultLayerEntry(SystemPath source, AbsoluteUnixPath destination)
        {
            return new LayerEntry(
                source,
                destination,
                LayerConfiguration.DefaultFilePermissionsProvider.Apply(source, destination),
                LayerConfiguration.DefaultModifiedTime);
        }

        [Test]
        public void TestAddEntryRecursive_defaults()
        {
            SystemPath testDirectory = TestResources.GetResource("core/layer");
            SystemPath testFile = TestResources.GetResource("core/fileA");

            ILayerConfiguration layerConfiguration =
                LayerConfiguration.CreateBuilder()
                    .AddEntryRecursive(testDirectory, AbsoluteUnixPath.Get("/app/layer/"))
                    .AddEntryRecursive(testFile, AbsoluteUnixPath.Get("/app/fileA"))
                    .Build();

            ImmutableHashSet<LayerEntry> expectedLayerEntries =
                ImmutableHashSet.Create(
                    DefaultLayerEntry(testDirectory, AbsoluteUnixPath.Get("/app/layer/")),
                    DefaultLayerEntry(testDirectory.Resolve("a"), AbsoluteUnixPath.Get("/app/layer/a/")),
                    DefaultLayerEntry(
                        testDirectory.Resolve("a/b"), AbsoluteUnixPath.Get("/app/layer/a/b/")),
                    DefaultLayerEntry(
                        testDirectory.Resolve("a/b/bar"), AbsoluteUnixPath.Get("/app/layer/a/b/bar/")),
                    DefaultLayerEntry(testDirectory.Resolve("c/"), AbsoluteUnixPath.Get("/app/layer/c")),
                    DefaultLayerEntry(
                        testDirectory.Resolve("c/cat/"), AbsoluteUnixPath.Get("/app/layer/c/cat")),
                    DefaultLayerEntry(testDirectory.Resolve("foo"), AbsoluteUnixPath.Get("/app/layer/foo")),
                    DefaultLayerEntry(testFile, AbsoluteUnixPath.Get("/app/fileA")));

            CollectionAssert.AreEquivalent(
                expectedLayerEntries, ImmutableHashSet.CreateRange(layerConfiguration.GetLayerEntries()));
        }

        [Test]
        public void TestAddEntryRecursive_permissionsAndTimestamps()
        {
            SystemPath testDirectory = TestResources.GetResource("core/layer");
            SystemPath testFile = TestResources.GetResource("core/fileA");

            FilePermissions permissions1 = FilePermissions.FromOctalString("111");
            FilePermissions permissions2 = FilePermissions.FromOctalString("777");
            Instant timestamp1 = Instant.FromUnixTimeSeconds(123);
            Instant timestamp2 = Instant.FromUnixTimeSeconds(987);

            FilePermissions permissionsProvider(SystemPath _, AbsoluteUnixPath destination) =>
                    JavaExtensions.StartsWith(destination.ToString(), "/app/layer/a") ? permissions1 : permissions2;
            Instant timestampProvider(SystemPath _, AbsoluteUnixPath destination) =>
                    JavaExtensions.StartsWith(destination.ToString(), "/app/layer/a") ? timestamp1 : timestamp2;

            ILayerConfiguration layerConfiguration =
                LayerConfiguration.CreateBuilder()
                    .AddEntryRecursive(
                        testDirectory,
                        AbsoluteUnixPath.Get("/app/layer/"),
                        permissionsProvider,
                        timestampProvider)
                    .AddEntryRecursive(
                        testFile,
                        AbsoluteUnixPath.Get("/app/fileA"),
                        permissionsProvider,
                        timestampProvider)
                    .Build();

            ImmutableHashSet<LayerEntry> expectedLayerEntries =
                ImmutableHashSet.Create(
                    new LayerEntry(
                        testDirectory, AbsoluteUnixPath.Get("/app/layer/"), permissions2, timestamp2),
                    new LayerEntry(
                        testDirectory.Resolve("a"),
                        AbsoluteUnixPath.Get("/app/layer/a/"),
                        permissions1,
                        timestamp1),
                    new LayerEntry(
                        testDirectory.Resolve("a/b"),
                        AbsoluteUnixPath.Get("/app/layer/a/b/"),
                        permissions1,
                        timestamp1),
                    new LayerEntry(
                        testDirectory.Resolve("a/b/bar"),
                        AbsoluteUnixPath.Get("/app/layer/a/b/bar/"),
                        permissions1,
                        timestamp1),
                    new LayerEntry(
                        testDirectory.Resolve("c/"),
                        AbsoluteUnixPath.Get("/app/layer/c"),
                        permissions2,
                        timestamp2),
                    new LayerEntry(
                        testDirectory.Resolve("c/cat/"),
                        AbsoluteUnixPath.Get("/app/layer/c/cat"),
                        permissions2,
                        timestamp2),
                    new LayerEntry(
                        testDirectory.Resolve("foo"),
                        AbsoluteUnixPath.Get("/app/layer/foo"),
                        permissions2,
                        timestamp2),
                    new LayerEntry(testFile, AbsoluteUnixPath.Get("/app/fileA"), permissions2, timestamp2));

            CollectionAssert.AreEquivalent(
                expectedLayerEntries, ImmutableHashSet.CreateRange(layerConfiguration.GetLayerEntries()));
        }
    }
}