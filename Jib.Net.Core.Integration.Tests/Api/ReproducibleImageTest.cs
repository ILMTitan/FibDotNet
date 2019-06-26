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
using Jib.Net.Core;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.api
{
    /**
     * Verify that created image has explicit directory structures, default timestamps, permissions, and
     * file orderings.
     */
    public class ReproducibleImageTest
    {
        public static readonly TemporaryFolder imageLocation = new TemporaryFolder();

        private static FileInfo imageTar;

        [OneTimeSetUp]
        public static async Task createImageAsync()
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

            await Jib.fromScratch()
                .setEntrypoint("echo", "Hello World")
                .addLayer(ImmutableArray.Create(fileA), AbsoluteUnixPath.get("/app"))
                // layer with out-of-order files
                .addLayer(ImmutableArray.Create(fileC, fileB), "/app")
                .addLayer(
                    LayerConfiguration.builder()
                        .addEntryRecursive(subdir, AbsoluteUnixPath.get("/app"))
                        .build())
                .containerizeAsync(containerizer).ConfigureAwait(false);
        }

        [OneTimeTearDown]
        public static void OneTimeTearDown()
        {
            imageLocation.Dispose();
        }

        [Test]
        public void testTarballStructure()
        {
            // known content should produce known results
            IList<string> expected =
                ImmutableArray.Create(
                    "d92b71b2286ff6c6f6fa33761a6fd4758208750c37856d5452d2e8588660f979.tar.gz",
                    "41ca669ea78da47d94469b16018242f6f6f5fac94c931cfc1a32fd66f33f65fe.tar.gz",
                    "905f254d440aeb07a4ed7b3d0713b6a005e122aef1b0d46bae6859158b3697cf.tar.gz",
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

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void testManifest()
        {
            using (Stream input = Files.newInputStream(imageTar.toPath()))
            {
                const string exectedManifest =
                    "[{" +
                        "\"config\":\"config.json\"," +
                        "\"repoTags\":[\"jib-core/reproducible:latest\"]," +
                        "\"layers\":[" +
                            "\"d92b71b2286ff6c6f6fa33761a6fd4758208750c37856d5452d2e8588660f979.tar.gz\"," +
                            "\"41ca669ea78da47d94469b16018242f6f6f5fac94c931cfc1a32fd66f33f65fe.tar.gz\"," +
                            "\"905f254d440aeb07a4ed7b3d0713b6a005e122aef1b0d46bae6859158b3697cf.tar.gz\"" +
                        "]" +
                    "}]";
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
                    "{" +
                        "\"created\":\"1970-01-01T00:00:00Z\"," +
                        "\"architecture\":\"amd64\"," +
                        "\"os\":\"linux\","+
                        "\"config\":{" +
                            "\"Env\":[]," +
                            "\"Entrypoint\":[" +
                                "\"echo\"," +
                                "\"Hello World\"" +
                            "]," +
                            "\"ExposedPorts\":{}," +
                            "\"Labels\":{}," +
                            "\"Volumes\":{}" +
                        "}," +
                        "\"history\":[" +
                            "{" +
                                "\"created\":\"1970-01-01T00:00:00Z\"," +
                                "\"author\":\"Jib\"," +
                                "\"created_by\":\"jib-core:null\"," +
                                "\"comment\":\"\"" +
                            "}," +
                            "{" +
                                "\"created\":\"1970-01-01T00:00:00Z\"," +
                                "\"author\":\"Jib\"," +
                                "\"created_by\":\"jib-core:null\"," +
                                "\"comment\":\"\"" +
                            "}," +
                            "{" +
                                "\"created\":\"1970-01-01T00:00:00Z\"," +
                                "\"author\":\"Jib\"," +
                                "\"created_by\":\"jib-core:null\"," +
                                "\"comment\":\"\"" +
                            "}" +
                        "]," +
                        "\"rootfs\":{" +
                            "\"type\":\"layers\"," +
                            "\"diff_ids\":[" +
                                "\"sha256:5be38b9a5bd93ad25adec1ced93c1d4051436246e815a0e7c917c872622b98c4\"," +
                                "\"sha256:4972099d16725fc66efd2a0e64637a4d8d43e74ef3cc26257484780180f5721d\"," +
                                "\"sha256:76ee9817f30cdfbe18d84c3b045ab223a0df85bb47020bfed304ecfa8510d077\"" +
                            "]" +
                        "}" +
                    "}";
                string generatedConfig = extractFromTarFileAsString(imageTar, "config.json");
                Assert.AreEqual(exectedConfig, generatedConfig);
            }
        }

        [Test]
        public void testImageLayout()
        {
            ISet<string> paths = new HashSet<string>();
            layerEntriesDo(
                (_, layerEntry) =>
                {
                    if (layerEntry.isFile())
                    {
                        paths.add(layerEntry.getName());
                    }
                });
            CollectionAssert.AreEquivalent(
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
                (_, layerEntry) =>
                    Assert.IsTrue(layerEntry.isFile() || layerEntry.isDirectory()));
        }

        [Test]
        public void testTimestampsEpochPlus1s()
        {
            layerEntriesDo(
                (layerName, layerEntry) =>
                {
                    Instant modificationTime = DateTime.SpecifyKind(layerEntry.getLastModifiedDate(), DateTimeKind.Utc).toInstant();
                    Assert.AreEqual(
                Instant.FromUnixTimeSeconds(1), modificationTime, layerName + ": " + layerEntry.getName());
                });
        }

        [Test]
        public void testPermissions()
        {
            Assert.AreEqual(FilePermissions.fromOctalString("644"), FilePermissions.DefaultFilePermissions);
            Assert.AreEqual(FilePermissions.fromOctalString("755"), FilePermissions.DefaultFolderPermissions);
            layerEntriesDo(
                (layerName, layerEntry) =>
                {
                    if (layerEntry.isFile())
                    {
                        const PosixFilePermissions expectedFilePermissions = PosixFilePermissions.OwnerRead
                            | PosixFilePermissions.OwnerWrite
                            | PosixFilePermissions.GroupRead
                            | PosixFilePermissions.OthersRead;
                        Assert.AreEqual(
                            expectedFilePermissions,
                            layerEntry.getMode() & PosixFilePermissions.All,
                            layerName + ": " + layerEntry.getName());
                    }
                    else if (layerEntry.isDirectory())
                    {
                        const PosixFilePermissions expectedDirectoryPermissions = PosixFilePermissions.OwnerAll
                            | PosixFilePermissions.GroupReadExecute
                            | PosixFilePermissions.OthersReadExecute;
                        Assert.AreEqual(
                            expectedDirectoryPermissions,
                            layerEntry.getMode() & PosixFilePermissions.All,
                            layerName + ": " + layerEntry.getName());
                    }
                });
        }

        [Test]
        public void testNoImplicitParentDirectories()
        {
            ISet<string> directories = new HashSet<string>();
            layerEntriesDo(
                (_, layerEntry) =>
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
                sorted.Sort();
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
                        TarInputStream layer = new TarInputStream(new GZipStream(input, CompressionMode.Decompress));
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
                    if (filename == imageEntry.getName())
                    {
                        return CharStreams.toString(new StreamReader(input, Encoding.UTF8));
                    }
                }
            }
            throw new AssertionException("file not found: " + filename);
        }
    }
}
