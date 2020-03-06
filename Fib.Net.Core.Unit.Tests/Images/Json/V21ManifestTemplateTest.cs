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
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Images.Json;
using Fib.Net.Core.Json;
using Fib.Net.Test.Common;
using NUnit.Framework;

namespace Fib.Net.Core.Unit.Tests.Images.Json
{
    /** Tests for {@link V21ManifestTemplate}. */
    public class V21ManifestTemplateTest
    {
        [Test]
        public void TestFromJson()
        {
            // Loads the JSON string.
            SystemPath jsonFile = Paths.Get(TestResources.GetResource("core/json/v21manifest.json").ToURI());

            // Deserializes into a manifest JSON object.
            V21ManifestTemplate manifestJson =
                JsonTemplateMapper.ReadJsonFromFile<V21ManifestTemplate>(jsonFile);

            Assert.AreEqual(
                DescriptorDigest.FromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                manifestJson.FsLayers[0].GetDigest());

            ContainerConfigurationTemplate containerConfiguration =
                manifestJson.GetContainerConfiguration().OrElse(null);
            Assert.AreEqual(
                new[] { "JAVA_HOME=/opt/openjdk", "PATH=/opt/openjdk/bin" },
                containerConfiguration.GetContainerEnvironment());
            Assert.AreEqual(
                new[] { "/opt/openjdk/bin/java" }, containerConfiguration.GetContainerEntrypoint());
        }
    }
}
