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
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using System.Collections.Immutable;
using com.google.cloud.tools.jib.api;
using System.Collections.Generic;
using NodaTime;
using com.google.cloud.tools.jib.json;

namespace com.google.cloud.tools.jib.image.json
{








    /** Tests for {@link ContainerConfigurationTemplate}. */
    public class ContainerConfigurationTemplateTest
    {
        [Test]
        public void testToJson()
        {
            // Loads the expected JSON string.
            SystemPath jsonFile = Paths.get(Resources.getResource("core/json/containerconfig.json").toURI());
            string expectedJson = StandardCharsets.UTF_8.GetString(Files.readAllBytes(jsonFile));

            // Creates the JSON object to serialize.
            ContainerConfigurationTemplate containerConfigJson = new ContainerConfigurationTemplate();

            containerConfigJson.setCreated("1970-01-01T00:00:20Z");
            containerConfigJson.setArchitecture("wasm");
            containerConfigJson.setOs("js");
            containerConfigJson.setContainerEnvironment(Arrays.asList("VAR1=VAL1", "VAR2=VAL2"));
            containerConfigJson.setContainerEntrypoint(Arrays.asList("some", "entrypoint", "command"));
            containerConfigJson.setContainerCmd(Arrays.asList("arg1", "arg2"));
            containerConfigJson.setContainerHealthCheckTest(Arrays.asList("CMD-SHELL", "/checkhealth"));
            containerConfigJson.setContainerHealthCheckInterval(3000000000L);
            containerConfigJson.setContainerHealthCheckTimeout(1000000000L);
            containerConfigJson.setContainerHealthCheckStartPeriod(2000000000L);
            containerConfigJson.setContainerHealthCheckRetries(3);
            containerConfigJson.setContainerExposedPorts(
                new Dictionary<string, IDictionary<object, object>>
                {
                    ["1000/tcp"] =
                    ImmutableDictionary.Create<object, object>(),
                    ["2000/tcp"] =
                    ImmutableDictionary.Create<object, object>(),
                    ["3000/udp"] =
                    ImmutableDictionary.Create<object, object>()
                }.ToImmutableSortedDictionary());
            containerConfigJson.setContainerLabels(ImmutableDic.of("key1", "value1", "key2", "value2"));
            containerConfigJson.setContainerVolumes(
                ImmutableDic.of<string, IDictionary<object, object>>(
                    "/var/job-result-data", ImmutableDictionary.Create<object, object>(), "/var/log/my-app-logs", ImmutableDictionary.Create<object, object>()));
            containerConfigJson.setContainerWorkingDir("/some/workspace");
            containerConfigJson.setContainerUser("tomcat");

            containerConfigJson.addLayerDiffId(
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"));
            containerConfigJson.addHistoryEntry(
                HistoryEntry.builder()
                    .setCreationTimestamp(Instant.FromUnixTimeSeconds(0))
                    .setAuthor("Bazel")
                    .setCreatedBy("bazel build ...")
                    .setEmptyLayer(true)
                    .build());
            containerConfigJson.addHistoryEntry(
                HistoryEntry.builder()
                    .setCreationTimestamp(Instant.FromUnixTimeSeconds(20))
                    .setAuthor("Jib")
                    .setCreatedBy("jib")
                    .build());

            // Serializes the JSON object.
            Assert.AreEqual(expectedJson, JsonTemplateMapper.toUtf8String(containerConfigJson));
        }

        [Test]
        public void testFromJson()
        {
            // Loads the JSON string.
            SystemPath jsonFile = Paths.get(Resources.getResource("core/json/containerconfig.json").toURI());

            // Deserializes into a manifest JSON object.
            ContainerConfigurationTemplate containerConfigJson =
                JsonTemplateMapper.readJsonFromFile<ContainerConfigurationTemplate>(jsonFile);

            Assert.AreEqual("1970-01-01T00:00:20Z", containerConfigJson.getCreated());
            Assert.AreEqual("wasm", containerConfigJson.getArchitecture());
            Assert.AreEqual("js", containerConfigJson.getOs());
            Assert.AreEqual(
                Arrays.asList("VAR1=VAL1", "VAR2=VAL2"), containerConfigJson.getContainerEnvironment());
            Assert.AreEqual(
                Arrays.asList("some", "entrypoint", "command"),
                containerConfigJson.getContainerEntrypoint());
            Assert.AreEqual(Arrays.asList("arg1", "arg2"), containerConfigJson.getContainerCmd());

            Assert.AreEqual(
                Arrays.asList("CMD-SHELL", "/checkhealth"), containerConfigJson.getContainerHealthTest());
            Assert.IsNotNull(containerConfigJson.getContainerHealthInterval());
            Assert.AreEqual(3000000000L, containerConfigJson.getContainerHealthInterval().longValue());
            Assert.IsNotNull(containerConfigJson.getContainerHealthTimeout());
            Assert.AreEqual(1000000000L, containerConfigJson.getContainerHealthTimeout().longValue());
            Assert.IsNotNull(containerConfigJson.getContainerHealthStartPeriod());
            Assert.AreEqual(
                2000000000L, containerConfigJson.getContainerHealthStartPeriod().longValue());
            Assert.IsNotNull(containerConfigJson.getContainerHealthRetries());
            Assert.AreEqual(3, containerConfigJson.getContainerHealthRetries().intValue());

            Assert.AreEqual(
                ImmutableDic.of("key1", "value1", "key2", "value2"),
                containerConfigJson.getContainerLabels());
            Assert.AreEqual("/some/workspace", containerConfigJson.getContainerWorkingDir());
            Assert.AreEqual(
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                containerConfigJson.getLayerDiffId(0));
            Assert.AreEqual(
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
                containerConfigJson.getHistory());
        }
    }
}