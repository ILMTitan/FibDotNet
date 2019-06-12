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

namespace com.google.cloud.tools.jib.registry {















/** Tests for {@link AuthenticationMethodRetriever}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class AuthenticationMethodRetrieverTest {

  private HttpResponseMessage mockHttpResponse = Mock.Of<HttpResponseMessage>();
  private HttpResponseHeaders mockHeaders = Mock.Of<HttpResponseHeaders>();

  private  RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties;
  private  AuthenticationMethodRetriever testAuthenticationMethodRetriever ;
        [SetUp]
        public void Setup()
        {
            fakeRegistryEndpointRequestProperties =
         new RegistryEndpointRequestProperties("someServerUrl", "someImageName");
            testAuthenticationMethodRetriever =
         new AuthenticationMethodRetriever(fakeRegistryEndpointRequestProperties, "user-agent");

        }

  [Test]
  public void testGetContent() {
    Assert.IsNull(testAuthenticationMethodRetriever.getContent());
  }

  [Test]
  public void testGetAccept() {
    Assert.AreEqual(0, testAuthenticationMethodRetriever.getAccept().size());
  }

  [Test]
  public void testHandleResponse() {
    Assert.IsNull(
        testAuthenticationMethodRetriever.handleResponse(Mock.Of<HttpResponseMessage>()));
  }

  [Test]
  public void testGetApiRoute() {
    Assert.AreEqual(
        new Uri("http://someApiBase/"),
        testAuthenticationMethodRetriever.getApiRoute("http://someApiBase/"));
  }

  [Test]
  public void testGetHttpMethod() {
    Assert.AreEqual(HttpMethod.Get, testAuthenticationMethodRetriever.getHttpMethod());
  }

  [Test]
  public void testGetActionDescription() {
    Assert.AreEqual(
        "retrieve authentication method for someServerUrl",
        testAuthenticationMethodRetriever.getActionDescription());
  }

  [Test]
  public void testHandleHttpResponseException_invalidStatusCode() {
    Mock.Get(mockHttpResponse).Setup(m => m.getStatusCode()).Returns((HttpStatusCode)(-1));

    try {
      testAuthenticationMethodRetriever.handleHttpResponse(mockHttpResponse);
      Assert.Fail(
          "Authentication method retriever should only handle HTTP 401 Unauthorized errors");

    } catch (HttpResponseException ex) {
      Assert.AreEqual(mockHttpResponse, ex);
    }
  }

  [Test]
  public void tsetHandleHttpResponseException_noHeader() {
    Mock.Get(mockHttpResponse).Setup(m => m.getStatusCode()).Returns(HttpStatusCode.Unauthorized);

    Mock.Get(mockHttpResponse).Setup(m => m.getHeaders()).Returns(mockHeaders);

    Mock.Get(mockHeaders).Setup(m => m.getAuthenticate()).Returns(() => null);

    try {
      testAuthenticationMethodRetriever.handleHttpResponse(mockHttpResponse);
      Assert.Fail(
          "Authentication method retriever should fail if 'WWW-Authenticate' header is not found");

    } catch (RegistryErrorException ex) {
                StringAssert.Contains(
                    ex.getMessage(), "'WWW-Authenticate' header not found");
    }
  }

  [Test]
  public void testHandleHttpResponseException_pass()
      {
    string authenticationMethod =
        "Bearer realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"";

    Mock.Get(mockHttpResponse).Setup(m => m.getStatusCode()).Returns(HttpStatusCode.Unauthorized);

    Mock.Get(mockHttpResponse).Setup(m => m.getHeaders()).Returns(mockHeaders);

            Mock.Get(mockHeaders).Setup(m => m.getAuthenticate()).Returns(Mock.Of<HttpHeaderValueCollection<AuthenticationHeaderValue>>(c => c.Count == 1 && c.GetEnumerator() == new[] { authenticationMethod }.GetEnumerator()));

    RegistryAuthenticator registryAuthenticator =
        testAuthenticationMethodRetriever.handleHttpResponse(mockHttpResponse);

    Assert.AreEqual(
        new Uri("https://somerealm?service=someservice&scope=repository:someImageName:someScope"),
        registryAuthenticator.getAuthenticationUrl(null, "someScope"));
  }
}
}
