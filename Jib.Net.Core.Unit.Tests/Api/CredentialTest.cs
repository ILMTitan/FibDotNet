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

using Jib.Net.Core.Global;
using NUnit.Framework;
using System.Collections.Generic;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link com.google.cloud.tools.jib.api.Credential}. */

    public class CredentialTest
    {
        [Test]
        public void testCredentialsHash()
        {
            Credential credentialA1 = Credential.from("username", "password");
            Credential credentialA2 = Credential.from("username", "password");
            Credential credentialB1 = Credential.from("", "");
            Credential credentialB2 = Credential.from("", "");

            Assert.AreEqual(credentialA1, credentialA2);
            Assert.AreEqual(credentialB1, credentialB2);
            Assert.AreNotEqual(credentialA1, credentialB1);
            Assert.AreNotEqual(credentialA1, credentialB2);

            ISet<Credential> credentialSet =
                new HashSet<Credential>(Arrays.asList(credentialA1, credentialA2, credentialB1, credentialB2));
            CollectionAssert.AreEquivalent(new HashSet<Credential>(Arrays.asList(credentialA2, credentialB1)), credentialSet);
        }

        [Test]
        public void testCredentialsOAuth2RefreshToken()
        {
            Credential oauth2Credential = Credential.from("<token>", "eyJhbGciOi...3gw");
            Assert.IsTrue(
                oauth2Credential.isOAuth2RefreshToken(),
                "Credential should be an auth2 token when username is <token>");
            Assert.AreEqual(
                "eyJhbGciOi...3gw",
                oauth2Credential.getPassword(),
                "OAuth2 token credential should take password as refresh token");
        }
    }
}