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










































/** Tests for {@link RegistryEndpointCaller}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class RegistryEndpointCallerTest {

  /** Implementation of {@link RegistryEndpointProvider} for testing. */
  private class TestRegistryEndpointProvider : RegistryEndpointProvider<string>  {
    public string getHttpMethod() {
      return "httpMethod";
    }

    public Uri getApiRoute(string apiRouteBase) {
      return new Uri(apiRouteBase + "/api");
    }

    public BlobHttpContent getContent() {
      return null;
    }

    public List<string> getAccept() {
      return Collections.emptyList();
    }

    public string handleResponse(Response response) {
      return CharStreams.toString(
          new InputStreamReader(response.getBody(), StandardCharsets.UTF_8));
    }

    public string getActionDescription() {
      return "actionDescription";
    }
  }

  private static HttpResponse mockHttpResponse(int statusCode, HttpHeaders headers)
      {
    HttpResponse mock = Mockito.mock(typeof(HttpResponse));
    Mockito.when(mock.getStatusCode()).thenReturn(statusCode);
    Mockito.when(mock.parseAsString()).thenReturn("");
    Mockito.when(mock.getHeaders()).thenReturn(headers != null ? headers : new HttpHeaders());
    return mock;
  }

  private static HttpResponse mockRedirectHttpResponse(string redirectLocation) {
    int code307 = HttpStatusCodes.STATUS_CODE_TEMPORARY_REDIRECT;
    return mockHttpResponse(code307, new HttpHeaders().setLocation(redirectLocation));
  }

  [Mock] private EventHandlers mockEventHandlers;
  [Mock] private Connection mockConnection;
  [Mock] private Connection mockInsecureConnection;
  [Mock] private Response mockResponse;
  [Mock] private Function<Uri, Connection> mockConnectionFactory;
  [Mock] private Function<Uri, Connection> mockInsecureConnectionFactory;

  private RegistryEndpointCaller<string> secureEndpointCaller;

  [TestInitialize]
  public void setUp() {
    secureEndpointCaller = createRegistryEndpointCaller(false, -1);

    Mockito.when(mockConnectionFactory.apply(Mockito.any())).thenReturn(mockConnection);
    Mockito.when(mockInsecureConnectionFactory.apply(Mockito.any()))
        .thenReturn(mockInsecureConnection);
    Mockito.when(mockResponse.getBody())
        .thenReturn(new ByteArrayInputStream("body".getBytes(StandardCharsets.UTF_8)));
  }

  @After
  public void tearDown() {
    System.clearProperty(JibSystemProperties.HTTP_TIMEOUT);
    System.clearProperty(JibSystemProperties.SEND_CREDENTIALS_OVER_HTTP);
  }

  [TestMethod]
  public void testCall_secureCallerOnUnverifiableServer() {
    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(SSLPeerUnverifiedException))); // unverifiable HTTPS server

    try {
      secureEndpointCaller.call();
      Assert.fail("Secure caller should fail if cannot verify server");
    } catch (InsecureRegistryException ex) {
      Assert.assertEquals(
          "Failed to verify the server at https://apiRouteBase/api because only secure connections are allowed.",
          ex.getMessage());
    }
  }

  [TestMethod]
  public void testCall_insecureCallerOnUnverifiableServer() {
    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(SSLPeerUnverifiedException))); // unverifiable HTTPS server
    Mockito.when(mockInsecureConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenReturn(mockResponse); // OK with non-verifying connection

    RegistryEndpointCaller<string> insecureCaller = createRegistryEndpointCaller(true, -1);
    Assert.assertEquals("body", insecureCaller.call());

    ArgumentCaptor<Uri> urlCaptor = ArgumentCaptor.forClass(typeof(Uri));
    Mockito.verify(mockConnectionFactory).apply(urlCaptor.capture());
    Assert.assertEquals(new Uri("https://apiRouteBase/api"), urlCaptor.getAllValues().get(0));

    Mockito.verify(mockInsecureConnectionFactory).apply(urlCaptor.capture());
    Assert.assertEquals(new Uri("https://apiRouteBase/api"), urlCaptor.getAllValues().get(1));

    Mockito.verifyNoMoreInteractions(mockConnectionFactory);
    Mockito.verifyNoMoreInteractions(mockInsecureConnectionFactory);

    Mockito.verify(mockEventHandlers)
        .dispatch(
            LogEvent.info(
                "Cannot verify server at https://apiRouteBase/api. Attempting again with no TLS verification."));
  }

  [TestMethod]
  public void testCall_insecureCallerOnHttpServer() {
    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(SSLPeerUnverifiedException))) // server is not HTTPS
        .thenReturn(mockResponse);
    Mockito.when(mockInsecureConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(SSLPeerUnverifiedException))); // server is not HTTPS

    RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
    Assert.assertEquals("body", insecureEndpointCaller.call());

    ArgumentCaptor<Uri> urlCaptor = ArgumentCaptor.forClass(typeof(Uri));
    Mockito.verify(mockConnectionFactory, Mockito.times(2)).apply(urlCaptor.capture());
    Assert.assertEquals(new Uri("https://apiRouteBase/api"), urlCaptor.getAllValues().get(0));
    Assert.assertEquals(new Uri("http://apiRouteBase/api"), urlCaptor.getAllValues().get(1));

    Mockito.verify(mockInsecureConnectionFactory).apply(urlCaptor.capture());
    Assert.assertEquals(new Uri("https://apiRouteBase/api"), urlCaptor.getAllValues().get(2));

    Mockito.verifyNoMoreInteractions(mockConnectionFactory);
    Mockito.verifyNoMoreInteractions(mockInsecureConnectionFactory);

    Mockito.verify(mockEventHandlers)
        .dispatch(
            LogEvent.info(
                "Cannot verify server at https://apiRouteBase/api. Attempting again with no TLS verification."));
    Mockito.verify(mockEventHandlers)
        .dispatch(
            LogEvent.info(
                "Failed to connect to https://apiRouteBase/api over HTTPS. Attempting again with HTTP: http://apiRouteBase/api"));
  }

  [TestMethod]
  public void testCall_insecureCallerOnHttpServerAndNoPortSpecified()
      {
    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(ConnectException))) // server is not listening on 443
        .thenReturn(mockResponse); // respond when connected through 80

    RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
    Assert.assertEquals("body", insecureEndpointCaller.call());

    ArgumentCaptor<Uri> urlCaptor = ArgumentCaptor.forClass(typeof(Uri));
    Mockito.verify(mockConnectionFactory, Mockito.times(2)).apply(urlCaptor.capture());
    Assert.assertEquals(new Uri("https://apiRouteBase/api"), urlCaptor.getAllValues().get(0));
    Assert.assertEquals(new Uri("http://apiRouteBase/api"), urlCaptor.getAllValues().get(1));

    Mockito.verifyNoMoreInteractions(mockConnectionFactory);
    Mockito.verifyNoMoreInteractions(mockInsecureConnectionFactory);

    Mockito.verify(mockEventHandlers)
        .dispatch(
            LogEvent.info(
                "Failed to connect to https://apiRouteBase/api over HTTPS. Attempting again with HTTP: http://apiRouteBase/api"));
  }

  [TestMethod]
  public void testCall_secureCallerOnNonListeningServerAndNoPortSpecified()
      {
    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(ConnectException))); // server is not listening on 443

    try {
      secureEndpointCaller.call();
      Assert.fail("Should not fall back to HTTP if not allowInsecureRegistries");
    } catch (ConnectException ex) {
      Assert.assertNull(ex.getMessage());
    }

    ArgumentCaptor<Uri> urlCaptor = ArgumentCaptor.forClass(typeof(Uri));
    Mockito.verify(mockConnectionFactory).apply(urlCaptor.capture());
    Assert.assertEquals(new Uri("https://apiRouteBase/api"), urlCaptor.getAllValues().get(0));

    Mockito.verifyNoMoreInteractions(mockConnectionFactory);
    Mockito.verifyNoMoreInteractions(mockInsecureConnectionFactory);
  }

  [TestMethod]
  public void testCall_insecureCallerOnNonListeningServerAndPortSpecified()
      {
    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(ConnectException))); // server is not listening on 5000

    RegistryEndpointCaller<string> insecureEndpointCaller =
        createRegistryEndpointCaller(true, 5000);
    try {
      insecureEndpointCaller.call();
      Assert.fail("Should not fall back to HTTP if port was explicitly given and cannot connect");
    } catch (ConnectException ex) {
      Assert.assertNull(ex.getMessage());
    }

    ArgumentCaptor<Uri> urlCaptor = ArgumentCaptor.forClass(typeof(Uri));
    Mockito.verify(mockConnectionFactory).apply(urlCaptor.capture());
    Assert.assertEquals(new Uri("https://apiRouteBase:5000/api"), urlCaptor.getAllValues().get(0));

    Mockito.verifyNoMoreInteractions(mockConnectionFactory);
    Mockito.verifyNoMoreInteractions(mockInsecureConnectionFactory);
  }

  [TestMethod]
  public void testCall_noHttpResponse() {
    NoHttpResponseException mockNoHttpResponseException =
        Mockito.mock(typeof(NoHttpResponseException));
    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(mockNoHttpResponseException);

    try {
      secureEndpointCaller.call();
      Assert.fail("Call should have failed");

    } catch (RegistryNoResponseException ex) {
      Assert.assertSame(mockNoHttpResponseException, ex.getCause());
    }
  }

  [TestMethod]
  public void testCall_unauthorized() {
    verifyThrowsRegistryUnauthorizedException(HttpStatusCodes.STATUS_CODE_UNAUTHORIZED);
  }

  [TestMethod]
  public void testCall_credentialsNotSentOverHttp() {
    HttpResponse redirectResponse = mockRedirectHttpResponse("http://newlocation");
    HttpResponse unauthroizedResponse =
        mockHttpResponse(HttpStatusCodes.STATUS_CODE_UNAUTHORIZED, null);

    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(SSLPeerUnverifiedException))) // server is not HTTPS
        .thenThrow(new HttpResponseException(redirectResponse)) // redirect to HTTP
        .thenThrow(new HttpResponseException(unauthroizedResponse)); // final response
    Mockito.when(mockInsecureConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(SSLPeerUnverifiedException))); // server is not HTTPS

    RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
    try {
      insecureEndpointCaller.call();
      Assert.fail("Call should have failed");

    } catch (RegistryCredentialsNotSentException ex) {
      Assert.assertEquals(
          "Required credentials for serverUrl/imageName were not sent because the connection was over HTTP",
          ex.getMessage());
    }
  }

  [TestMethod]
  public void testCall_credentialsForcedOverHttp() {
    HttpResponse redirectResponse = mockRedirectHttpResponse("http://newlocation");

    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(SSLPeerUnverifiedException))) // server is not HTTPS
        .thenThrow(new HttpResponseException(redirectResponse)) // redirect to HTTP
        .thenReturn(mockResponse); // final response
    Mockito.when(mockInsecureConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(Mockito.mock(typeof(SSLPeerUnverifiedException))); // server is not HTTPS

    System.setProperty(JibSystemProperties.SEND_CREDENTIALS_OVER_HTTP, "true");
    RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
    Assert.assertEquals("body", insecureEndpointCaller.call());

    ArgumentCaptor<Uri> urlCaptor = ArgumentCaptor.forClass(typeof(Uri));
    Mockito.verify(mockConnectionFactory, Mockito.times(3)).apply(urlCaptor.capture());
    Assert.assertEquals(new Uri("https://apiRouteBase/api"), urlCaptor.getAllValues().get(0));
    Assert.assertEquals(new Uri("http://apiRouteBase/api"), urlCaptor.getAllValues().get(1));
    Assert.assertEquals(new Uri("http://newlocation"), urlCaptor.getAllValues().get(2));

    Mockito.verify(mockInsecureConnectionFactory).apply(urlCaptor.capture());
    Assert.assertEquals(new Uri("https://apiRouteBase/api"), urlCaptor.getAllValues().get(3));

    Mockito.verifyNoMoreInteractions(mockConnectionFactory);
    Mockito.verifyNoMoreInteractions(mockInsecureConnectionFactory);

    Mockito.verify(mockEventHandlers)
        .dispatch(
            LogEvent.info(
                "Cannot verify server at https://apiRouteBase/api. Attempting again with no TLS verification."));
    Mockito.verify(mockEventHandlers)
        .dispatch(
            LogEvent.info(
                "Failed to connect to https://apiRouteBase/api over HTTPS. Attempting again with HTTP: http://apiRouteBase/api"));
  }

  [TestMethod]
  public void testCall_forbidden() {
    verifyThrowsRegistryUnauthorizedException(HttpStatusCodes.STATUS_CODE_FORBIDDEN);
  }

  [TestMethod]
  public void testCall_badRequest() {
    verifyThrowsRegistryErrorException(HttpStatusCodes.STATUS_CODE_BAD_REQUEST);
  }

  [TestMethod]
  public void testCall_notFound() {
    verifyThrowsRegistryErrorException(HttpStatusCodes.STATUS_CODE_NOT_FOUND);
  }

  [TestMethod]
  public void testCall_methodNotAllowed() {
    verifyThrowsRegistryErrorException(HttpStatusCodes.STATUS_CODE_METHOD_NOT_ALLOWED);
  }

  [TestMethod]
  public void testCall_unknown() {
    HttpResponse mockHttpResponse =
        mockHttpResponse(HttpStatusCodes.STATUS_CODE_SERVER_ERROR, null);
    HttpResponseException httpResponseException = new HttpResponseException(mockHttpResponse);

    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(httpResponseException);

    try {
      secureEndpointCaller.call();
      Assert.fail("Call should have failed");

    } catch (HttpResponseException ex) {
      Assert.assertSame(httpResponseException, ex);
    }
  }

  [TestMethod]
  public void testCall_temporaryRedirect() {
    verifyRetriesWithNewLocation(HttpStatusCodes.STATUS_CODE_TEMPORARY_REDIRECT);
  }

  [TestMethod]
  public void testCall_movedPermanently() {
    verifyRetriesWithNewLocation(HttpStatusCodes.STATUS_CODE_MOVED_PERMANENTLY);
  }

  [TestMethod]
  public void testCall_permanentRedirect() {
    verifyRetriesWithNewLocation(RegistryEndpointCaller.STATUS_CODE_PERMANENT_REDIRECT);
  }

  [TestMethod]
  public void testCall_disallowInsecure() {
    // Mocks a response for temporary redirect to a new location.
    HttpResponse redirectResponse = mockRedirectHttpResponse("http://newlocation");

    HttpResponseException redirectException = new HttpResponseException(redirectResponse);
    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(redirectException);

    try {
      secureEndpointCaller.call();
      Assert.fail("Call should have failed");

    } catch (InsecureRegistryException ex) {
      // pass
    }
  }

  [TestMethod]
  public void testHttpTimeout_propertyNotSet() {
    MockConnection mockConnection = new MockConnection((httpMethod, request) => mockResponse);
    Mockito.when(mockConnectionFactory.apply(Mockito.any())).thenReturn(mockConnection);

    Assert.assertNull(System.getProperty(JibSystemProperties.HTTP_TIMEOUT));
    secureEndpointCaller.call();

    // We fall back to the default timeout:
    // https://github.com/GoogleContainerTools/jib/pull/656#discussion_r203562639
    Assert.assertEquals(20000, mockConnection.getRequestedHttpTimeout().intValue());
  }

  [TestMethod]
  public void testHttpTimeout_stringValue() {
    MockConnection mockConnection = new MockConnection((httpMethod, request) => mockResponse);
    Mockito.when(mockConnectionFactory.apply(Mockito.any())).thenReturn(mockConnection);

    System.setProperty(JibSystemProperties.HTTP_TIMEOUT, "random string");
    secureEndpointCaller.call();

    Assert.assertEquals(20000, mockConnection.getRequestedHttpTimeout().intValue());
  }

  [TestMethod]
  public void testHttpTimeout_negativeValue() {
    MockConnection mockConnection = new MockConnection((httpMethod, request) => mockResponse);
    Mockito.when(mockConnectionFactory.apply(Mockito.any())).thenReturn(mockConnection);

    System.setProperty(JibSystemProperties.HTTP_TIMEOUT, "-1");
    secureEndpointCaller.call();

    // We let the negative value pass through:
    // https://github.com/GoogleContainerTools/jib/pull/656#discussion_r203562639
    Assert.assertEquals(Integer.valueOf(-1), mockConnection.getRequestedHttpTimeout());
  }

  [TestMethod]
  public void testHttpTimeout_0accepted() {
    System.setProperty(JibSystemProperties.HTTP_TIMEOUT, "0");

    MockConnection mockConnection = new MockConnection((httpMethod, request) => mockResponse);
    Mockito.when(mockConnectionFactory.apply(Mockito.any())).thenReturn(mockConnection);

    secureEndpointCaller.call();

    Assert.assertEquals(Integer.valueOf(0), mockConnection.getRequestedHttpTimeout());
  }

  [TestMethod]
  public void testHttpTimeout() {
    System.setProperty(JibSystemProperties.HTTP_TIMEOUT, "7593");

    MockConnection mockConnection = new MockConnection((httpMethod, request) => mockResponse);
    Mockito.when(mockConnectionFactory.apply(Mockito.any())).thenReturn(mockConnection);

    secureEndpointCaller.call();

    Assert.assertEquals(Integer.valueOf(7593), mockConnection.getRequestedHttpTimeout());
  }

  [TestMethod]
  public void testIsBrokenPipe_notBrokenPipe() {
    Assert.assertFalse(RegistryEndpointCaller.isBrokenPipe(new IOException()));
    Assert.assertFalse(RegistryEndpointCaller.isBrokenPipe(new SocketException()));
    Assert.assertFalse(RegistryEndpointCaller.isBrokenPipe(new SSLException("mock")));
  }

  [TestMethod]
  public void testIsBrokenPipe_brokenPipe() {
    Assert.assertTrue(RegistryEndpointCaller.isBrokenPipe(new IOException("cool broken pipe !")));
    Assert.assertTrue(RegistryEndpointCaller.isBrokenPipe(new SocketException("BROKEN PIPE")));
    Assert.assertTrue(RegistryEndpointCaller.isBrokenPipe(new SSLException("calm BrOkEn PiPe")));
  }

  [TestMethod]
  public void testIsBrokenPipe_nestedBrokenPipe() {
    IOException exception = new IOException(new SSLException(new SocketException("Broken pipe")));
    Assert.assertTrue(RegistryEndpointCaller.isBrokenPipe(exception));
  }

  [TestMethod]
  public void testIsBrokenPipe_terminatesWhenCauseIsOriginal() {
    IOException exception = Mockito.mock(typeof(IOException));
    Mockito.when(exception.getCause()).thenReturn(exception);

    Assert.assertFalse(RegistryEndpointCaller.isBrokenPipe(exception));
  }

  [TestMethod]
  public void testNewRegistryErrorException_jsonErrorOutput() {
    HttpResponseException httpException = Mockito.mock(typeof(HttpResponseException));
    Mockito.when(httpException.getContent())
        .thenReturn(
            "{\"errors\": [{\"code\": \"MANIFEST_UNKNOWN\", \"message\": \"manifest unknown\"}]}");

    RegistryErrorException registryException =
        secureEndpointCaller.newRegistryErrorException(httpException);
    Assert.assertSame(httpException, registryException.getCause());
    Assert.assertEquals(
        "Tried to actionDescription but failed because: manifest unknown | If this is a bug, "
            + "please file an issue at https://github.com/GoogleContainerTools/jib/issues/new",
        registryException.getMessage());
  }

  [TestMethod]
  public void testNewRegistryErrorException_nonJsonErrorOutput() {
    HttpResponseException httpException = Mockito.mock(typeof(HttpResponseException));
    // Registry returning non-structured error output
    Mockito.when(httpException.getContent()).thenReturn(">>>>> (404) page not found <<<<<");
    Mockito.when(httpException.getStatusCode()).thenReturn(404);

    RegistryErrorException registryException =
        secureEndpointCaller.newRegistryErrorException(httpException);
    Assert.assertSame(httpException, registryException.getCause());
    Assert.assertEquals(
        "Tried to actionDescription but failed because: registry returned error code 404; "
            + "possible causes include invalid or wrong reference. Actual error output follows:\n"
            + ">>>>> (404) page not found <<<<<\n"
            + " | If this is a bug, please file an issue at "
            + "https://github.com/GoogleContainerTools/jib/issues/new",
        registryException.getMessage());
  }

  /**
   * Verifies that a response with {@code httpStatusCode} throws {@link
   * RegistryUnauthorizedException}.
   */
  private void verifyThrowsRegistryUnauthorizedException(int httpStatusCode)
      {
    HttpResponse mockHttpResponse = mockHttpResponse(httpStatusCode, null);
    HttpResponseException httpResponseException = new HttpResponseException(mockHttpResponse);

    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(httpResponseException);

    try {
      secureEndpointCaller.call();
      Assert.fail("Call should have failed");

    } catch (RegistryUnauthorizedException ex) {
      Assert.assertEquals("serverUrl", ex.getRegistry());
      Assert.assertEquals("imageName", ex.getRepository());
      Assert.assertSame(httpResponseException, ex.getHttpResponseException());
    }
  }

  /**
   * Verifies that a response with {@code httpStatusCode} throws {@link
   * RegistryUnauthorizedException}.
   */
  private void verifyThrowsRegistryErrorException(int httpStatusCode)
      {
    HttpResponse errorResponse = mockHttpResponse(httpStatusCode, null);
    Mockito.when(errorResponse.parseAsString())
        .thenReturn("{\"errors\":[{\"code\":\"code\",\"message\":\"message\"}]}");
    HttpResponseException httpResponseException = new HttpResponseException(errorResponse);

    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(httpResponseException);

    try {
      secureEndpointCaller.call();
      Assert.fail("Call should have failed");

    } catch (RegistryErrorException ex) {
      Assert.assertThat(
          ex.getMessage(),
          CoreMatchers.containsString(
              "Tried to actionDescription but failed because: unknown: message"));
    }
  }

  /**
   * Verifies that a response with {@code httpStatusCode} retries the request with the {@code
   * Location} header.
   */
  private void verifyRetriesWithNewLocation(int httpStatusCode)
      {
    // Mocks a response for temporary redirect to a new location.
    HttpResponse redirectResponse =
        mockHttpResponse(httpStatusCode, new HttpHeaders().setLocation("https://newlocation"));

    // Has mockConnection.send throw first, then succeed.
    HttpResponseException redirectException = new HttpResponseException(redirectResponse);
    Mockito.when(mockConnection.send(Mockito.eq("httpMethod"), Mockito.any()))
        .thenThrow(redirectException)
        .thenReturn(mockResponse);

    Assert.assertEquals("body", secureEndpointCaller.call());

    // Checks that the Uri was changed to the new location.
    ArgumentCaptor<Uri> urlArgumentCaptor = ArgumentCaptor.forClass(typeof(Uri));
    Mockito.verify(mockConnectionFactory, Mockito.times(2)).apply(urlArgumentCaptor.capture());
    Assert.assertEquals(
        new Uri("https://apiRouteBase/api"), urlArgumentCaptor.getAllValues().get(0));
    Assert.assertEquals(new Uri("https://newlocation"), urlArgumentCaptor.getAllValues().get(1));

    Mockito.verifyNoMoreInteractions(mockConnectionFactory);
    Mockito.verifyNoMoreInteractions(mockInsecureConnectionFactory);
  }

  private RegistryEndpointCaller<string> createRegistryEndpointCaller(
      bool allowInsecure, int port) {
    return new RegistryEndpointCaller<>(
        mockEventHandlers,
        "userAgent",
        (port == -1) ? "apiRouteBase" : ("apiRouteBase:" + port),
        new TestRegistryEndpointProvider(),
        Authorization.fromBasicToken("token"),
        new RegistryEndpointRequestProperties("serverUrl", "imageName"),
        allowInsecure,
        mockConnectionFactory,
        mockInsecureConnectionFactory);
  }
}
}
