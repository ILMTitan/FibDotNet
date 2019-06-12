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

using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.image.json
{



    /** Tests for {@link V22ManifestTemplate}. */
    public class V22ManifestTemplateTest
    {
        [Test]
        public void testToJson()
        {
            // Loads the expected JSON string.
            SystemPath jsonFile = Paths.get(Resources.getResource("core/json/v22manifest.json").toURI());
            string expectedJson = StandardCharsets.UTF_8.GetString(Files.readAllBytes(jsonFile));

            // Creates the JSON object to serialize.
            V22ManifestTemplate manifestJson = new V22ManifestTemplate();

            manifestJson.setContainerConfiguration(
                1000,
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"));

            manifestJson.addLayer(
                1000_000,
                DescriptorDigest.fromHash(
                    "4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236"));

            // Serializes the JSON object.
            Assert.AreEqual(expectedJson, JsonTemplateMapper.toUtf8String(manifestJson));
        }

        [Test]
        public void testFromJson()
        {
            // Loads the JSON string.
            SystemPath jsonFile = Paths.get(Resources.getResource("core/json/v22manifest.json").toURI());

            // Deserializes into a manifest JSON object.
            V22ManifestTemplate manifestJson =
                JsonTemplateMapper.readJsonFromFile<V22ManifestTemplate>(jsonFile);

            Assert.AreEqual(
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                manifestJson.getContainerConfiguration().getDigest());

            Assert.AreEqual(1000, manifestJson.getContainerConfiguration().getSize());

            Assert.AreEqual(
                DescriptorDigest.fromHash(
                    "4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236"),
                manifestJson.getLayers().get(0).getDigest());

            Assert.AreEqual(1000_000, manifestJson.getLayers().get(0).getSize());
        }
    }
}