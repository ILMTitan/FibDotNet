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

namespace com.google.cloud.tools.jib.registry {















/** Tests for {@link AuthenticationMethodRetriever}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class AuthenticationMethodRetrieverTest {

  [Mock] private HttpResponseException mockHttpResponseException;
  [Mock] private HttpHeaders mockHeaders;

  private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
      new RegistryEndpointRequestProperties("someServerUrl", "someImageName");
  private readonly AuthenticationMethodRetriever testAuthenticationMethodRetriever =
      new AuthenticationMethodRetriever(fakeRegistryEndpointRequestProperties, "user-agent");

  [TestMethod]
  public void testGetContent() {
    Assert.assertNull(testAuthenticationMethodRetriever.getContent());
  }

  [TestMethod]
  public void testGetAccept() {
    Assert.assertEquals(0, testAuthenticationMethodRetriever.getAccept().size());
  }

  [TestMethod]
  public void testHandleResponse() {
    Assert.assertNull(
        testAuthenticationMethodRetriever.handleResponse(Mockito.mock(typeof(HttpResponseMessage))));
  }

  [TestMethod]
  public void testGetApiRoute() {
    Assert.assertEquals(
        new Uri("http://someApiBase/"),
        testAuthenticationMethodRetriever.getApiRoute("http://someApiBase/"));
  }

  [TestMethod]
  public void testGetHttpMethod() {
    Assert.assertEquals(HttpMethod.Get, testAuthenticationMethodRetriever.getHttpMethod());
  }

  [TestMethod]
  public void testGetActionDescription() {
    Assert.assertEquals(
        "retrieve authentication method for someServerUrl",
        testAuthenticationMethodRetriever.getActionDescription());
  }

  [TestMethod]
  public void testHandleHttpResponseException_invalidStatusCode() {
    Mockito.when(mockHttpResponseException.getStatusCode()).thenReturn(-1);

    try {
      testAuthenticationMethodRetriever.handleHttpResponseException(mockHttpResponseException);
      Assert.fail(
          "Authentication method retriever should only handle HTTP 401 Unauthorized errors");

    } catch (HttpResponseException ex) {
      Assert.assertEquals(mockHttpResponseException, ex);
    }
  }

  [TestMethod]
  public void tsetHandleHttpResponseException_noHeader() {
    Mockito.when(mockHttpResponseException.getStatusCode())
        .thenReturn(HttpStatusCode.Unauthorized);
    Mockito.when(mockHttpResponseException.getHeaders()).thenReturn(mockHeaders);
    Mockito.when(mockHeaders.getAuthenticate()).thenReturn(null);

    try {
      testAuthenticationMethodRetriever.handleHttpResponseException(mockHttpResponseException);
      Assert.fail(
          "Authentication method retriever should fail if 'WWW-Authenticate' header is not found");

    } catch (RegistryErrorException ex) {
      Assert.assertThat(
          ex.getMessage(), CoreMatchers.containsString("'WWW-Authenticate' header not found"));
    }
  }

  [TestMethod]
  public void testHandleHttpResponseException_badAuthenticationMethod()
      {
    string authenticationMethod = "bad authentication method";

    Mockito.when(mockHttpResponseException.getStatusCode())
        .thenReturn(HttpStatusCode.Unauthorized);
    Mockito.when(mockHttpResponseException.getHeaders()).thenReturn(mockHeaders);
    Mockito.when(mockHeaders.getAuthenticate()).thenReturn(authenticationMethod);

    try {
      testAuthenticationMethodRetriever.handleHttpResponseException(mockHttpResponseException);
      Assert.fail(
          "Authentication method retriever should fail if 'WWW-Authenticate' header failed to parse");

    } catch (RegistryErrorException ex) {
      Assert.assertThat(
          ex.getMessage(),
          CoreMatchers.containsString(
              "Failed get authentication method from 'WWW-Authenticate' header"));
    }
  }

  [TestMethod]
  public void testHandleHttpResponseException_pass()
      {
    string authenticationMethod =
        "Bearer realm=\"https://somerealm\",service=\"someservice\",scope=\"somescope\"";

    Mockito.when(mockHttpResponseException.getStatusCode())
        .thenReturn(HttpStatusCode.Unauthorized);
    Mockito.when(mockHttpResponseException.getHeaders()).thenReturn(mockHeaders);
    Mockito.when(mockHeaders.getAuthenticate()).thenReturn(authenticationMethod);

    RegistryAuthenticator registryAuthenticator =
        testAuthenticationMethodRetriever.handleHttpResponseException(mockHttpResponseException);

    Assert.assertEquals(
        new Uri("https://somerealm?service=someservice&scope=repository:someImageName:someScope"),
        registryAuthenticator.getAuthenticationUrl(null, "someScope"));
  }
}
}
