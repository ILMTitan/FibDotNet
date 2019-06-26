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

using com.google.cloud.tools.jib.cache;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Tests for {@link AuthenticationMethodRetriever}. */
    public class AuthenticationMethodRetrieverTest
    {
        private RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties;
        private AuthenticationMethodRetriever testAuthenticationMethodRetriever;

        [SetUp]
        public void Setup()
        {
            fakeRegistryEndpointRequestProperties =
         new RegistryEndpointRequestProperties("someServerUrl", "someImageName");
            testAuthenticationMethodRetriever =
         new AuthenticationMethodRetriever(
             fakeRegistryEndpointRequestProperties,
             new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) });
        }

        [Test]
        public void TestGetContent()
        {
            Assert.IsNull(testAuthenticationMethodRetriever.GetContent());
        }

        [Test]
        public void TestGetAccept()
        {
            Assert.AreEqual(0, testAuthenticationMethodRetriever.GetAccept().Count);
        }

        [Test]
        public async Task TestHandleResponseAsync()
        {
            Assert.IsNull(await testAuthenticationMethodRetriever.HandleResponseAsync(Mock.Of<HttpResponseMessage>()).ConfigureAwait(false));
        }

        [Test]
        public void TestGetApiRoute()
        {
            Assert.AreEqual(
                new Uri("http://someApiBase/"),
                testAuthenticationMethodRetriever.GetApiRoute("http://someApiBase/"));
        }

        [Test]
        public void TestGetHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Get, testAuthenticationMethodRetriever.GetHttpMethod());
        }

        [Test]
        public void TestGetActionDescription()
        {
            Assert.AreEqual(
                "retrieve authentication method for someServerUrl",
                testAuthenticationMethodRetriever.GetActionDescription());
        }

        [Test]
        public void TestHandleHttpResponseException_invalidStatusCode()
        {
            var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            try
            {
                testAuthenticationMethodRetriever.HandleHttpResponse(mockHttpResponse);
                Assert.Fail(
                    "Authentication method retriever should only handle HTTP 401 Unauthorized errors");
            }
            catch (HttpResponseException ex)
            {
                Assert.AreEqual(mockHttpResponse, ex.Cause);
            }
        }

        [Test]
        public void TsetHandleHttpResponseException_noHeader()
        {
            var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            mockHttpResponse.Headers.WwwAuthenticate.Clear();

            try
            {
                testAuthenticationMethodRetriever.HandleHttpResponse(mockHttpResponse);
                Assert.Fail(
                    "Authentication method retriever should fail if 'WWW-Authenticate' header is not found");
            }
            catch (RegistryErrorException ex)
            {
                Assert.That(
                    ex.GetMessage(), Does.Contain("'WWW-Authenticate' header not found"));
            }
        }

        [Test]
        public void TestHandleHttpResponseException_pass()
        {
            const string authScheme = "Bearer";
            const string authParamter = "realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"";

            var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers ={
                    WwwAuthenticate = {
                        new AuthenticationHeaderValue(authScheme,authParamter)
                    }
                }
            };

            RegistryAuthenticator registryAuthenticator =
                testAuthenticationMethodRetriever.HandleHttpResponse(mockHttpResponse);

            Assert.AreEqual(
                new Uri("https://somerealm?service=someservice&scope=repository:someImageName:someScope"),
                registryAuthenticator.GetAuthenticationUrl(null, "someScope"));
        }
    }
}
