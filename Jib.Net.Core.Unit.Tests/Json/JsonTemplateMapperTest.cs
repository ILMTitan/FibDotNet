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
using com.google.cloud.tools.jib.docker;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Test.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace com.google.cloud.tools.jib.json
{
    /** Tests for {@link JsonTemplateMapper}. */
    public class JsonTemplateMapperTest
    {
        [JsonObject(NamingStrategyType =typeof(CamelCaseNamingStrategy), ItemNullValueHandling = NullValueHandling.Ignore)]
        private class TestJson
        {
            public int Number { get; set; }
            public string Text { get; set; }
            public DescriptorDigest Digest { get; set; }
            public InnerObjectClass InnerObject { get; set; }
            public IList<InnerObjectClass> List { get; set; }

            [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy), ItemNullValueHandling = NullValueHandling.Ignore)]
            public class InnerObjectClass
            {
                // This field has the same name as a field in the outer class, but either NOT interfere with
                // the other.
                public int Number { get; set; }
                public List<string> Texts { get; set; }
                public IList<DescriptorDigest> Digests { get; set; }
            }
        }

        [Test]
        public void testWriteJson()
        {
            SystemPath jsonFile = Paths.get(TestResources.getResource("core/json/basic.json").toURI());
            string expectedJson = Encoding.UTF8.GetString(Files.readAllBytes(jsonFile));

            TestJson testJson = new TestJson
            {
                Number = 54,
                Text = "crepecake",
                Digest =
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                InnerObject = new TestJson.InnerObjectClass
                {
                    Number = 23,
                    Texts = Arrays.asList("first text", "second text"),
                    Digests =
                Arrays.asList(
                    DescriptorDigest.fromDigest(
                        "sha256:91e0cae00b86c289b33fee303a807ae72dd9f0315c16b74e6ab0cdbe9d996c10"),
                    DescriptorDigest.fromHash(
                        "4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236"))
                }
            };

            TestJson.InnerObjectClass innerObject1 = new TestJson.InnerObjectClass
            {
                Number = 42,
                Texts = Collections.emptyList<string>()
            };
            TestJson.InnerObjectClass innerObject2 = new TestJson.InnerObjectClass
            {
                Number = 99,
                Texts = Collections.singletonList("some text"),
                Digests =
                Collections.singletonList(
                    DescriptorDigest.fromDigest(
                        "sha256:d38f571aa1c11e3d516e0ef7e513e7308ccbeb869770cb8c4319d63b10a0075e"))
            };
            testJson.List = Arrays.asList(innerObject1, innerObject2);

            Assert.AreEqual(expectedJson, JsonTemplateMapper.toUtf8String(testJson));
        }

        [Test]
        public void testReadJsonWithLock()
        {
            SystemPath jsonFile = Paths.get(TestResources.getResource("core/json/basic.json").toURI());

            // Deserializes into a metadata JSON object.
            TestJson testJson = JsonTemplateMapper.readJsonFromFileWithLock<TestJson>(jsonFile);

            Assert.AreEqual(testJson.Number, 54);
            Assert.AreEqual(testJson.Text, "crepecake");
            Assert.AreEqual(
                testJson.Digest,
                    DescriptorDigest.fromDigest(
                        "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"));
            Assert.IsInstanceOf<TestJson.InnerObjectClass>(testJson.InnerObject);
            Assert.AreEqual(testJson.InnerObject.Number, 23);

            Assert.AreEqual(
                testJson.InnerObject.Texts, Arrays.asList("first text", "second text"));

            Assert.AreEqual(
                testJson.InnerObject.Digests,
                    Arrays.asList(
                        DescriptorDigest.fromDigest(
                            "sha256:91e0cae00b86c289b33fee303a807ae72dd9f0315c16b74e6ab0cdbe9d996c10"),
                        DescriptorDigest.fromHash(
                            "4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236")));

            // ignore testJson.list
        }

        [Test]
        public void testReadListOfJson()
        {
            SystemPath jsonFile = Paths.get(TestResources.getResource("core/json/basic_list.json").toURI());

            string jsonString = Encoding.UTF8.GetString(Files.readAllBytes(jsonFile));
            IList<TestJson> listofJsons = JsonTemplateMapper.readListOfJson<TestJson>(jsonString);
            TestJson json1 = listofJsons.get(0);
            TestJson json2 = listofJsons.get(1);

            DescriptorDigest digest1 =
                DescriptorDigest.fromDigest(
                    "sha256:91e0cae00b86c289b33fee303a807ae72dd9f0315c16b74e6ab0cdbe9d996c10");
            DescriptorDigest digest2 =
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad");

            Assert.AreEqual(1, json1.Number);
            Assert.AreEqual(2, json2.Number);
            Assert.AreEqual("text1", json1.Text);
            Assert.AreEqual("text2", json2.Text);
            Assert.AreEqual(digest1, json1.Digest);
            Assert.AreEqual(digest2, json2.Digest);
            Assert.AreEqual(10, json1.InnerObject.Number);
            Assert.AreEqual(20, json2.InnerObject.Number);
            Assert.AreEqual(2, json1.List.size());
            Assert.IsTrue(json2.List.isEmpty());
        }

        [Test]
        public void testToBlob_listOfJson()
        {
            SystemPath jsonFile = Paths.get(TestResources.getResource("core/json/basic_list.json").toURI());

            string jsonString = Encoding.UTF8.GetString(Files.readAllBytes(jsonFile));
            List<TestJson> listOfJson = JsonTemplateMapper.readListOfJson<TestJson>(jsonString);

            Assert.AreEqual(jsonString, JsonTemplateMapper.toUtf8String(listOfJson));
        }
    }
}
