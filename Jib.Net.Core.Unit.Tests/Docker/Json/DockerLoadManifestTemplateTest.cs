/*
 * Copyright 2018 Google LLC.
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

using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Test.Common;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace com.google.cloud.tools.jib.docker.json
{
    /** Tests for {@link DockerLoadManifestEntryTemplate}. */
    public class DockerLoadManifestTemplateTest
    {
        [Test]
        public void TestToJson()
        {
            // Loads the expected JSON string.
            SystemPath jsonFile = Paths.Get(TestResources.GetResource("core/json/loadmanifest.json").ToURI());
            string expectedJson = Encoding.UTF8.GetString(Files.ReadAllBytes(jsonFile));

            DockerLoadManifestEntryTemplate template = new DockerLoadManifestEntryTemplate();
            template.SetRepoTags(
                ImageReference.Of("testregistry", "testrepo", "testtag").ToStringWithTag());
            template.AddLayerFile("layer1.tar.gz");
            template.AddLayerFile("layer2.tar.gz");
            template.AddLayerFile("layer3.tar.gz");

            List<DockerLoadManifestEntryTemplate> loadManifest = new List<DockerLoadManifestEntryTemplate> { template };
            Assert.AreEqual(expectedJson, JsonTemplateMapper.ToUtf8String(loadManifest));
        }
    }
}
