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

using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Jib.Net.Core.Registry;
using Jib.Net.Core.Unit.Tests.Http;
using NUnit.Framework;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Jib.Net.Core.Unit.Tests.Registry
{
    /** Tests for {@link RegistryAuthenticator}. */
    public class RegistryAuthenticatorTest
    {
        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties =
            new RegistryEndpointRequestProperties("someserver", "someimage");

        private RegistryAuthenticator registryAuthenticator;

        [SetUp]
        public void SetUp()
        {
            registryAuthenticator =
                RegistryAuthenticator.FromAuthenticationMethod(
                    new AuthenticationHeaderValue("Bearer", "realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\""),
                    registryEndpointRequestProperties,
                    new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) });
        }

        [Test]
        public void TestFromAuthenticationMethod_bearer()
        {
            RegistryAuthenticator registryAuthenticator =
                RegistryAuthenticator.FromAuthenticationMethod(
                    new AuthenticationHeaderValue("Bearer", "realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\""),
                    registryEndpointRequestProperties,
                    new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) });
            Assert.AreEqual(
                new Uri("https://somerealm?service=someservice&scope=repository:someimage:scope"),
                registryAuthenticator.GetAuthenticationUrl(null, "scope"));

            registryAuthenticator =
                RegistryAuthenticator.FromAuthenticationMethod(
                    new AuthenticationHeaderValue("bEaReR", "realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\""),
                    registryEndpointRequestProperties,
                    new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) });
            Assert.AreEqual(
                new Uri("https://somerealm?service=someservice&scope=repository:someimage:scope"),
                registryAuthenticator.GetAuthenticationUrl(null, "scope"));
        }

        [Test]
        public void TestAuthRequestParameters_basicAuth()
        {
            Assert.AreEqual(
                "service=someservice&scope=repository:someimage:scope",
                registryAuthenticator.GetAuthRequestParameters(null, "scope"));
        }

        [Test]
        public void TestAuthRequestParameters_oauth2()
        {
            Credential credential = Credential.From("<token>", "oauth2_access_token");
            Assert.AreEqual(
                "service=someservice&scope=repository:someimage:scope"
                    + "&client_id=jib.da031fe481a93ac107a95a96462358f9"
                    + "&grant_type=refresh_token&refresh_token=oauth2_access_token",
                registryAuthenticator.GetAuthRequestParameters(credential, "scope"));
        }

        [Test]
        public void IsOAuth2Auth_nullCredential()
        {
            Assert.IsFalse(RegistryAuthenticator.IsOAuth2Auth(null));
        }

        [Test]
        public void IsOAuth2Auth_basicAuth()
        {
            Credential credential = Credential.From("name", "password");
            Assert.IsFalse(RegistryAuthenticator.IsOAuth2Auth(credential));
        }

        [Test]
        public void IsOAuth2Auth_oauth2()
        {
            Credential credential = Credential.From("<token>", "oauth2_token");
            Assert.IsTrue(RegistryAuthenticator.IsOAuth2Auth(credential));
        }

        [Test]
        public void GetAuthenticationUrl_basicAuth()
        {
            Assert.AreEqual(
                new Uri("https://somerealm?service=someservice&scope=repository:someimage:scope"),
                registryAuthenticator.GetAuthenticationUrl(null, "scope"));
        }

        [Test]
        public void IstAuthenticationUrl_oauth2()
        {
            Credential credential = Credential.From("<token>", "oauth2_token");
            Assert.AreEqual(
                new Uri("https://somerealm"),
                registryAuthenticator.GetAuthenticationUrl(credential, "scope"));
        }

        [Test]
        public void TestFromAuthenticationMethod_basic()
        {
            Assert.IsNull(
                RegistryAuthenticator.FromAuthenticationMethod(
                    new AuthenticationHeaderValue("Basic", "realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\""),
                    registryEndpointRequestProperties,
                    new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) }));

            Assert.IsNull(
                RegistryAuthenticator.FromAuthenticationMethod(
                    new AuthenticationHeaderValue("BASIC", "realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\""),
                    registryEndpointRequestProperties,
                    new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) }));

            Assert.IsNull(
                RegistryAuthenticator.FromAuthenticationMethod(
                    new AuthenticationHeaderValue("bASIC", "realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\""),
                    registryEndpointRequestProperties,
                    new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) }));
        }

        [Test]
        public void TestFromAuthenticationMethod_noBearer()
        {
            try
            {
                RegistryAuthenticator.FromAuthenticationMethod(
                    new AuthenticationHeaderValue("unknown", "realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\""),
                    registryEndpointRequestProperties,
                    new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) });
                Assert.Fail("Authentication method without 'Bearer ' or 'Basic ' should fail");
            }
            catch (RegistryAuthenticationFailedException ex)
            {
                Assert.AreEqual(
                    "Failed to authenticate with registry someserver/someimage because: 'Bearer' was not found in the 'WWW-Authenticate' header, tried to parse: unknown",
                    ex.GetMessage());
            }
        }

        [Test]
        public void TestFromAuthenticationMethod_noRealm()
        {
            try
            {
                RegistryAuthenticator.FromAuthenticationMethod(
                    new AuthenticationHeaderValue("Bearer", "scope=\"somescope\""),
                    registryEndpointRequestProperties,
                    new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) });
                Assert.Fail("Authentication method without 'realm' should fail");
            }
            catch (RegistryAuthenticationFailedException ex)
            {
                Assert.AreEqual(
                    "Failed to authenticate with registry someserver/someimage because: 'realm' was not found in the 'WWW-Authenticate' header, tried to parse: scope=\"somescope\"",
                    ex.GetMessage());
            }
        }

        [Test]
        public void TestFromAuthenticationMethod_noService()
        {
            RegistryAuthenticator registryAuthenticator =
                RegistryAuthenticator.FromAuthenticationMethod(
                    new AuthenticationHeaderValue("Bearer", "realm=\"https://somerealm\""),
                    registryEndpointRequestProperties,
                    new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) });

            Assert.AreEqual(
                new Uri("https://somerealm?service=someserver&scope=repository:someimage:scope"),
                registryAuthenticator.GetAuthenticationUrl(null, "scope"));
        }

        [Test]
        public async Task TestUserAgentAsync()
        {
            using (TestWebServer server = new TestWebServer(false))
            {
                try
                {
                    RegistryAuthenticator authenticator =
                        RegistryAuthenticator.FromAuthenticationMethod(
                            new AuthenticationHeaderValue("Bearer", "realm=\"" + server.GetAddressAndPort() + "\""),
                            registryEndpointRequestProperties,
                            new[] { new ProductInfoHeaderValue(new ProductHeaderValue("Competent-Agent")) });
                    await authenticator.AuthenticatePushAsync(null).ConfigureAwait(false);
                }
                catch (RegistryAuthenticationFailedException)
                {
                    // Doesn't matter if auth fails. We only examine what we sent.
                }
                Assert.That(server.GetInputRead(), Does.Contain("User-Agent: Competent-Agent"));
            }
        }
    }
}
