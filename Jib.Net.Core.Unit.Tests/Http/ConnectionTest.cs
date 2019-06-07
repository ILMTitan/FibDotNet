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

namespace com.google.cloud.tools.jib.http {





















/** Tests for {@link Connection}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class ConnectionTest {

  @FunctionalInterface
  private interface SendFunction {

    Response send(Connection connection, Request request) {
    setUpMocksAndFakes(null);
    testSend(HttpMethods.GET, Connection::get);
  }

  [TestMethod]
  public void testPost() {
    setUpMocksAndFakes(null);
    testSend(HttpMethods.POST, Connection::post);
  }

  [TestMethod]
  public void testPut() {
    setUpMocksAndFakes(null);
    testSend(HttpMethods.PUT, Connection.put);
  }

  [TestMethod]
  public void testHttpTimeout_doNotSetByDefault() {
    setUpMocksAndFakes(null);
    using (Connection connection = testConnection) {
      connection.send(HttpMethods.GET, fakeRequest);
    }

    Mockito.verify(mockHttpRequest, Mockito.never()).setConnectTimeout(Mockito.anyInt());
    Mockito.verify(mockHttpRequest, Mockito.never()).setReadTimeout(Mockito.anyInt());
  }

  [TestMethod]
  public void testHttpTimeout() {
    setUpMocksAndFakes(5982);
    using (Connection connection = testConnection) {
      connection.send(HttpMethods.GET, fakeRequest);
    }

    Mockito.verify(mockHttpRequest).setConnectTimeout(5982);
    Mockito.verify(mockHttpRequest).setReadTimeout(5982);
  }

  private void setUpMocksAndFakes(Integer httpTimeout) {
    fakeRequest =
        Request.builder()
            .setAccept(Arrays.asList("fake.accept", "another.fake.accept"))
            .setUserAgent("fake user agent")
            .setBody(
                new BlobHttpContent(
                    Blobs.from("crepecake"), "fake.content.type", totalByteCount::add))
            .setAuthorization(Authorization.fromBasicCredentials("fake-username", "fake-secret"))
            .setHttpTimeout(httpTimeout)
            .build();

    Mockito.when(
            mockHttpRequestFactory.buildRequest(
                Mockito.any(typeof(string)), Mockito.eq(fakeUrl), Mockito.any(typeof(BlobHttpContent))))
        .thenReturn(mockHttpRequest);

    Mockito.when(mockHttpRequest.setHeaders(Mockito.any(typeof(HttpHeaders))))
        .thenReturn(mockHttpRequest);
    if (httpTimeout != null) {
      Mockito.when(mockHttpRequest.setConnectTimeout(Mockito.anyInt())).thenReturn(mockHttpRequest);
      Mockito.when(mockHttpRequest.setReadTimeout(Mockito.anyInt())).thenReturn(mockHttpRequest);
    }
    mockHttpResponse = Mockito.mock(typeof(HttpResponse));
    Mockito.when(mockHttpRequest.execute()).thenReturn(mockHttpResponse);
  }

  private void testSend(string httpMethod, SendFunction sendFunction) {
    using (Connection connection = testConnection) {
      sendFunction.send(connection, fakeRequest);
    }

    Mockito.verify(mockHttpRequest).setHeaders(httpHeadersArgumentCaptor.capture());
    Mockito.verify(mockHttpResponse).disconnect();

    Assert.assertEquals(
        "fake.accept,another.fake.accept", httpHeadersArgumentCaptor.getValue().getAccept());
    Assert.assertEquals("fake user agent", httpHeadersArgumentCaptor.getValue().getUserAgent());
    // Base64 representation of "fake-username:fake-secret"
    Assert.assertEquals(
        "Basic ZmFrZS11c2VybmFtZTpmYWtlLXNlY3JldA==",
        httpHeadersArgumentCaptor.getValue().getAuthorization());

    Mockito.verify(mockHttpRequestFactory)
        .buildRequest(
            Mockito.eq(httpMethod), Mockito.eq(fakeUrl), blobHttpContentArgumentCaptor.capture());
    Assert.assertEquals("fake.content.type", blobHttpContentArgumentCaptor.getValue().getType());

    ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
    blobHttpContentArgumentCaptor.getValue().writeTo(byteArrayOutputStream);

    Assert.assertEquals(
        "crepecake", new string(byteArrayOutputStream.toByteArray(), StandardCharsets.UTF_8));
    Assert.assertEquals("crepecake".length(), totalByteCount.longValue());
  }
}
}
