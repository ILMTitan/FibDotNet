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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.http;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;

namespace com.google.cloud.tools.jib.registry
{



    /** Tests for {@link RegistryAuthenticator}. */
    public class RegistryAuthenticatorTest
    {
        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties =
            new RegistryEndpointRequestProperties("someserver", "someimage");

        private RegistryAuthenticator registryAuthenticator;

        [SetUp]
        public void setUp()
        {
            registryAuthenticator =
                RegistryAuthenticator.fromAuthenticationMethod(
                    "Bearer realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"",
                    registryEndpointRequestProperties,
                    "user-agent");
        }

        [Test]
        public void testFromAuthenticationMethod_bearer()
        {
            RegistryAuthenticator registryAuthenticator =
                RegistryAuthenticator.fromAuthenticationMethod(
                    "Bearer realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"",
                    registryEndpointRequestProperties,
                    "user-agent");
            Assert.AreEqual(
                new Uri("https://somerealm?service=someservice&scope=repository:someimage:scope"),
                registryAuthenticator.getAuthenticationUrl(null, "scope"));

            registryAuthenticator =
                RegistryAuthenticator.fromAuthenticationMethod(
                    "bEaReR realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"",
                    registryEndpointRequestProperties,
                    "user-agent");
            Assert.AreEqual(
                new Uri("https://somerealm?service=someservice&scope=repository:someimage:scope"),
                registryAuthenticator.getAuthenticationUrl(null, "scope"));
        }

        [Test]
        public void testAuthRequestParameters_basicAuth()
        {
            Assert.AreEqual(
                "service=someservice&scope=repository:someimage:scope",
                registryAuthenticator.getAuthRequestParameters(null, "scope"));
        }

        [Test]
        public void testAuthRequestParameters_oauth2()
        {
            Credential credential = Credential.from("<token>", "oauth2_access_token");
            Assert.AreEqual(
                "service=someservice&scope=repository:someimage:scope"
                    + "&client_id=jib.da031fe481a93ac107a95a96462358f9"
                    + "&grant_type=refresh_token&refresh_token=oauth2_access_token",
                registryAuthenticator.getAuthRequestParameters(credential, "scope"));
        }

        [Test]
        public void isOAuth2Auth_nullCredential()
        {
            Assert.IsFalse(registryAuthenticator.isOAuth2Auth(null));
        }

        [Test]
        public void isOAuth2Auth_basicAuth()
        {
            Credential credential = Credential.from("name", "password");
            Assert.IsFalse(registryAuthenticator.isOAuth2Auth(credential));
        }

        [Test]
        public void isOAuth2Auth_oauth2()
        {
            Credential credential = Credential.from("<token>", "oauth2_token");
            Assert.IsTrue(registryAuthenticator.isOAuth2Auth(credential));
        }

        [Test]
        public void getAuthenticationUrl_basicAuth()
        {
            Assert.AreEqual(
                new Uri("https://somerealm?service=someservice&scope=repository:someimage:scope"),
                registryAuthenticator.getAuthenticationUrl(null, "scope"));
        }

        [Test]
        public void istAuthenticationUrl_oauth2()
        {
            Credential credential = Credential.from("<token>", "oauth2_token");
            Assert.AreEqual(
                new Uri("https://somerealm"),
                registryAuthenticator.getAuthenticationUrl(credential, "scope"));
        }

        [Test]
        public void testFromAuthenticationMethod_basic()
        {
            Assert.IsNull(
                RegistryAuthenticator.fromAuthenticationMethod(
                    "Basic realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"",
                    registryEndpointRequestProperties,
                    "user-agent"));

            Assert.IsNull(
                RegistryAuthenticator.fromAuthenticationMethod(
                    "BASIC realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"",
                    registryEndpointRequestProperties,
                    "user-agent"));

            Assert.IsNull(
                RegistryAuthenticator.fromAuthenticationMethod(
                    "bASIC realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"",
                    registryEndpointRequestProperties,
                    "user-agent"));
        }

        [Test]
        public void testFromAuthenticationMethod_noBearer()
        {
            try
            {
                RegistryAuthenticator.fromAuthenticationMethod(
                    "realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"",
                    registryEndpointRequestProperties,
                    "user-agent");
                Assert.Fail("Authentication method without 'Bearer ' or 'Basic ' should fail");
            }
            catch (RegistryAuthenticationFailedException ex)
            {
                Assert.AreEqual(
                    "Failed to authenticate with registry someserver/someimage because: 'Bearer' was not found in the 'WWW-Authenticate' header, tried to parse: realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"",
                    ex.getMessage());
            }
        }

        [Test]
        public void testFromAuthenticationMethod_noRealm()
        {
            try
            {
                RegistryAuthenticator.fromAuthenticationMethod(
                    "Bearer scope=\"somescope\"", registryEndpointRequestProperties, "user-agent");
                Assert.Fail("Authentication method without 'realm' should fail");
            }
            catch (RegistryAuthenticationFailedException ex)
            {
                Assert.AreEqual(
                    "Failed to authenticate with registry someserver/someimage because: 'realm' was not found in the 'WWW-Authenticate' header, tried to parse: Bearer scope=\"somescope\"",
                    ex.getMessage());
            }
        }

        [Test]
        public void testFromAuthenticationMethod_noService()
        {
            RegistryAuthenticator registryAuthenticator =
                RegistryAuthenticator.fromAuthenticationMethod(
                    "Bearer realm=\"https://somerealm\"", registryEndpointRequestProperties, "user-agent");

            Assert.AreEqual(
                new Uri("https://somerealm?service=someserver&scope=repository:someimage:scope"),
                registryAuthenticator.getAuthenticationUrl(null, "scope"));
        }

        [Test]
        public void testUserAgent()
        {
            using (TestWebServer server = new TestWebServer(false))
            {
                try
                {
                    RegistryAuthenticator authenticator =
                        RegistryAuthenticator.fromAuthenticationMethod(
                            "Bearer realm=\"" + server.getEndpoint() + "\"",
                            registryEndpointRequestProperties,
                            "Competent-Agent");
                    authenticator.authenticatePush(null);
                }
                catch (RegistryAuthenticationFailedException)
                {
                    // Doesn't matter if auth fails. We only examine what we sent.
                }
                StringAssert.Contains(
                    server.getInputRead(), "User-Agent: Competent-Agent");
            }
        }
    }
}
