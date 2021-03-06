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
using Fib.Net.Core.Images;
using Fib.Net.Core.Images.Json;
using Fib.Net.Test.Common;
using Moq;
using NodaTime;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fib.Net.Core.Unit.Tests.Images
{
    /** Tests for {@link Image}. */
    public class ImageTest
    {
        private readonly ILayer mockLayer = Mock.Of<ILayer>();
        private readonly DescriptorDigest mockDescriptorDigest = DescriptorDigest.FromHash(new string('a', 64));

        [SetUp]
        public void SetUp()
        {
            Mock.Get(mockLayer).Setup(m => m.GetBlobDescriptor()).Returns(new BlobDescriptor(mockDescriptorDigest));
        }

        [Test]
        public void Test_smokeTest()
        {
            Image image =
                Image.CreateBuilder(ManifestFormat.V22)
                    .SetCreated(Instant.FromUnixTimeSeconds(10000))
                    .AddEnvironmentVariable("crepecake", "is great")
                    .AddEnvironmentVariable("VARIABLE", "VALUE")
                    .SetEntrypoint(new[] { "some", "command" })
                    .SetProgramArguments(new[] { "arg1", "arg2" })
                    .AddExposedPorts(ImmutableHashSet.Create(Port.Tcp(1000), Port.Tcp(2000)))
                    .AddVolumes(
                        ImmutableHashSet.Create(
                            AbsoluteUnixPath.Get("/a/path"), AbsoluteUnixPath.Get("/another/path")))
                    .SetUser("john")
                    .AddLayer(mockLayer)
                    .Build();

            Assert.AreEqual(ManifestFormat.V22, image.GetImageFormat());
            Assert.AreEqual(
                mockDescriptorDigest, image.GetLayers()[0].GetBlobDescriptor().GetDigest());
            Assert.AreEqual(Instant.FromUnixTimeSeconds(10000), image.GetCreated());
            Assert.AreEqual(
                ImmutableDic.Of("crepecake", "is great", "VARIABLE", "VALUE"), image.GetEnvironment());
            Assert.AreEqual(new[] { "some", "command" }, image.GetEntrypoint());
            Assert.AreEqual(new[] { "arg1", "arg2" }, image.GetProgramArguments());
            Assert.AreEqual(ImmutableHashSet.Create(Port.Tcp(1000), Port.Tcp(2000)), image.GetExposedPorts());
            Assert.AreEqual(
                ImmutableHashSet.Create(AbsoluteUnixPath.Get("/a/path"), AbsoluteUnixPath.Get("/another/path")),
                image.GetVolumes());
            Assert.AreEqual("john", image.GetUser());
        }

        [Test]
        public void TestDefaults()
        {
            Image image = Image.CreateBuilder(ManifestFormat.V22).Build();
            Assert.AreEqual("amd64", image.GetArchitecture());
            Assert.AreEqual("linux", image.GetOs());
            Assert.AreEqual(new List<ILayer>(), image.GetLayers());
            Assert.AreEqual(new List<HistoryEntry>(), image.GetHistory());
        }

        [Test]
        public void TestOsArch()
        {
            Image image =
                Image.CreateBuilder(ManifestFormat.V22).SetArchitecture("wasm").SetOs("js").Build();
            Assert.AreEqual("wasm", image.GetArchitecture());
            Assert.AreEqual("js", image.GetOs());
        }
    }
}
