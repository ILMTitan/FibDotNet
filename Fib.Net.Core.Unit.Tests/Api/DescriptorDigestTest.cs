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

using Fib.Net.Core.Api;
using NUnit.Framework;
using System.Text;
using System.Collections.Generic;

namespace Fib.Net.Core.Unit.Tests.Api
{
    /** Tests for {@link DescriptorDigest}. */

    public class DescriptorDigestTest
    {
        [Test]
        public void TestCreateFromHash_pass()
        {
            string goodHash = CreateGoodHash('a');

            DescriptorDigest descriptorDigest = DescriptorDigest.FromHash(goodHash);

            Assert.AreEqual(goodHash, descriptorDigest.GetHash());
            Assert.AreEqual("sha256:" + goodHash, descriptorDigest.ToString());
        }

        [Test]
        public void TestCreateFromHash_fail()
        {
            const string badHash = "not a valid hash";

            try
            {
                DescriptorDigest.FromHash(badHash);
                Assert.Fail("Invalid hash should have caused digest creation failure.");
            }
            catch (DigestException ex)
            {
                Assert.AreEqual("Invalid hash: " + badHash, ex.Message);
            }
        }

        [Test]
        public void TestCreateFromHash_failIncorrectLength()
        {
            string badHash = CreateGoodHash('a') + 'a';

            try
            {
                DescriptorDigest.FromHash(badHash);
                Assert.Fail("Invalid hash should have caused digest creation failure.");
            }
            catch (DigestException ex)
            {
                Assert.AreEqual("Invalid hash: " + badHash, ex.Message);
            }
        }

        [Test]
        public void TestCreateFromDigest_pass()
        {
            string goodHash = CreateGoodHash('a');
            string goodDigest = "sha256:" + CreateGoodHash('a');

            DescriptorDigest descriptorDigest = DescriptorDigest.FromDigest(goodDigest);

            Assert.AreEqual(goodHash, descriptorDigest.GetHash());
            Assert.AreEqual(goodDigest, descriptorDigest.ToString());
        }

        [Test]
        public void TestCreateFromDigest_fail()
        {
            const string badDigest = "sha256:not a valid digest";

            try
            {
                DescriptorDigest.FromDigest(badDigest);
                Assert.Fail("Invalid digest should have caused digest creation failure.");
            }
            catch (DigestException ex)
            {
                Assert.AreEqual("Invalid digest: " + badDigest, ex.Message);
            }
        }

        [Test]
        public void TestUseAsMapKey()
        {
            DescriptorDigest descriptorDigestA1 = DescriptorDigest.FromHash(CreateGoodHash('a'));
            DescriptorDigest descriptorDigestA2 = DescriptorDigest.FromHash(CreateGoodHash('a'));
            DescriptorDigest descriptorDigestA3 =
                DescriptorDigest.FromDigest("sha256:" + CreateGoodHash('a'));
            DescriptorDigest descriptorDigestB = DescriptorDigest.FromHash(CreateGoodHash('b'));

            IDictionary<DescriptorDigest, string> digestMap = new Dictionary<DescriptorDigest, string>
            {
                [descriptorDigestA1] = "digest with a"
            };
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA1]);
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA2]);
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA3]);
            Assert.IsFalse(digestMap.ContainsKey(descriptorDigestB));

            digestMap[descriptorDigestA2] = "digest with a";
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA1]);
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA2]);
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA3]);
            Assert.IsFalse(digestMap.ContainsKey(descriptorDigestB));

            digestMap[descriptorDigestA3] = "digest with a";
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA1]);
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA2]);
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA3]);
            Assert.IsFalse(digestMap.ContainsKey(descriptorDigestB));

            digestMap[descriptorDigestB] = "digest with b";
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA1]);
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA2]);
            Assert.AreEqual("digest with a", digestMap[descriptorDigestA3]);
            Assert.AreEqual("digest with b", digestMap[descriptorDigestB]);
        }

        /** Creates a 32 byte hexademical string to fit valid hash pattern. */

        private static string CreateGoodHash(char character)
        {
            StringBuilder goodHashBuffer = new StringBuilder(64);
            for (int i = 0; i < 64; i++)
            {
                goodHashBuffer.Append(character);
            }
            return goodHashBuffer.ToString();
        }
    }
}