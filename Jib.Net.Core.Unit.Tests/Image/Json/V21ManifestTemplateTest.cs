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
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Test.Common;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.image.json
{
    /** Tests for {@link V21ManifestTemplate}. */
    public class V21ManifestTemplateTest
    {
        [Test]
        public void testFromJson()
        {
            // Loads the JSON string.
            SystemPath jsonFile = Paths.get(TestResources.getResource("core/json/v21manifest.json").toURI());

            // Deserializes into a manifest JSON object.
            V21ManifestTemplate manifestJson =
                JsonTemplateMapper.readJsonFromFile<V21ManifestTemplate>(jsonFile);

            Assert.AreEqual(
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                manifestJson.getFsLayers().get(0).getDigest());

            ContainerConfigurationTemplate containerConfiguration =
                manifestJson.getContainerConfiguration().orElse(null);
            Assert.AreEqual(
                Arrays.asList("JAVA_HOME=/opt/openjdk", "PATH=/opt/openjdk/bin"),
                containerConfiguration.getContainerEnvironment());
            Assert.AreEqual(
                Arrays.asList("/opt/openjdk/bin/java"), containerConfiguration.getContainerEntrypoint());
        }
    }
}
