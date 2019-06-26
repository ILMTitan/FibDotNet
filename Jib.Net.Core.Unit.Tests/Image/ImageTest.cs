/*
 * Copyright 2017 Google LLC.
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
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images;
using Moq;
using NodaTime;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.image
{
    /** Tests for {@link Image}. */
    public class ImageTest
    {
        private readonly ILayer mockLayer = Mock.Of<ILayer>();
        private readonly DescriptorDigest mockDescriptorDigest = DescriptorDigest.fromHash(new string('a', 64));

        [SetUp]
        public void setUp()
        {
            Mock.Get(mockLayer).Setup(m => m.getBlobDescriptor()).Returns(new BlobDescriptor(mockDescriptorDigest));
        }

        [Test]
        public void test_smokeTest()
        {
            Image image =
                Image.builder(ManifestFormat.V22)
                    .setCreated(Instant.FromUnixTimeSeconds(10000))
                    .addEnvironmentVariable("crepecake", "is great")
                    .addEnvironmentVariable("VARIABLE", "VALUE")
                    .setEntrypoint(Arrays.asList("some", "command"))
                    .setProgramArguments(Arrays.asList("arg1", "arg2"))
                    .addExposedPorts(ImmutableHashSet.Create(Port.tcp(1000), Port.tcp(2000)))
                    .addVolumes(
                        ImmutableHashSet.Create(
                            AbsoluteUnixPath.get("/a/path"), AbsoluteUnixPath.get("/another/path")))
                    .setUser("john")
                    .addLayer(mockLayer)
                    .build();

            Assert.AreEqual(ManifestFormat.V22, image.getImageFormat());
            Assert.AreEqual(
                mockDescriptorDigest, image.getLayers().get(0).getBlobDescriptor().getDigest());
            Assert.AreEqual(Instant.FromUnixTimeSeconds(10000), image.getCreated());
            Assert.AreEqual(
                ImmutableDic.of("crepecake", "is great", "VARIABLE", "VALUE"), image.getEnvironment());
            Assert.AreEqual(Arrays.asList("some", "command"), image.getEntrypoint());
            Assert.AreEqual(Arrays.asList("arg1", "arg2"), image.getProgramArguments());
            Assert.AreEqual(ImmutableHashSet.Create(Port.tcp(1000), Port.tcp(2000)), image.getExposedPorts());
            Assert.AreEqual(
                ImmutableHashSet.Create(AbsoluteUnixPath.get("/a/path"), AbsoluteUnixPath.get("/another/path")),
                image.getVolumes());
            Assert.AreEqual("john", image.getUser());
        }

        [Test]
        public void testDefaults()
        {
            Image image = Image.builder(ManifestFormat.V22).build();
            Assert.AreEqual("amd64", image.getArchitecture());
            Assert.AreEqual("linux", image.getOs());
            Assert.AreEqual(new List<ILayer>(), image.getLayers());
            Assert.AreEqual(new List<HistoryEntry>(), image.getHistory());
        }

        [Test]
        public void testOsArch()
        {
            Image image =
                Image.builder(ManifestFormat.V22).setArchitecture("wasm").setOs("js").build();
            Assert.AreEqual("wasm", image.getArchitecture());
            Assert.AreEqual("js", image.getOs());
        }
    }
}
