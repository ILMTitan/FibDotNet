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

using NUnit.Framework;
using Jib.Net.Core.Global;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Api;
using com.google.cloud.tools.jib.docker;
using System.Collections.Immutable;
using com.google.cloud.tools.jib.api;
using System.Collections.Generic;
using NodaTime;
using com.google.cloud.tools.jib.json;
using Jib.Net.Test.Common;
using System.Text;
using Jib.Net.Core.Images.Json;

namespace com.google.cloud.tools.jib.image.json
{
    /** Tests for {@link ContainerConfigurationTemplate}. */
    public class ContainerConfigurationTemplateTest
    {
        [Test]
        public void TestToJson()
        {
            // Loads the expected JSON string.
            SystemPath jsonFile = Paths.Get(TestResources.GetResource("core/json/containerconfig.json").ToURI());
            string expectedJson = Encoding.UTF8.GetString(Files.ReadAllBytes(jsonFile));

            // Creates the JSON object to serialize.
            ContainerConfigurationTemplate containerConfigJson = new ContainerConfigurationTemplate();

            containerConfigJson.Created = "1970-01-01T00:00:20Z";
            containerConfigJson.Architecture = "wasm";
            containerConfigJson.Os = "js";
            containerConfigJson.SetContainerEnvironment(Arrays.AsList("VAR1=VAL1", "VAR2=VAL2"));
            containerConfigJson.SetContainerEntrypoint(Arrays.AsList("some", "entrypoint", "command"));
            containerConfigJson.SetContainerCmd(Arrays.AsList("arg1", "arg2"));
            containerConfigJson.SetContainerHealthCheckTest(Arrays.AsList("CMD-SHELL", "/checkhealth"));
            containerConfigJson.SetContainerHealthCheckInterval(3000000000L);
            containerConfigJson.SetContainerHealthCheckTimeout(1000000000L);
            containerConfigJson.SetContainerHealthCheckStartPeriod(2000000000L);
            containerConfigJson.SetContainerHealthCheckRetries(3);
            containerConfigJson.SetContainerExposedPorts(
                new Dictionary<string, IDictionary<object, object>>
                {
                    ["1000/tcp"] =
                    ImmutableDictionary.Create<object, object>(),
                    ["2000/tcp"] =
                    ImmutableDictionary.Create<object, object>(),
                    ["3000/udp"] =
                    ImmutableDictionary.Create<object, object>()
                }.ToImmutableSortedDictionary());
            containerConfigJson.SetContainerLabels(ImmutableDic.Of("key1", "value1", "key2", "value2"));
            containerConfigJson.SetContainerVolumes(
                ImmutableDic.Of<string, IDictionary<object, object>>(
                    "/var/job-result-data", ImmutableDictionary.Create<object, object>(), "/var/log/my-app-logs", ImmutableDictionary.Create<object, object>()));
            containerConfigJson.SetContainerWorkingDir("/some/workspace");
            containerConfigJson.SetContainerUser("tomcat");

            containerConfigJson.AddLayerDiffId(
                DescriptorDigest.FromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"));
            containerConfigJson.AddHistoryEntry(
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .SetAuthor("Bazel")
                    .SetCreatedBy("bazel build ...")
                    .SetEmptyLayer(true)
                    .Build());
            containerConfigJson.AddHistoryEntry(
                HistoryEntry.CreateBuilder()
                    .SetCreationTimestamp(Instant.FromUnixTimeSeconds(20))
                    .SetAuthor("Jib")
                    .SetCreatedBy("jib")
                    .Build());

            // Serializes the JSON object.
            Assert.AreEqual(expectedJson, JsonTemplateMapper.ToUtf8String(containerConfigJson));
        }

        [Test]
        public void TestFromJson()
        {
            // Loads the JSON string.
            SystemPath jsonFile = Paths.Get(TestResources.GetResource("core/json/containerconfig.json").ToURI());

            // Deserializes into a manifest JSON object.
            ContainerConfigurationTemplate containerConfigJson =
                JsonTemplateMapper.ReadJsonFromFile<ContainerConfigurationTemplate>(jsonFile);

            Assert.AreEqual("1970-01-01T00:00:20Z", containerConfigJson.Created);
            Assert.AreEqual("wasm", containerConfigJson.Architecture);
            Assert.AreEqual("js", containerConfigJson.Os);
            Assert.AreEqual(
                Arrays.AsList("VAR1=VAL1", "VAR2=VAL2"), containerConfigJson.GetContainerEnvironment());
            Assert.AreEqual(
                Arrays.AsList("some", "entrypoint", "command"),
                containerConfigJson.GetContainerEntrypoint());
            Assert.AreEqual(Arrays.AsList("arg1", "arg2"), containerConfigJson.GetContainerCmd());

            Assert.AreEqual(
                Arrays.AsList("CMD-SHELL", "/checkhealth"), containerConfigJson.GetContainerHealthTest());
            Assert.IsNotNull(containerConfigJson.GetContainerHealthInterval());
            Assert.AreEqual(3000000000L, containerConfigJson.GetContainerHealthInterval().LongValue());
            Assert.IsNotNull(containerConfigJson.GetContainerHealthTimeout());
            Assert.AreEqual(1000000000L, containerConfigJson.GetContainerHealthTimeout().LongValue());
            Assert.IsNotNull(containerConfigJson.GetContainerHealthStartPeriod());
            Assert.AreEqual(
                2000000000L, containerConfigJson.GetContainerHealthStartPeriod().LongValue());
            Assert.IsNotNull(containerConfigJson.GetContainerHealthRetries());
            Assert.AreEqual(3, containerConfigJson.GetContainerHealthRetries().IntValue());

            Assert.AreEqual(
                ImmutableDic.Of("key1", "value1", "key2", "value2"),
                containerConfigJson.GetContainerLabels());
            Assert.AreEqual("/some/workspace", containerConfigJson.GetContainerWorkingDir());
            Assert.AreEqual(
                DescriptorDigest.FromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                containerConfigJson.GetLayerDiffId(0));
            Assert.AreEqual(
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
                containerConfigJson.History);
        }
    }
}
