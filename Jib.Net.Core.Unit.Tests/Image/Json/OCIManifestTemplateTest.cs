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

using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Core.Images.Json;
using Jib.Net.Test.Common;
using NUnit.Framework;
using System.Text;

namespace com.google.cloud.tools.jib.image.json
{
    /** Tests for {@link OCIManifestTemplate}. */
    public class OCIManifestTemplateTest
    {
        [Test]
        public void TestToJson()
        {
            // Loads the expected JSON string.
            SystemPath jsonFile = Paths.Get(TestResources.GetResource("core/json/ocimanifest.json").ToURI());
            string expectedJson = Encoding.UTF8.GetString(Files.ReadAllBytes(jsonFile));

            // Creates the JSON object to serialize.
            OCIManifestTemplate manifestJson = new OCIManifestTemplate();

            manifestJson.SetContainerConfiguration(
                1000,
                DescriptorDigest.FromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"));

            manifestJson.AddLayer(
                1000_000,
                DescriptorDigest.FromHash(
                    "4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236"));

            // Serializes the JSON object.
            Assert.AreEqual(expectedJson, JsonTemplateMapper.ToUtf8String(manifestJson));
        }

        [Test]
        public void TestFromJson()
        {
            // Loads the JSON string.
            SystemPath jsonFile = Paths.Get(TestResources.GetResource("core/json/ocimanifest.json").ToURI());

            // Deserializes into a manifest JSON object.
            OCIManifestTemplate manifestJson =
                JsonTemplateMapper.ReadJsonFromFile<OCIManifestTemplate>(jsonFile);

            Assert.AreEqual(
                DescriptorDigest.FromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                manifestJson.GetContainerConfiguration().Digest);

            Assert.AreEqual(1000, manifestJson.GetContainerConfiguration().Size);

            Assert.AreEqual(
                DescriptorDigest.FromHash(
                    "4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236"),
                manifestJson.Layers.Get(0).Digest);

            Assert.AreEqual(1000_000, manifestJson.Layers.Get(0).Size);
        }
    }
}
