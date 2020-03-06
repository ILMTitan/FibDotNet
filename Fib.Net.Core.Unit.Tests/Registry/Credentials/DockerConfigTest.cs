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

using Fib.Net.Core.Api;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Json;
using Fib.Net.Core.Registry.Credentials;
using Fib.Net.Core.Registry.Credentials.Json;
using Fib.Net.Test.Common;
using NUnit.Framework;
using System;
using System.Text;

namespace Fib.Net.Core.Unit.Tests.Registry.Credentials
{
    /** Tests for {@link DockerConfig}. */
    public class DockerConfigTest
    {
        private static string DecodeBase64(string base64String)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
        }

        [Test]
        public void Test_fromJson()
        {
            // Loads the JSON string.
            SystemPath jsonFile = Paths.Get(TestResources.GetResource("core/json/dockerconfig.json").ToURI());

            // Deserializes into a docker config JSON object.
            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.ReadJsonFromFile<DockerConfigTemplate>(jsonFile));

            Assert.AreEqual("some:auth", DecodeBase64(dockerConfig.GetAuthFor("some registry")));
            Assert.AreEqual(
                "some:other:auth", DecodeBase64(dockerConfig.GetAuthFor("some other registry")));
            Assert.AreEqual("token", DecodeBase64(dockerConfig.GetAuthFor("registry")));
            Assert.AreEqual("token", DecodeBase64(dockerConfig.GetAuthFor("https://registry")));
            Assert.IsNull(dockerConfig.GetAuthFor("just registry"));

            Assert.AreEqual(
                Paths.Get("docker-credential-some credential store"),
                dockerConfig.GetCredentialHelperFor("some registry").GetCredentialHelper());
            Assert.AreEqual(
                Paths.Get("docker-credential-some credential store"),
                dockerConfig.GetCredentialHelperFor("some other registry").GetCredentialHelper());
            Assert.AreEqual(
                Paths.Get("docker-credential-some credential store"),
                dockerConfig.GetCredentialHelperFor("just registry").GetCredentialHelper());
            Assert.AreEqual(
                Paths.Get("docker-credential-some credential store"),
                dockerConfig.GetCredentialHelperFor("with.protocol").GetCredentialHelper());
            Assert.AreEqual(
                Paths.Get("docker-credential-another credential helper"),
                dockerConfig.GetCredentialHelperFor("another registry").GetCredentialHelper());
            Assert.IsNull(dockerConfig.GetCredentialHelperFor("unknonwn registry"));
        }

        [Test]
        public void TestGetAuthFor_orderOfMatchPreference()
        {
            SystemPath json =
                Paths.Get(TestResources.GetResource("core/json/dockerconfig_extra_matches.json").ToURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.ReadJsonFromFile<DockerConfigTemplate>(json));

            Assert.AreEqual("my-registry: exact match", dockerConfig.GetAuthFor("my-registry"));
            Assert.AreEqual("cool-registry: with https", dockerConfig.GetAuthFor("cool-registry"));
            Assert.AreEqual(
                "awesome-registry: starting with name", dockerConfig.GetAuthFor("awesome-registry"));
            Assert.AreEqual(
                "dull-registry: starting with name and with https",
                dockerConfig.GetAuthFor("dull-registry"));
        }

        [Test]
        public void TestGetAuthFor_correctSuffixMatching()
        {
            SystemPath json =
                Paths.Get(TestResources.GetResource("core/json/dockerconfig_extra_matches.json").ToURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.ReadJsonFromFile<DockerConfigTemplate>(json));

            Assert.IsNull(dockerConfig.GetAuthFor("example"));
        }

        [Test]
        public void TestGetCredentialHelperFor()
        {
            SystemPath json = Paths.Get(TestResources.GetResource("core/json/dockerconfig.json").ToURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.ReadJsonFromFile<DockerConfigTemplate>(json));

            Assert.AreEqual(
                Paths.Get("docker-credential-some credential store"),
                dockerConfig.GetCredentialHelperFor("just registry").GetCredentialHelper());
        }

        [Test]
        public void TestGetCredentialHelperFor_withHttps()
        {
            SystemPath json = Paths.Get(TestResources.GetResource("core/json/dockerconfig.json").ToURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.ReadJsonFromFile<DockerConfigTemplate>(json));

            Assert.AreEqual(
                Paths.Get("docker-credential-some credential store"),
                dockerConfig.GetCredentialHelperFor("with.protocol").GetCredentialHelper());
        }

        [Test]
        public void TestGetCredentialHelperFor_withSuffix()
        {
            SystemPath json = Paths.Get(TestResources.GetResource("core/json/dockerconfig.json").ToURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.ReadJsonFromFile<DockerConfigTemplate>(json));

            Assert.AreEqual(
                Paths.Get("docker-credential-some credential store"),
                dockerConfig.GetCredentialHelperFor("with.suffix").GetCredentialHelper());
        }

        [Test]
        public void TestGetCredentialHelperFor_withProtocolAndSuffix()
        {
            SystemPath json = Paths.Get(TestResources.GetResource("core/json/dockerconfig.json").ToURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.ReadJsonFromFile<DockerConfigTemplate>(json));

            Assert.AreEqual(
                Paths.Get("docker-credential-some credential store"),
                dockerConfig.GetCredentialHelperFor("with.protocol.and.suffix").GetCredentialHelper());
        }

        [Test]
        public void TestGetCredentialHelperFor_correctSuffixMatching()
        {
            SystemPath json = Paths.Get(TestResources.GetResource("core/json/dockerconfig.json").ToURI());

            DockerConfig dockerConfig =
                new DockerConfig(JsonTemplateMapper.ReadJsonFromFile<DockerConfigTemplate>(json));

            Assert.IsNull(dockerConfig.GetCredentialHelperFor("example"));
            Assert.IsNull(dockerConfig.GetCredentialHelperFor("another.example"));
        }
    }
}
