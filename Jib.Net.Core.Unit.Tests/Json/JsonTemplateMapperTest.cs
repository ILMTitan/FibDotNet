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
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.json
{






    /** Tests for {@link JsonTemplateMapper}. */
    public class JsonTemplateMapperTest
    {
        private class TestJson : JsonTemplate
        {
            public int number;
            public string text;
            public DescriptorDigest digest;
            public InnerObject innerObject;
            public IList<InnerObject> list;

            public class InnerObject : JsonTemplate
            {
                // This field has the same name as a field in the outer class, but either NOT interfere with
                // the other.
                public int number;
                public List<string> texts;
                public IList<DescriptorDigest> digests;
            }
        }

        [Test]
        public void testWriteJson()
        {
            SystemPath jsonFile = Paths.get(Resources.getResource("core/json/basic.json").toURI());
            string expectedJson = StandardCharsets.UTF_8.GetString(Files.readAllBytes(jsonFile));

            TestJson testJson = new TestJson
            {
                number = 54,
                text = "crepecake",
                digest =
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"),
                innerObject = new TestJson.InnerObject
                {
                    number = 23,
                    texts = Arrays.asList("first text", "second text"),
                    digests =
                Arrays.asList(
                    DescriptorDigest.fromDigest(
                        "sha256:91e0cae00b86c289b33fee303a807ae72dd9f0315c16b74e6ab0cdbe9d996c10"),
                    DescriptorDigest.fromHash(
                        "4945ba5011739b0b98c4a41afe224e417f47c7c99b2ce76830999c9a0861b236"))
                }
            };

            TestJson.InnerObject innerObject1 = new TestJson.InnerObject
            {
                number = 42,
                texts = Collections.emptyList<string>()
            };
            TestJson.InnerObject innerObject2 = new TestJson.InnerObject
            {
                number = 99,
                texts = Collections.singletonList("some text"),
                digests =
                Collections.singletonList(
                    DescriptorDigest.fromDigest(
                        "sha256:d38f571aa1c11e3d516e0ef7e513e7308ccbeb869770cb8c4319d63b10a0075e"))
            };
            testJson.list = Arrays.asList(innerObject1, innerObject2);

            Assert.AreEqual(expectedJson, JsonTemplateMapper.toUtf8String(testJson));
        }

        [Test]
        public void testReadJsonWithLock()
        {
            SystemPath jsonFile = Paths.get(Resources.getResource("core/json/basic.json").toURI());

            // Deserializes into a metadata JSON object.
            TestJson testJson = JsonTemplateMapper.readJsonFromFileWithLock<TestJson>(jsonFile);

            Assert.AreEqual(testJson.number, 54);
            Assert.AreEqual(testJson.text, "crepecake");
            Assert.AreEqual(
                testJson.digest,
                    DescriptorDigest.fromDigest(
                        "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad"));
            Assert.IsInstanceOf<TestJson.InnerObject>(testJson.innerObject);
            Assert.AreEqual(testJson.innerObject.number, 23);

            Assert.AreEqual(
                testJson.innerObject.texts, Arrays.asList("first text", "second text"));

            Assert.AreEqual(
                testJson.innerObject.digests,
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
            SystemPath jsonFile = Paths.get(Resources.getResource("core/json/basic_list.json").toURI());

            string jsonString = StandardCharsets.UTF_8.GetString(Files.readAllBytes(jsonFile));
            IList<TestJson> listofJsons = JsonTemplateMapper.readListOfJson<TestJson>(jsonString);
            TestJson json1 = listofJsons.get(0);
            TestJson json2 = listofJsons.get(1);

            DescriptorDigest digest1 =
                DescriptorDigest.fromDigest(
                    "sha256:91e0cae00b86c289b33fee303a807ae72dd9f0315c16b74e6ab0cdbe9d996c10");
            DescriptorDigest digest2 =
                DescriptorDigest.fromDigest(
                    "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad");

            Assert.AreEqual(1, json1.number);
            Assert.AreEqual(2, json2.number);
            Assert.AreEqual("text1", json1.text);
            Assert.AreEqual("text2", json2.text);
            Assert.AreEqual(digest1, json1.digest);
            Assert.AreEqual(digest2, json2.digest);
            Assert.AreEqual(10, json1.innerObject.number);
            Assert.AreEqual(20, json2.innerObject.number);
            Assert.AreEqual(2, json1.list.size());
            Assert.IsTrue(json2.list.isEmpty());
        }

        [Test]
        public void testToBlob_listOfJson()
        {
            SystemPath jsonFile = Paths.get(Resources.getResource("core/json/basic_list.json").toURI());

            string jsonString = StandardCharsets.UTF_8.GetString(Files.readAllBytes(jsonFile));
            List<TestJson> listOfJson = JsonTemplateMapper.readListOfJson<TestJson>(jsonString);

            Assert.AreEqual(jsonString, JsonTemplateMapper.toUtf8String(listOfJson));
        }
    }
}
