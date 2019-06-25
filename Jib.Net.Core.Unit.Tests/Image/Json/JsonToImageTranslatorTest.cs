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
        public void testToImage_v21()
        {
            // Loads the JSON string.
            SystemPath jsonFile =
                Paths.get(TestResources.getResource("core/json/v21manifest.json").toURI());

            // Deserializes into a manifest JSON object.
            V21ManifestTemplate manifestTemplate =
                JsonTemplateMapper.readJsonFromFile<V21ManifestTemplate>(jsonFile);

            Image image = JsonToImageTranslator.toImage(manifestTemplate);

            IList<ILayer> layers = image.getLayers();
            Assert.AreEqual(2, layers.size());
            Assert.AreEqual(
                DescriptorDigest.fromDigest(
                    "sha256:5bd451067f9ab05e97cda8476c82f86d9b69c2dffb60a8ad2fe3723942544ab3"),
                layers.get(0).getBlobDescriptor().getDigest());
            Assert.AreEqual(
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                layers.get(1).getBlobDescriptor().getDigest());
        }

        [Test]
        public void testToImage_v22()
        {
            testToImage_buildable<V22ManifestTemplate>("core/json/v22manifest.json");
        }

        [Test]
        public void testToImage_oci()
        {
            testToImage_buildable<OCIManifestTemplate>("core/json/ocimanifest.json");
        }

        [Test]
        public void testPortMapToList()
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
            ImmutableHashSet<Port> expected = ImmutableHashSet.Create(Port.tcp(1000), Port.tcp(2000), Port.udp(3000));
            Assert.AreEqual(expected, JsonToImageTranslator.portMapToSet(input));

            ImmutableArray<IDictionary<string, IDictionary<object, object>>> badInputs =
                ImmutableArray.Create<IDictionary<string, IDictionary<object, object>>>(
                    ImmutableDic.of<string, IDictionary<object, object>>("abc", ImmutableDictionary.Create<object, object>()),
                    ImmutableDic.of<string, IDictionary<object, object>>("1000-2000", ImmutableDictionary.Create<object, object>()),
                    ImmutableDic.of<string, IDictionary<object, object>>("/udp", ImmutableDictionary.Create<object, object>()),
                    ImmutableDic.of<string, IDictionary<object, object>>("123/xxx", ImmutableDictionary.Create<object, object>()));
            foreach (IDictionary<string, IDictionary<object, object>> badInput in badInputs)
            {
                try
                {
                    JsonToImageTranslator.portMapToSet(badInput);
                    Assert.Fail();
                }
                catch (BadContainerConfigurationFormatException)
                {
                }
            }
        }

        [Test]
        public void testVolumeMapToList()
        {
            ImmutableSortedDictionary<string, IDictionary<object, object>> input =
                        new Dictionary<string, IDictionary<object, object>>
                        {
                            ["/var/job-result-data"] = ImmutableDictionary.Create<object, object>(),
                            ["/var/log/my-app-logs"] = ImmutableDictionary.Create<object, object>()
                        }.ToImmutableSortedDictionary();
            ImmutableHashSet<AbsoluteUnixPath> expected =
                ImmutableHashSet.Create(
                    AbsoluteUnixPath.get("/var/job-result-data"),
                    AbsoluteUnixPath.get("/var/log/my-app-logs"));
            Assert.AreEqual(expected, JsonToImageTranslator.volumeMapToSet(input));

            ImmutableArray<IDictionary<string, IDictionary<object, object>>> badInputs =
                ImmutableArray.Create<IDictionary<string, IDictionary<object, object>>>(
                    ImmutableDic.of<string, IDictionary<object, object>>("var/job-result-data", ImmutableDictionary.Create<object, object>()),
                    ImmutableDic.of<string, IDictionary<object, object>>("log", ImmutableDictionary.Create<object, object>()),
                    ImmutableDic.of<string, IDictionary<object, object>>("C:/udp", ImmutableDictionary.Create<object, object>()));
            foreach (IDictionary<string, IDictionary<object, object>> badInput in badInputs)
            {
                try
                {
                    JsonToImageTranslator.volumeMapToSet(badInput);
                    Assert.Fail();
                }
                catch (BadContainerConfigurationFormatException)
                {
                }
            }
        }

        [Test]
        public void testJsonToImageTranslatorRegex()
        {
            assertGoodEnvironmentPattern("NAME=VALUE", "NAME", "VALUE");
            assertGoodEnvironmentPattern("A1203921=www=ww", "A1203921", "www=ww");
            assertGoodEnvironmentPattern("&*%(&#$(*@(%&@$*$(=", "&*%(&#$(*@(%&@$*$(", "");
            assertGoodEnvironmentPattern("m_a_8943=100", "m_a_8943", "100");
            assertGoodEnvironmentPattern("A_B_C_D=*****", "A_B_C_D", "*****");

            assertBadEnvironmentPattern("=================");
            assertBadEnvironmentPattern("A_B_C");
        }

        private void assertGoodEnvironmentPattern(
            string input, string expectedName, string expectedValue)
        {
            Match matcher = JsonToImageTranslator.EnvironmentPattern.matcher(input);
            Assert.IsTrue(matcher.matches());
            Assert.AreEqual(expectedName, matcher.group("name"));
            Assert.AreEqual(expectedValue, matcher.group("value"));
        }

        private void assertBadEnvironmentPattern(string input)
        {
            Match matcher = JsonToImageTranslator.EnvironmentPattern.matcher(input);
            Assert.IsFalse(matcher.matches());
        }

        private void testToImage_buildable<T>(
            string jsonFilename) where T : IBuildableManifestTemplate
        {
            // Loads the container configuration JSON.
            SystemPath containerConfigurationJsonFile =
                Paths.get(
                    TestResources.getResource("core/json/containerconfig.json").toURI());
            ContainerConfigurationTemplate containerConfigurationTemplate =
                JsonTemplateMapper.readJsonFromFile<ContainerConfigurationTemplate>(
                    containerConfigurationJsonFile);

            // Loads the manifest JSON.
            SystemPath manifestJsonFile =
                Paths.get(TestResources.getResource(jsonFilename).toURI());
            T manifestTemplate =
                JsonTemplateMapper.readJsonFromFile<T>(manifestJsonFile);

            Image image = JsonToImageTranslator.toImage(manifestTemplate, containerConfigurationTemplate);

            IList<ILayer> layers = image.getLayers();
            Assert.AreEqual(1, layers.size());
            Assert.AreEqual(
                new BlobDescriptor(
                    1000000,
                    DescriptorDigest.fromDigest(
                        "sha256:4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236")),
                layers.get(0).getBlobDescriptor());
            Assert.AreEqual(
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                layers.get(0).getDiffId());
            CollectionAssert.AreEqual(
                ImmutableArray.Create(
                    HistoryEntry.builder()
                        .setCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                        .setAuthor("Bazel")
                        .setCreatedBy("bazel build ...")
                        .setEmptyLayer(true)
                        .build(),
                    HistoryEntry.builder()
                        .setCreationTimestamp(Instant.FromUnixTimeSeconds(20))
                        .setAuthor("Jib")
                        .setCreatedBy("jib")
                        .build()),
                image.getHistory());
            Assert.AreEqual(Instant.FromUnixTimeSeconds(20), image.getCreated());
            Assert.AreEqual(Arrays.asList("some", "entrypoint", "command"), image.getEntrypoint());
            Assert.AreEqual(ImmutableDic.of("VAR1", "VAL1", "VAR2", "VAL2"), image.getEnvironment());
            Assert.AreEqual("/some/workspace", image.getWorkingDirectory());
            Assert.AreEqual(
                ImmutableHashSet.Create(Port.tcp(1000), Port.tcp(2000), Port.udp(3000)), image.getExposedPorts());
            Assert.AreEqual(
                ImmutableHashSet.Create(
                    AbsoluteUnixPath.get("/var/job-result-data"),
                    AbsoluteUnixPath.get("/var/log/my-app-logs")),
                image.getVolumes());
            Assert.AreEqual("tomcat", image.getUser());
            Assert.AreEqual("value1", image.getLabels().get("key1"));
            Assert.AreEqual("value2", image.getLabels().get("key2"));
            Assert.AreEqual(2, image.getLabels().size());
        }
    }
}
