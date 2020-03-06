// Copyright 2019 Google Inc.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using ICSharpCode.SharpZipLib.Tar;
using Fib.Net.Core.Api;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Tar;
using Fib.Net.Test.Common;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Fib.Net.Core.Integration.Tests.Api
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
        public static async Task CreateImageAsync()
        {
            SystemPath root = imageLocation.GetRoot().ToPath();
            SystemPath fileA = Files.CreateFile(root.Resolve("fileA.txt"));
            SystemPath fileB = Files.CreateFile(root.Resolve("fileB.txt"));
            SystemPath fileC = Files.CreateFile(root.Resolve("fileC.txt"));
            SystemPath subdir = Files.CreateDirectory(root.Resolve("dir"));
            SystemPath subsubdir = Files.CreateDirectory(subdir.Resolve("subdir"));
            Files.CreateFile(subdir.Resolve("fileD.txt"));
            Files.CreateFile(subsubdir.Resolve("fileE.txt"));

            imageTar = new FileInfo(Path.Combine(imageLocation.GetRoot().FullName, "image.tar"));
            Containerizer containerizer =
                Containerizer.To(TarImage.Named("fibdotnet-core/reproducible").SaveTo(imageTar.ToPath()));

            await FibContainerBuilder.FromScratch()
                .SetEntrypoint("echo", "Hello World")
                .AddLayer(ImmutableArray.Create(fileA), AbsoluteUnixPath.Get("/app"))
                // layer with out-of-order files
                .AddLayer(ImmutableArray.Create(fileC, fileB), "/app")
                .AddLayer(
                    LayerConfiguration.CreateBuilder()
                        .AddEntryRecursive(subdir, AbsoluteUnixPath.Get("/app"))
                        .Build())
                .ContainerizeAsync(containerizer).ConfigureAwait(false);
        }

        [OneTimeTearDown]
        public static void OneTimeTearDown()
        {
            imageLocation.Dispose();
        }

        [Test]
        public void TestTarballStructure()
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
                new TarInputStream(Files.NewInputStream(imageTar.ToPath())))
            {
                TarEntry imageEntry;
                while ((imageEntry = input.GetNextEntry()) != null)
                {
                    actual.Add(imageEntry.Name);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TestManifest()
        {
            using (Stream input = Files.NewInputStream(imageTar.ToPath()))
            {
                const string exectedManifest =
                    "[{" +
                        "\"config\":\"config.json\"," +
                        "\"repoTags\":[\"fibdotnet-core/reproducible:latest\"]," +
                        "\"layers\":[" +
                            "\"d92b71b2286ff6c6f6fa33761a6fd4758208750c37856d5452d2e8588660f979.tar.gz\"," +
                            "\"41ca669ea78da47d94469b16018242f6f6f5fac94c931cfc1a32fd66f33f65fe.tar.gz\"," +
                            "\"905f254d440aeb07a4ed7b3d0713b6a005e122aef1b0d46bae6859158b3697cf.tar.gz\"" +
                        "]" +
                    "}]";
                string generatedManifest = ExtractFromTarFileAsString(imageTar, "manifest.json");
                Assert.AreEqual(exectedManifest, generatedManifest);
            }
        }

        [Test]
        public void TestConfiguration()
        {
            using (Stream input = Files.NewInputStream(imageTar.ToPath()))
            {
                const string exectedConfig =
                    "{" +
                        "\"created\":\"1970-01-01T00:00:00Z\"," +
                        "\"architecture\":\"amd64\"," +
                        "\"os\":\"linux\"," +
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
                                "\"author\":\"Fib\"," +
                                "\"created_by\":\"fibdotnet-core:null\"," +
                                "\"comment\":\"\"" +
                            "}," +
                            "{" +
                                "\"created\":\"1970-01-01T00:00:00Z\"," +
                                "\"author\":\"Fib\"," +
                                "\"created_by\":\"fibdotnet-core:null\"," +
                                "\"comment\":\"\"" +
                            "}," +
                            "{" +
                                "\"created\":\"1970-01-01T00:00:00Z\"," +
                                "\"author\":\"Fib\"," +
                                "\"created_by\":\"fibdotnet-core:null\"," +
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
                string generatedConfig = ExtractFromTarFileAsString(imageTar, "config.json");
                Assert.AreEqual(exectedConfig, generatedConfig);
            }
        }

        [Test]
        public void TestImageLayout()
        {
            ISet<string> paths = new HashSet<string>();
            LayerEntriesDo(
                (_, layerEntry) =>
                {
                    if (layerEntry.IsFile())
                    {
                        paths.Add(layerEntry.Name);
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
        public void TestAllFileAndDirectories()
        {
            LayerEntriesDo(
                (_, layerEntry) =>
                    Assert.IsTrue(layerEntry.IsFile() || layerEntry.IsDirectory));
        }

        [Test]
        public void TestTimestampsEpochPlus1s()
        {
            LayerEntriesDo(
                (layerName, layerEntry) =>
                {
                    Instant modificationTime = Instant.FromDateTimeUtc(DateTime.SpecifyKind(layerEntry.TarHeader.ModTime, DateTimeKind.Utc));
                    Assert.AreEqual(
                Instant.FromUnixTimeSeconds(1), modificationTime, layerName + ": " + layerEntry.Name);
                });
        }

        [Test]
        public void TestPermissions()
        {
            Assert.AreEqual(FilePermissions.FromOctalString("644"), FilePermissions.DefaultFilePermissions);
            Assert.AreEqual(FilePermissions.FromOctalString("755"), FilePermissions.DefaultFolderPermissions);
            LayerEntriesDo(
                (layerName, layerEntry) =>
                {
                    if (layerEntry.IsFile())
                    {
                        const PosixFilePermissions expectedFilePermissions = PosixFilePermissions.OwnerRead
                            | PosixFilePermissions.OwnerWrite
                            | PosixFilePermissions.GroupRead
                            | PosixFilePermissions.OthersRead;
                        Assert.AreEqual(
                            expectedFilePermissions,
                            layerEntry.GetMode() & PosixFilePermissions.All,
                            layerName + ": " + layerEntry.Name);
                    }
                    else if (layerEntry.IsDirectory)
                    {
                        const PosixFilePermissions expectedDirectoryPermissions = PosixFilePermissions.OwnerAll
                            | PosixFilePermissions.GroupReadExecute
                            | PosixFilePermissions.OthersReadExecute;
                        Assert.AreEqual(
                            expectedDirectoryPermissions,
                            layerEntry.GetMode() & PosixFilePermissions.All,
                            layerName + ": " + layerEntry.Name);
                    }
                });
        }

        [Test]
        public void TestNoImplicitParentDirectories()
        {
            ISet<string> directories = new HashSet<string>();
            LayerEntriesDo(
                (_, layerEntry) =>
                {
                    string entryPath = layerEntry.Name;
                    if (layerEntry.IsDirectory)
                    {
                        Assert.That(entryPath, Does.EndWith("/"), "directories in tar end with /");
                        entryPath = entryPath.Substring(0, entryPath.Length - 1);
                    }

                    int lastSlashPosition = entryPath.LastIndexOf('/');
                    string parent = entryPath.Substring(0, Math.Max(0, lastSlashPosition));
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Assert.That(directories, Does.Contain(parent), "layer has implicit parent directory: " + parent);
                    }
                    if (layerEntry.IsDirectory)
                    {
                        directories.Add(entryPath);
                    }
                });
        }

        [Test]
        public void TestFileOrdering()
        {
            Dictionary<string, List<string>> layerPaths = new Dictionary<string, List<string>>();
            LayerEntriesDo((layerName, layerEntry) =>
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
                pathsList.Add(layerEntry.Name);
            });
            foreach (ICollection<string> paths in layerPaths.Values)
            {
                List<string> sorted = new List<string>(paths);
                // ReproducibleLayerBuilder sorts by TarArchiveEntry.getName()
                sorted.Sort();
                Assert.AreEqual(sorted, (List<string>)paths, "layer files are not consistently sorted");
            }
        }

        private void LayerEntriesDo(Action<string, TarEntry> layerConsumer)
        {
            using (TarInputStream input =
                new TarInputStream(Files.NewInputStream(imageTar.ToPath())))
            {
                TarEntry imageEntry;
                while ((imageEntry = input.GetNextEntry()) != null)
                {
                    string imageEntryName = imageEntry.Name;
                    // assume all .tar.gz files are layers
                    if (imageEntry.IsFile() && imageEntryName.EndsWith(".tar.gz", StringComparison.Ordinal))
                    {
                        TarInputStream layer = new TarInputStream(new GZipStream(input, CompressionMode.Decompress));
                        TarEntry layerEntry;
                        while ((layerEntry = layer.GetNextEntry()) != null)
                        {
                            layerConsumer(imageEntryName, layerEntry);
                        }
                    }
                }
            }
        }

        private static string ExtractFromTarFileAsString(FileInfo tarFile, string filename)
        {
            using (TarInputStream input =
                new TarInputStream(Files.NewInputStream(tarFile.ToPath())))
            {
                TarEntry imageEntry;
                while ((imageEntry = input.GetNextEntry()) != null)
                {
                    if (filename == imageEntry.Name)
                    {
                        return new StreamReader(input).ReadToEnd();
                    }
                }
            }
            throw new AssertionException("file not found: " + filename);
        }
    }
}
