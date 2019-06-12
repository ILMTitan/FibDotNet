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

using Jib.Net.Core.Api;
using NUnit.Framework;
using System.Text;
using Jib.Net.Core.Global;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link DescriptorDigest}. */

    public class DescriptorDigestTest
    {
        [Test]
        public void testCreateFromHash_pass()
        {
            string goodHash = createGoodHash('a');

            DescriptorDigest descriptorDigest = DescriptorDigest.fromHash(goodHash);

            Assert.AreEqual(goodHash, descriptorDigest.getHash());
            Assert.AreEqual("sha256:" + goodHash, descriptorDigest.toString());
        }

        [Test]
        public void testCreateFromHash_fail()
        {
            const string badHash = "not a valid hash";

            try
            {
                DescriptorDigest.fromHash(badHash);
                Assert.Fail("Invalid hash should have caused digest creation failure.");
            }
            catch (DigestException ex)
            {
                Assert.AreEqual("Invalid hash: " + badHash, ex.getMessage());
            }
        }

        [Test]
        public void testCreateFromHash_failIncorrectLength()
        {
            string badHash = createGoodHash('a') + 'a';

            try
            {
                DescriptorDigest.fromHash(badHash);
                Assert.Fail("Invalid hash should have caused digest creation failure.");
            }
            catch (DigestException ex)
            {
                Assert.AreEqual("Invalid hash: " + badHash, ex.getMessage());
            }
        }

        [Test]
        public void testCreateFromDigest_pass()
        {
            string goodHash = createGoodHash('a');
            string goodDigest = "sha256:" + createGoodHash('a');

            DescriptorDigest descriptorDigest = DescriptorDigest.fromDigest(goodDigest);

            Assert.AreEqual(goodHash, descriptorDigest.getHash());
            Assert.AreEqual(goodDigest, descriptorDigest.toString());
        }

        [Test]
        public void testCreateFromDigest_fail()
        {
            const string badDigest = "sha256:not a valid digest";

            try
            {
                DescriptorDigest.fromDigest(badDigest);
                Assert.Fail("Invalid digest should have caused digest creation failure.");
            }
            catch (DigestException ex)
            {
                Assert.AreEqual("Invalid digest: " + badDigest, ex.getMessage());
            }
        }

        [Test]
        public void testUseAsMapKey()
        {
            DescriptorDigest descriptorDigestA1 = DescriptorDigest.fromHash(createGoodHash('a'));
            DescriptorDigest descriptorDigestA2 = DescriptorDigest.fromHash(createGoodHash('a'));
            DescriptorDigest descriptorDigestA3 =
                DescriptorDigest.fromDigest("sha256:" + createGoodHash('a'));
            DescriptorDigest descriptorDigestB = DescriptorDigest.fromHash(createGoodHash('b'));

            IDictionary<DescriptorDigest, string> digestMap = new Dictionary<DescriptorDigest, string>();

            digestMap.put(descriptorDigestA1, "digest with a");
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA1));
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA2));
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA3));
            Assert.IsNull(digestMap.get(descriptorDigestB));

            digestMap.put(descriptorDigestA2, "digest with a");
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA1));
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA2));
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA3));
            Assert.IsNull(digestMap.get(descriptorDigestB));

            digestMap.put(descriptorDigestA3, "digest with a");
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA1));
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA2));
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA3));
            Assert.IsNull(digestMap.get(descriptorDigestB));

            digestMap.put(descriptorDigestB, "digest with b");
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA1));
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA2));
            Assert.AreEqual("digest with a", digestMap.get(descriptorDigestA3));
            Assert.AreEqual("digest with b", digestMap.get(descriptorDigestB));
        }

        /** Creates a 32 byte hexademical string to fit valid hash pattern. */

        private static string createGoodHash(char character)
        {
            StringBuilder goodHashBuffer = new StringBuilder(64);
            for (int i = 0; i < 64; i++)
            {
                goodHashBuffer.append(character);
            }
            return goodHashBuffer.toString();
        }
    }
}