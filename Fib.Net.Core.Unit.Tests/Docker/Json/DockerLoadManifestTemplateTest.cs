// Copyright 2018 Google LLC.
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
using Fib.Net.Core.Docker.Json;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Json;
using Fib.Net.Test.Common;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace Fib.Net.Core.Unit.Tests.Docker.Json
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
