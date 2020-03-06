// Copyright 2017 Google LLC.
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

using Fib.Net.Core.Api;
using Fib.Net.Core.Blob;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Hash;
using Fib.Net.Core.Images;
using Fib.Net.Core.Images.Json;
using Fib.Net.Core.Json;
using Fib.Net.Test.Common;
using NodaTime;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace Fib.Net.Core.Unit.Tests.Images.Json
{
    /** Tests for {@link ImageToJsonTranslator}. */
    public class ImageToJsonTranslatorTest
    {
        private ImageToJsonTranslator imageToJsonTranslator;
        //private static DescriptorDigest fakeDigest = DescriptorDigest.fromHash(new string('a', 64));

        private void SetUp(ManifestFormat imageFormat)
        {
            Image.Builder testImageBuilder =
                Image.CreateBuilder(imageFormat)
                    .SetCreated(Instant.FromUnixTimeSeconds(20))
                    .SetArchitecture("wasm")
                    .SetOs("js")
                    .AddEnvironmentVariable("VAR1", "VAL1")
                    .AddEnvironmentVariable("VAR2", "VAL2")
                    .SetEntrypoint(new[] { "some", "entrypoint", "command" })
                    .SetProgramArguments(new[] { "arg1", "arg2" })
                    .SetHealthCheck(
                        DockerHealthCheck.FromCommand(ImmutableArray.Create("CMD-SHELL", "/checkhealth"))
                            .SetInterval(Duration.FromSeconds(3))
                            .SetTimeout(Duration.FromSeconds(1))
                            .SetStartPeriod(Duration.FromSeconds(2))
                            .SetRetries(3)
                            .Build())
                    .AddExposedPorts(ImmutableHashSet.Create(Port.Tcp(1000), Port.Tcp(2000), Port.Udp(3000)))
                    .AddVolumes(
                        ImmutableHashSet.Create(
                            AbsoluteUnixPath.Get("/var/job-result-data"),
                            AbsoluteUnixPath.Get("/var/log/my-app-logs")))
                    .AddLabels(ImmutableDic.Of("key1", "value1", "key2", "value2"))
                    .SetWorkingDirectory("/some/workspace")
                    .SetUser("tomcat");

            DescriptorDigest fakeDigest =
                DescriptorDigest.FromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad");
            testImageBuilder.AddLayer(
                new FakeLayer(fakeDigest));
            testImageBuilder.AddHistory(
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .SetAuthor("Bazel")
                    .SetCreatedBy("bazel build ...")
                    .SetEmptyLayer(true)
                    .Build());
            testImageBuilder.AddHistory(
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(20))
                    .SetAuthor("Fib")
                    .SetCreatedBy("fib")
                    .Build());
            imageToJsonTranslator = new ImageToJsonTranslator(testImageBuilder.Build());
        }

        private class FakeLayer : ILayer
        {
            private readonly DescriptorDigest fakeDigest;

            public FakeLayer(DescriptorDigest fakeDigest)
            {
                this.fakeDigest = fakeDigest;
            }

            public IBlob GetBlob()
            {
                return Blobs.From("ignored");
            }

            public BlobDescriptor GetBlobDescriptor()
            {
                return new BlobDescriptor(1000, fakeDigest);
            }

            public DescriptorDigest GetDiffId()
            {
                return fakeDigest;
            }
        }

        [Test]
        public void TestGetContainerConfiguration()
        {
            SetUp(ManifestFormat.V22);

            // Loads the expected JSON string.
            SystemPath jsonFile = Paths.Get(TestResources.GetResource("core/json/containerconfig.json").ToURI());
            string expectedJson = Encoding.UTF8.GetString(Files.ReadAllBytes(jsonFile));

            // Translates the image to the container configuration and writes the JSON string.
            ContainerConfigurationTemplate containerConfiguration = imageToJsonTranslator.GetContainerConfiguration();

            Assert.AreEqual(expectedJson, JsonTemplateMapper.ToUtf8String(containerConfiguration));
        }

        [Test]
        public async Task TestGetManifest_v22Async()
        {
            SetUp(ManifestFormat.V22);
            await TestGetManifestAsync(ManifestFormat.V22, "core/json/translated_v22manifest.json").ConfigureAwait(false);
        }

        [Test]
        public async Task TestGetManifest_ociAsync()
        {
            SetUp(ManifestFormat.OCI);
            await TestGetManifestAsync(ManifestFormat.OCI, "core/json/translated_ocimanifest.json").ConfigureAwait(false);
        }

        [Test]
        public void TestPortListToMap()
        {
            ImmutableHashSet<Port> input = ImmutableHashSet.Create(Port.Tcp(1000), Port.Udp(2000));
            ImmutableSortedDictionary<string, IDictionary<object, object>> expected =
                new Dictionary<string, IDictionary<object, object>>
                {
                    ["1000/tcp"] = ImmutableDictionary.Create<object, object>(),
                    ["2000/udp"] = ImmutableDictionary.Create<object, object>()
                }.ToImmutableSortedDictionary();
            Assert.AreEqual(expected, ImageToJsonTranslator.PortSetToMap(input));
        }

        [Test]
        public void TestVolumeListToMap()
        {
            ImmutableHashSet<AbsoluteUnixPath> input =
                ImmutableHashSet.Create(
                    AbsoluteUnixPath.Get("/var/job-result-data"),
                    AbsoluteUnixPath.Get("/var/log/my-app-logs"));
            ImmutableSortedDictionary<string, IDictionary<object, object>> expected =
                new Dictionary<string, IDictionary<object, object>>
                {
                    ["/var/job-result-data"] = ImmutableDictionary.Create<object, object>(),
                    ["/var/log/my-app-logs"] = ImmutableDictionary.Create<object, object>()
                }.ToImmutableSortedDictionary();
            Assert.AreEqual(expected, ImageToJsonTranslator.VolumesSetToMap(input));
        }

        [Test]
        public void TestEnvironmentMapToList()
        {
            ImmutableDictionary<string, string> input = ImmutableDic.Of("NAME1", "VALUE1", "NAME2", "VALUE2");
            ImmutableArray<string> expected = ImmutableArray.Create("NAME1=VALUE1", "NAME2=VALUE2");
            CollectionAssert.AreEqual(expected, ImageToJsonTranslator.EnvironmentMapToList(input));
        }

        /** Tests translation of image to {@link BuildableManifestTemplate}. */
        private async Task TestGetManifestAsync(
            ManifestFormat manifestTemplateClass, string translatedJsonFilename)
        {
            // Loads the expected JSON string.
            SystemPath jsonFile = Paths.Get(TestResources.GetResource(translatedJsonFilename).ToURI());
            string expectedJson = Encoding.UTF8.GetString(Files.ReadAllBytes(jsonFile));

            // Translates the image to the manifest and writes the JSON string.
            ContainerConfigurationTemplate containerConfiguration = imageToJsonTranslator.GetContainerConfiguration();
            BlobDescriptor blobDescriptor = await Digests.ComputeJsonDescriptorAsync(containerConfiguration).ConfigureAwait(false);
            IBuildableManifestTemplate manifestTemplate =
                imageToJsonTranslator.GetManifestTemplate(manifestTemplateClass, blobDescriptor);

            Assert.AreEqual(expectedJson, JsonTemplateMapper.ToUtf8String(manifestTemplate));
        }
    }
}
