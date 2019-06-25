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
        private static LayerEntry defaultLayerEntry(SystemPath source, AbsoluteUnixPath destination)
        {
            return new LayerEntry(
                source,
                destination,
                LayerConfiguration.DefaultFilePermissionsProvider.apply(source, destination),
                LayerConfiguration.DefaultModifiedTime);
        }

        [Test]
        public void testAddEntryRecursive_defaults()
        {
            SystemPath testDirectory = Paths.get(TestResources.getResource("core/layer").toURI()).toAbsolutePath();
            SystemPath testFile = Paths.get(TestResources.getResource("core/fileA").toURI());

            ILayerConfiguration layerConfiguration =
                LayerConfiguration.builder()
                    .addEntryRecursive(testDirectory, AbsoluteUnixPath.get("/app/layer/"))
                    .addEntryRecursive(testFile, AbsoluteUnixPath.get("/app/fileA"))
                    .build();

            ImmutableHashSet<LayerEntry> expectedLayerEntries =
                ImmutableHashSet.Create(
                    defaultLayerEntry(testDirectory, AbsoluteUnixPath.get("/app/layer/")),
                    defaultLayerEntry(testDirectory.resolve("a"), AbsoluteUnixPath.get("/app/layer/a/")),
                    defaultLayerEntry(
                        testDirectory.resolve("a/b"), AbsoluteUnixPath.get("/app/layer/a/b/")),
                    defaultLayerEntry(
                        testDirectory.resolve("a/b/bar"), AbsoluteUnixPath.get("/app/layer/a/b/bar/")),
                    defaultLayerEntry(testDirectory.resolve("c/"), AbsoluteUnixPath.get("/app/layer/c")),
                    defaultLayerEntry(
                        testDirectory.resolve("c/cat/"), AbsoluteUnixPath.get("/app/layer/c/cat")),
                    defaultLayerEntry(testDirectory.resolve("foo"), AbsoluteUnixPath.get("/app/layer/foo")),
                    defaultLayerEntry(testFile, AbsoluteUnixPath.get("/app/fileA")));

            CollectionAssert.AreEquivalent(
                expectedLayerEntries, ImmutableHashSet.CreateRange(layerConfiguration.getLayerEntries()));
        }

        [Test]
        public void testAddEntryRecursive_permissionsAndTimestamps()
        {
            SystemPath testDirectory = Paths.get(TestResources.getResource("core/layer").toURI()).toAbsolutePath();
            SystemPath testFile = Paths.get(TestResources.getResource("core/fileA").toURI());

            FilePermissions permissions1 = FilePermissions.fromOctalString("111");
            FilePermissions permissions2 = FilePermissions.fromOctalString("777");
            Instant timestamp1 = Instant.FromUnixTimeSeconds(123);
            Instant timestamp2 = Instant.FromUnixTimeSeconds(987);

            FilePermissions permissionsProvider(SystemPath _, AbsoluteUnixPath destination) =>
                    destination.toString().startsWith("/app/layer/a") ? permissions1 : permissions2;
            Instant timestampProvider(SystemPath _, AbsoluteUnixPath destination) =>
                    destination.toString().startsWith("/app/layer/a") ? timestamp1 : timestamp2;

            ILayerConfiguration layerConfiguration =
                LayerConfiguration.builder()
                    .addEntryRecursive(
                        testDirectory,
                        AbsoluteUnixPath.get("/app/layer/"),
                        permissionsProvider,
                        timestampProvider)
                    .addEntryRecursive(
                        testFile,
                        AbsoluteUnixPath.get("/app/fileA"),
                        permissionsProvider,
                        timestampProvider)
                    .build();

            ImmutableHashSet<LayerEntry> expectedLayerEntries =
                ImmutableHashSet.Create(
                    new LayerEntry(
                        testDirectory, AbsoluteUnixPath.get("/app/layer/"), permissions2, timestamp2),
                    new LayerEntry(
                        testDirectory.resolve("a"),
                        AbsoluteUnixPath.get("/app/layer/a/"),
                        permissions1,
                        timestamp1),
                    new LayerEntry(
                        testDirectory.resolve("a/b"),
                        AbsoluteUnixPath.get("/app/layer/a/b/"),
                        permissions1,
                        timestamp1),
                    new LayerEntry(
                        testDirectory.resolve("a/b/bar"),
                        AbsoluteUnixPath.get("/app/layer/a/b/bar/"),
                        permissions1,
                        timestamp1),
                    new LayerEntry(
                        testDirectory.resolve("c/"),
                        AbsoluteUnixPath.get("/app/layer/c"),
                        permissions2,
                        timestamp2),
                    new LayerEntry(
                        testDirectory.resolve("c/cat/"),
                        AbsoluteUnixPath.get("/app/layer/c/cat"),
                        permissions2,
                        timestamp2),
                    new LayerEntry(
                        testDirectory.resolve("foo"),
                        AbsoluteUnixPath.get("/app/layer/foo"),
                        permissions2,
                        timestamp2),
                    new LayerEntry(testFile, AbsoluteUnixPath.get("/app/fileA"), permissions2, timestamp2));

            CollectionAssert.AreEquivalent(
                expectedLayerEntries, ImmutableHashSet.CreateRange(layerConfiguration.getLayerEntries()));
        }
    }
}