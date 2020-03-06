// Copyright 2018 Google LLC.
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
using NUnit.Framework;
using System.Collections.Generic;

namespace Fib.Net.Core.Unit.Tests.Api
{
    /** Tests for {@link Fib.Net.Core.Api.Credential}. */

    public class CredentialTest
    {
        [Test]
        public void TestCredentialsHash()
        {
            Credential credentialA1 = Credential.From("username", "password");
            Credential credentialA2 = Credential.From("username", "password");
            Credential credentialB1 = Credential.From("", "");
            Credential credentialB2 = Credential.From("", "");

            Assert.AreEqual(credentialA1, credentialA2);
            Assert.AreEqual(credentialB1, credentialB2);
            Assert.AreNotEqual(credentialA1, credentialB1);
            Assert.AreNotEqual(credentialA1, credentialB2);

            ISet<Credential> credentialSet =
                new HashSet<Credential>(new[] { credentialA1, credentialA2, credentialB1, credentialB2 });
            CollectionAssert.AreEquivalent(new HashSet<Credential>(new[] { credentialA2, credentialB1 }), credentialSet);
        }

        [Test]
        public void TestCredentialsOAuth2RefreshToken()
        {
            Credential oauth2Credential = Credential.From("<token>", "eyJhbGciOi...3gw");
            Assert.IsTrue(
                oauth2Credential.IsOAuth2RefreshToken(),
                "Credential should be an auth2 token when username is <token>");
            Assert.AreEqual(
                "eyJhbGciOi...3gw",
                oauth2Credential.GetPassword(),
                "OAuth2 token credential should take password as refresh token");
        }
    }
}