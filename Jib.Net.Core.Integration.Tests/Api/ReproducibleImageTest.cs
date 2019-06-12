/*
 * Copyright 2019 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace com.google.cloud.tools.jib.api
{




















    /**
     * Verify that created image has explicit directory structures, default timestamps, permissions, and
     * file orderings.
     */
    public class ReproducibleImageTest
    {
        [ClassRule] public static readonly TemporaryFolder imageLocation = new TemporaryFolder();

        private static FileInfo imageTar;

        [OneTimeSetUp]
        public static void createImage()
        {
            SystemPath root = imageLocation.getRoot().toPath();
            SystemPath fileA = Files.createFile(root.resolve("fileA.txt"));
            SystemPath fileB = Files.createFile(root.resolve("fileB.txt"));
            SystemPath fileC = Files.createFile(root.resolve("fileC.txt"));
            SystemPath subdir = Files.createDirectory(root.resolve("dir"));
            SystemPath subsubdir = Files.createDirectory(subdir.resolve("subdir"));
            Files.createFile(subdir.resolve("fileD.txt"));
            Files.createFile(subsubdir.resolve("fileE.txt"));

            imageTar = new FileInfo(Path.Combine(imageLocation.getRoot().FullName, "image.tar"));
            Containerizer containerizer =
                Containerizer.to(TarImage.named("jib-core/reproducible").saveTo(imageTar.toPath()));

            Jib.fromScratch()
                .setEntrypoint("echo", "Hello World")
                .addLayer(ImmutableArray.Create(fileA), AbsoluteUnixPath.get("/app"))
                // layer with out-of-order files
                .addLayer(ImmutableArray.Create(fileC, fileB), "/app")
                .addLayer(
                    LayerConfiguration.builder()
                        .addEntryRecursive(subdir, AbsoluteUnixPath.get("/app"))
                        .build())
                .containerize(containerizer);
        }

        [Test]
        public void testTarballStructure()
        {
            // known content should produce known results
            IList<string> expected =
                ImmutableArray.Create(
                    "c46572ef74f58d95e44dd36c1fbdfebd3752e8b56a794a13c11cfed35a1a6e1c.tar.gz",
                    "6d2763b0f3940d324ea6b55386429e5b173899608abf7d1bff62e25dd2e4dcea.tar.gz",
                    "530c1954a2b087d0b989895ea56435c9dc739a973f2d2b6cb9bb98e55bbea7ac.tar.gz",
                    "config.json",
                    "manifest.json");

            IList<string> actual = new List<string>();
            using (TarInputStream input =
                new TarInputStream(Files.newInputStream(imageTar.toPath())))
            {
                TarEntry imageEntry;
                while ((imageEntry = input.getNextTarEntry()) != null)
                {
                    actual.add(imageEntry.getName());
                }
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void testManifest()
        {
            using (Stream input = Files.newInputStream(imageTar.toPath()))
            {
                const string exectedManifest =
                    "[{\"config\":\"config.json\",\"repoTags\":[\"jib-core/reproducible:latest\"],"
                        + "\"layers\":[\"c46572ef74f58d95e44dd36c1fbdfebd3752e8b56a794a13c11cfed35a1a6e1c.tar.gz\",\"6d2763b0f3940d324ea6b55386429e5b173899608abf7d1bff62e25dd2e4dcea.tar.gz\",\"530c1954a2b087d0b989895ea56435c9dc739a973f2d2b6cb9bb98e55bbea7ac.tar.gz\"]}]";
                string generatedManifest = extractFromTarFileAsString(imageTar, "manifest.json");
                Assert.AreEqual(exectedManifest, generatedManifest);
            }
        }

        [Test]
        public void testConfiguration()
        {
            using (Stream input = Files.newInputStream(imageTar.toPath()))
            {
                const string exectedConfig =
                    "{\"created\":\"1970-01-01T00:00:00Z\",\"architecture\":\"amd64\",\"os\":\"linux\","
                        + "\"config\":{\"Env\":[],\"Entrypoint\":[\"echo\",\"Hello World\"],\"ExposedPorts\":{},\"Labels\":{},\"Volumes\":{}},"
                        + "\"history\":[{\"created\":\"1970-01-01T00:00:00Z\",\"author\":\"Jib\",\"created_by\":\"jib-core:null\",\"comment\":\"\"},{\"created\":\"1970-01-01T00:00:00Z\",\"author\":\"Jib\",\"created_by\":\"jib-core:null\",\"comment\":\"\"},{\"created\":\"1970-01-01T00:00:00Z\",\"author\":\"Jib\",\"created_by\":\"jib-core:null\",\"comment\":\"\"}],"
                        + "\"rootfs\":{\"type\":\"layers\",\"diff_ids\":[\"sha256:18e4f44e6d1835bd968339b166057bd17ab7d4cbb56dc7262a5cafea7cf8d405\",\"sha256:13369c34f073f2b9c1fa6431e23d925f1a8eac65b1726c8cc8fcc2596c69b414\",\"sha256:4f92c507112d7880ca0f504ef8272b7fdee107263270125036a260a741565923\"]}}";
                string generatedConfig = extractFromTarFileAsString(imageTar, "config.json");
                Assert.AreEqual(exectedConfig, generatedConfig);
            }
        }

        [Test]
        public void testImageLayout()
        {
            ISet<string> paths = new HashSet<string>();
            layerEntriesDo(
                (layerName, layerEntry) =>
                {
                    if (layerEntry.isFile())
                    {
                        paths.add(layerEntry.getName());
                    }
                });
            Assert.AreEqual(
                ImmutableHashSet.Create(
                    "app/fileA.txt",
                    "app/fileB.txt",
                    "app/fileC.txt",
                    "app/fileD.txt",
                    "app/subdir/fileE.txt"),
                paths);
        }

        [Test]
        public void testAllFileAndDirectories()
        {
            layerEntriesDo(
                (layerName, layerEntry) =>
                    Assert.IsTrue(layerEntry.isFile() || layerEntry.isDirectory()));
        }

        [Test]
        public void testTimestampsEpochPlus1s()
        {
            layerEntriesDo(
                (layerName, layerEntry) =>
                {
                    Instant modificationTime = layerEntry.getLastModifiedDate().toInstant();
                    Assert.AreEqual(
                Instant.FromUnixTimeSeconds(1), modificationTime, layerName + ": " + layerEntry.getName());
                });
        }

        [Test]
        public void testPermissions()
        {
            Assert.AreEqual(0644, FilePermissions.DEFAULT_FILE_PERMISSIONS.getPermissionBits());
            Assert.AreEqual(0755, FilePermissions.DEFAULT_FOLDER_PERMISSIONS.getPermissionBits());
            layerEntriesDo(
                (layerName, layerEntry) =>
                {
                    if (layerEntry.isFile())
                    {
                        Assert.AreEqual(
                    0644, layerEntry.getMode() & 0777, layerName + ": " + layerEntry.getName());
                    }
                    else if (layerEntry.isDirectory())
                    {
                        Assert.AreEqual(
                    0755, layerEntry.getMode() & 0777, layerName + ": " + layerEntry.getName());
                    }
                });
        }

        [Test]
        public void testNoImplicitParentDirectories()
        {
            ISet<string> directories = new HashSet<string>();
            layerEntriesDo(
                (layerName, layerEntry) =>
                {
                    string entryPath = layerEntry.getName();
                    if (layerEntry.isDirectory())
                    {
                        Assert.IsTrue(entryPath.endsWith("/"), "directories in tar end with /");
                        entryPath = entryPath.substring(0, entryPath.length() - 1);
                    }

                    int lastSlashPosition = entryPath.lastIndexOf('/');
                    string parent = entryPath.substring(0, Math.Max(0, lastSlashPosition));
                    if (!parent.isEmpty())
                    {
                        Assert.IsTrue(directories.contains(parent),
                    "layer has implicit parent directory: " + parent);
                    }
                    if (layerEntry.isDirectory())
                    {
                        directories.add(entryPath);
                    }
                });
        }

        [Test]
        public void testFileOrdering()
        {
            Dictionary<string, List<string>> layerPaths = new Dictionary<string, List<string>>();
            layerEntriesDo((layerName, layerEntry) =>
            {
                List<string> pathsList;
                if (layerPaths.ContainsKey(layerName))
                {
                    pathsList = layerPaths[layerName];
                }
                else
                {
                    pathsList = new List<string>();
                    layerPaths[layerName] = pathsList;
                }
                pathsList.Add(layerEntry.getName());
            });
            foreach (ICollection<string> paths in layerPaths.asMap().values())
            {
                List<string> sorted = new List<string>(paths);
                // ReproducibleLayerBuilder sorts by TarArchiveEntry.getName()
                Collections.sort(sorted);
                Assert.AreEqual(sorted, (List<string>)paths, "layer files are not consistently sorted");
            }
        }

        private void layerEntriesDo(Action<string, TarEntry> layerConsumer)
        {
            using (TarInputStream input =
                new TarInputStream(Files.newInputStream(imageTar.toPath())))
            {
                TarEntry imageEntry;
                while ((imageEntry = input.getNextTarEntry()) != null)
                {
                    string imageEntryName = imageEntry.getName();
                    // assume all .tar.gz files are layers
                    if (imageEntry.isFile() && imageEntryName.endsWith(".tar.gz"))
                    {
                        TarInputStream layer = new TarInputStream(new GZipInputStream(input));
                        TarEntry layerEntry;
                        while ((layerEntry = layer.getNextTarEntry()) != null)
                        {
                            layerConsumer.accept(imageEntryName, layerEntry);
                        }
                    }
                }
            }
        }

        private static string extractFromTarFileAsString(FileInfo tarFile, string filename)
        {
            using (TarInputStream input =
                new TarInputStream(Files.newInputStream(tarFile.toPath())))
            {
                TarEntry imageEntry;
                while ((imageEntry = input.getNextTarEntry()) != null)
                {
                    if (filename.Equals(imageEntry.getName()))
                    {
                        return CharStreams.toString(new StreamReader(input, StandardCharsets.UTF_8));
                    }
                }
            }
            throw new AssertionException("file not found: " + filename);
        }
    }
}
