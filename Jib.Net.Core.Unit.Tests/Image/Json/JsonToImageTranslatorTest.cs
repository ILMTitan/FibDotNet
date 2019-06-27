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
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images;
using Jib.Net.Core.Images.Json;
using Jib.Net.Test.Common;
using NodaTime;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace com.google.cloud.tools.jib.image.json
{
    /** Tests for {@link JsonToImageTranslator}. */
    public class JsonToImageTranslatorTest
    {
        [Test]
        public void TestToImage_v21()
        {
            // Loads the JSON string.
            SystemPath jsonFile =
                Paths.Get(TestResources.GetResource("core/json/v21manifest.json").ToURI());

            // Deserializes into a manifest JSON object.
            V21ManifestTemplate manifestTemplate =
                JsonTemplateMapper.ReadJsonFromFile<V21ManifestTemplate>(jsonFile);

            Image image = JsonToImageTranslator.ToImage(manifestTemplate);

            IList<ILayer> layers = image.GetLayers();
            Assert.AreEqual(2, layers.Count);
            Assert.AreEqual(
                DescriptorDigest.FromDigest(
                    "sha256:5bd451067f9ab05e97cda8476c82f86d9b69c2dffb60a8ad2fe3723942544ab3"),
                layers[0].GetBlobDescriptor().GetDigest());
            Assert.AreEqual(
                DescriptorDigest.FromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                layers[1].GetBlobDescriptor().GetDigest());
        }

        [Test]
        public void TestToImage_v22()
        {
            TestToImage_buildable<V22ManifestTemplate>("core/json/v22manifest.json");
        }

        [Test]
        public void TestToImage_oci()
        {
            TestToImage_buildable<OCIManifestTemplate>("core/json/ocimanifest.json");
        }

        [Test]
        public void TestPortMapToList()
        {
            ImmutableSortedDictionary<string, IDictionary<object, object>> input =
                new Dictionary<string, IDictionary<object, object>>
                {
                    ["1000"] = ImmutableDictionary.Create<object, object>(),
                    ["2000/tcp"] =
                    ImmutableDictionary.Create<object, object>(),
                    ["3000/udp"] =
                    ImmutableDictionary.Create<object, object>()
                }.ToImmutableSortedDictionary();
            ImmutableHashSet<Port> expected = ImmutableHashSet.Create(Port.Tcp(1000), Port.Tcp(2000), Port.Udp(3000));
            Assert.AreEqual(expected, JsonToImageTranslator.PortMapToSet(input));

            ImmutableArray<IDictionary<string, IDictionary<object, object>>> badInputs =
                ImmutableArray.Create<IDictionary<string, IDictionary<object, object>>>(
                    ImmutableDic.Of<string, IDictionary<object, object>>("abc", ImmutableDictionary.Create<object, object>()),
                    ImmutableDic.Of<string, IDictionary<object, object>>("1000-2000", ImmutableDictionary.Create<object, object>()),
                    ImmutableDic.Of<string, IDictionary<object, object>>("/udp", ImmutableDictionary.Create<object, object>()),
                    ImmutableDic.Of<string, IDictionary<object, object>>("123/xxx", ImmutableDictionary.Create<object, object>()));
            foreach (IDictionary<string, IDictionary<object, object>> badInput in badInputs)
            {
                try
                {
                    JsonToImageTranslator.PortMapToSet(badInput);
                    Assert.Fail();
                }
                catch (BadContainerConfigurationFormatException)
                {
                }
            }
        }

        [Test]
        public void TestVolumeMapToList()
        {
            ImmutableSortedDictionary<string, IDictionary<object, object>> input =
                        new Dictionary<string, IDictionary<object, object>>
                        {
                            ["/var/job-result-data"] = ImmutableDictionary.Create<object, object>(),
                            ["/var/log/my-app-logs"] = ImmutableDictionary.Create<object, object>()
                        }.ToImmutableSortedDictionary();
            ImmutableHashSet<AbsoluteUnixPath> expected =
                ImmutableHashSet.Create(
                    AbsoluteUnixPath.Get("/var/job-result-data"),
                    AbsoluteUnixPath.Get("/var/log/my-app-logs"));
            Assert.AreEqual(expected, JsonToImageTranslator.VolumeMapToSet(input));

            ImmutableArray<IDictionary<string, IDictionary<object, object>>> badInputs =
                ImmutableArray.Create<IDictionary<string, IDictionary<object, object>>>(
                    ImmutableDic.Of<string, IDictionary<object, object>>("var/job-result-data", ImmutableDictionary.Create<object, object>()),
                    ImmutableDic.Of<string, IDictionary<object, object>>("log", ImmutableDictionary.Create<object, object>()),
                    ImmutableDic.Of<string, IDictionary<object, object>>("C:/udp", ImmutableDictionary.Create<object, object>()));
            foreach (IDictionary<string, IDictionary<object, object>> badInput in badInputs)
            {
                try
                {
                    JsonToImageTranslator.VolumeMapToSet(badInput);
                    Assert.Fail();
                }
                catch (BadContainerConfigurationFormatException)
                {
                }
            }
        }

        [Test]
        public void TestJsonToImageTranslatorRegex()
        {
            AssertGoodEnvironmentPattern("NAME=VALUE", "NAME", "VALUE");
            AssertGoodEnvironmentPattern("A1203921=www=ww", "A1203921", "www=ww");
            AssertGoodEnvironmentPattern("&*%(&#$(*@(%&@$*$(=", "&*%(&#$(*@(%&@$*$(", "");
            AssertGoodEnvironmentPattern("m_a_8943=100", "m_a_8943", "100");
            AssertGoodEnvironmentPattern("A_B_C_D=*****", "A_B_C_D", "*****");

            AssertBadEnvironmentPattern("=================");
            AssertBadEnvironmentPattern("A_B_C");
        }

        private void AssertGoodEnvironmentPattern(
            string input, string expectedName, string expectedValue)
        {
            Match matcher = JsonToImageTranslator.EnvironmentPattern.Match(input);
            Assert.IsTrue(matcher.Success);
            Assert.AreEqual(expectedName, matcher.Groups["name"].Value);
            Assert.AreEqual(expectedValue, matcher.Groups["value"].Value);
        }

        private void AssertBadEnvironmentPattern(string input)
        {
            Match matcher = JsonToImageTranslator.EnvironmentPattern.Match(input);
            Assert.IsFalse(matcher.Success);
        }

        private void TestToImage_buildable<T>(
            string jsonFilename) where T : IBuildableManifestTemplate
        {
            // Loads the container configuration JSON.
            SystemPath containerConfigurationJsonFile =
                Paths.Get(
                    TestResources.GetResource("core/json/containerconfig.json").ToURI());
            ContainerConfigurationTemplate containerConfigurationTemplate =
                JsonTemplateMapper.ReadJsonFromFile<ContainerConfigurationTemplate>(
                    containerConfigurationJsonFile);

            // Loads the manifest JSON.
            SystemPath manifestJsonFile =
                Paths.Get(TestResources.GetResource(jsonFilename).ToURI());
            T manifestTemplate =
                JsonTemplateMapper.ReadJsonFromFile<T>(manifestJsonFile);

            Image image = JsonToImageTranslator.ToImage(manifestTemplate, containerConfigurationTemplate);

            IList<ILayer> layers = image.GetLayers();
            Assert.AreEqual(1, layers.Count);
            Assert.AreEqual(
                new BlobDescriptor(
                    1000000,
                    DescriptorDigest.FromDigest(
                        "sha256:4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236")),
                layers[0].GetBlobDescriptor());
            Assert.AreEqual(
                DescriptorDigest.FromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                layers[0].GetDiffId());
            CollectionAssert.AreEqual(
                ImmutableArray.Create(
                    HistoryEntry.CreateBuilder()
                        .SetCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                        .SetAuthor("Bazel")
                        .SetCreatedBy("bazel build ...")
                        .SetEmptyLayer(true)
                        .Build(),
                    HistoryEntry.CreateBuilder()
                        .SetCreationTimestamp(Instant.FromUnixTimeSeconds(20))
                        .SetAuthor("Jib")
                        .SetCreatedBy("jib")
                        .Build()),
                image.GetHistory());
            Assert.AreEqual(Instant.FromUnixTimeSeconds(20), image.GetCreated());
            Assert.AreEqual(new []{"some", "entrypoint", "command"}, image.GetEntrypoint());
            Assert.AreEqual(ImmutableDic.Of("VAR1", "VAL1", "VAR2", "VAL2"), image.GetEnvironment());
            Assert.AreEqual("/some/workspace", image.GetWorkingDirectory());
            Assert.AreEqual(
                ImmutableHashSet.Create(Port.Tcp(1000), Port.Tcp(2000), Port.Udp(3000)), image.GetExposedPorts());
            Assert.AreEqual(
                ImmutableHashSet.Create(
                    AbsoluteUnixPath.Get("/var/job-result-data"),
                    AbsoluteUnixPath.Get("/var/log/my-app-logs")),
                image.GetVolumes());
            Assert.AreEqual("tomcat", image.GetUser());
            Assert.AreEqual("value1", image.GetLabels()["key1"]);
            Assert.AreEqual("value2", image.GetLabels()["key2"]);
            Assert.AreEqual(2, image.GetLabels().Count);
        }
    }
}
