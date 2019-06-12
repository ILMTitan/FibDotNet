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

using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry.credentials.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using NUnit.Framework;
using System;
using System.Buffers.Text;

namespace com.google.cloud.tools.jib.registry.credentials
{


    /** Tests for {@link DockerConfig}. */
    public class DockerConfigTest
    {
        private static string decodeBase64(string base64String)
        {
            return StandardCharsets.UTF_8.GetString(Convert.FromBase64String(base64String));
        }

        [Test]
        public void test_fromJson()
        {
            // Loads the JSON string.
            SystemPath jsonFile = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

            // Deserializes into a docker config JSON object.
            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.readJsonFromFile<DockerConfigTemplate>(jsonFile));

            Assert.AreEqual("some:auth", decodeBase64(dockerConfig.getAuthFor("some registry")));
            Assert.AreEqual(
                "some:other:auth", decodeBase64(dockerConfig.getAuthFor("some other registry")));
            Assert.AreEqual("token", decodeBase64(dockerConfig.getAuthFor("registry")));
            Assert.AreEqual("token", decodeBase64(dockerConfig.getAuthFor("https://registry")));
            Assert.IsNull(dockerConfig.getAuthFor("just registry"));

            Assert.AreEqual(
                Paths.get("docker-credential-some credential store"),
                dockerConfig.getCredentialHelperFor("some registry").getCredentialHelper());
            Assert.AreEqual(
                Paths.get("docker-credential-some credential store"),
                dockerConfig.getCredentialHelperFor("some other registry").getCredentialHelper());
            Assert.AreEqual(
                Paths.get("docker-credential-some credential store"),
                dockerConfig.getCredentialHelperFor("just registry").getCredentialHelper());
            Assert.AreEqual(
                Paths.get("docker-credential-some credential store"),
                dockerConfig.getCredentialHelperFor("with.protocol").getCredentialHelper());
            Assert.AreEqual(
                Paths.get("docker-credential-another credential helper"),
                dockerConfig.getCredentialHelperFor("another registry").getCredentialHelper());
            Assert.IsNull(dockerConfig.getCredentialHelperFor("unknonwn registry"));
        }

        [Test]
        public void testGetAuthFor_orderOfMatchPreference()
        {
            SystemPath json =
                Paths.get(Resources.getResource("core/json/dockerconfig_extra_matches.json").toURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.readJsonFromFile<DockerConfigTemplate>(json));

            Assert.AreEqual("my-registry: exact match", dockerConfig.getAuthFor("my-registry"));
            Assert.AreEqual("cool-registry: with https", dockerConfig.getAuthFor("cool-registry"));
            Assert.AreEqual(
                "awesome-registry: starting with name", dockerConfig.getAuthFor("awesome-registry"));
            Assert.AreEqual(
                "dull-registry: starting with name and with https",
                dockerConfig.getAuthFor("dull-registry"));
        }

        [Test]
        public void testGetAuthFor_correctSuffixMatching()
        {
            SystemPath json =
                Paths.get(Resources.getResource("core/json/dockerconfig_extra_matches.json").toURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.readJsonFromFile<DockerConfigTemplate>(json));

            Assert.IsNull(dockerConfig.getAuthFor("example"));
        }

        [Test]
        public void testGetCredentialHelperFor()
        {
            SystemPath json = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.readJsonFromFile<DockerConfigTemplate>(json));

            Assert.AreEqual(
                Paths.get("docker-credential-some credential store"),
                dockerConfig.getCredentialHelperFor("just registry").getCredentialHelper());
        }

        [Test]
        public void testGetCredentialHelperFor_withHttps()
        {
            SystemPath json = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.readJsonFromFile<DockerConfigTemplate>(json));

            Assert.AreEqual(
                Paths.get("docker-credential-some credential store"),
                dockerConfig.getCredentialHelperFor("with.protocol").getCredentialHelper());
        }

        [Test]
        public void testGetCredentialHelperFor_withSuffix()
        {
            SystemPath json = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.readJsonFromFile<DockerConfigTemplate>(json));

            Assert.AreEqual(
                Paths.get("docker-credential-some credential store"),
                dockerConfig.getCredentialHelperFor("with.suffix").getCredentialHelper());
        }

        [Test]
        public void testGetCredentialHelperFor_withProtocolAndSuffix()
        {
            SystemPath json = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.readJsonFromFile<DockerConfigTemplate>(json));

            Assert.AreEqual(
                Paths.get("docker-credential-some credential store"),
                dockerConfig.getCredentialHelperFor("with.protocol.and.suffix").getCredentialHelper());
        }

        [Test]
        public void testGetCredentialHelperFor_correctSuffixMatching()
        {
            SystemPath json = Paths.get(Resources.getResource("core/json/dockerconfig.json").toURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.readJsonFromFile<DockerConfigTemplate>(json));

            Assert.IsNull(dockerConfig.getCredentialHelperFor("example"));
            Assert.IsNull(dockerConfig.getCredentialHelperFor("another.example"));
        }
    }
}
