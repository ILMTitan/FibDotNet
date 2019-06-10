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





















/** Tests for {@link BlobChecker}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class BlobCheckerTest {

  [Mock] private HttpResponseMessage mockResponse;

  private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
      new RegistryEndpointRequestProperties("someServerUrl", "someImageName");

  private BlobChecker testBlobChecker;
  private DescriptorDigest fakeDigest;

  [TestInitialize]
  public void setUpFakes() {
    fakeDigest =
        DescriptorDigest.fromHash(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    testBlobChecker = new BlobChecker(fakeRegistryEndpointRequestProperties, fakeDigest);
  }

  [TestMethod]
  public void testHandleResponse() {
    Mockito.when(mockResponse.getContentLength()).thenReturn(0L);
    BlobDescriptor expectedBlobDescriptor = new BlobDescriptor(0, fakeDigest);

    BlobDescriptor blobDescriptor = testBlobChecker.handleResponse(mockResponse);

    Assert.assertEquals(expectedBlobDescriptor, blobDescriptor);
  }

  [TestMethod]
  public void testHandleResponse_noContentLength() {
    Mockito.when(mockResponse.getContentLength()).thenReturn(-1L);

    try {
      testBlobChecker.handleResponse(mockResponse);
      Assert.fail("Should throw exception if Content-Length header is not present");

    } catch (RegistryErrorException ex) {
      Assert.assertThat(
          ex.getMessage(), CoreMatchers.containsString("Did not receive Content-Length header"));
    }
  }

  [TestMethod]
  public void testHandleHttpResponseException() {
    HttpResponseException mockHttpResponseException = Mockito.mock(typeof(HttpResponseException));
    Mockito.when(mockHttpResponseException.getStatusCode())
        .thenReturn(HttpStatusCode.NotFound);

    ErrorResponseTemplate emptyErrorResponseTemplate =
        new ErrorResponseTemplate()
            .addError(new ErrorEntryTemplate(ErrorCodes.BLOB_UNKNOWN.name(), "some message"));
    Mockito.when(mockHttpResponseException.getContent())
        .thenReturn(JsonTemplateMapper.toUtf8String(emptyErrorResponseTemplate));

    BlobDescriptor blobDescriptor =
        testBlobChecker.handleHttpResponseException(mockHttpResponseException);

    Assert.assertNull(blobDescriptor);
  }

  [TestMethod]
  public void testHandleHttpResponseException_hasOtherErrors()
      {
    HttpResponseException mockHttpResponseException = Mockito.mock(typeof(HttpResponseException));
    Mockito.when(mockHttpResponseException.getStatusCode())
        .thenReturn(HttpStatusCode.NotFound);

    ErrorResponseTemplate emptyErrorResponseTemplate =
        new ErrorResponseTemplate()
            .addError(new ErrorEntryTemplate(ErrorCodes.BLOB_UNKNOWN.name(), "some message"))
            .addError(new ErrorEntryTemplate(ErrorCodes.MANIFEST_UNKNOWN.name(), "some message"));
    Mockito.when(mockHttpResponseException.getContent())
        .thenReturn(JsonTemplateMapper.toUtf8String(emptyErrorResponseTemplate));

    try {
      testBlobChecker.handleHttpResponseException(mockHttpResponseException);
      Assert.fail("Non-BLOB_UNKNOWN errors should not be handled");

    } catch (HttpResponseException ex) {
      Assert.assertEquals(mockHttpResponseException, ex);
    }
  }

  [TestMethod]
  public void testHandleHttpResponseException_notBlobUnknown()
      {
    HttpResponseException mockHttpResponseException = Mockito.mock(typeof(HttpResponseException));
    Mockito.when(mockHttpResponseException.getStatusCode())
        .thenReturn(HttpStatusCode.NotFound);

    ErrorResponseTemplate emptyErrorResponseTemplate = new ErrorResponseTemplate();
    Mockito.when(mockHttpResponseException.getContent())
        .thenReturn(JsonTemplateMapper.toUtf8String(emptyErrorResponseTemplate));

    try {
      testBlobChecker.handleHttpResponseException(mockHttpResponseException);
      Assert.fail("Non-BLOB_UNKNOWN errors should not be handled");

    } catch (HttpResponseException ex) {
      Assert.assertEquals(mockHttpResponseException, ex);
    }
  }

  [TestMethod]
  public void testHandleHttpResponseException_invalidStatusCode() {
    HttpResponseException mockHttpResponseException = Mockito.mock(typeof(HttpResponseException));
    Mockito.when(mockHttpResponseException.getStatusCode()).thenReturn(-1);

    try {
      testBlobChecker.handleHttpResponseException(mockHttpResponseException);
      Assert.fail("Non-404 status codes should not be handled");

    } catch (HttpResponseException ex) {
      Assert.assertEquals(mockHttpResponseException, ex);
    }
  }

  [TestMethod]
  public void testGetApiRoute() {
    Assert.assertEquals(
        new Uri("http://someApiBase/someImageName/blobs/" + fakeDigest),
        testBlobChecker.getApiRoute("http://someApiBase/"));
  }

  [TestMethod]
  public void testGetContent() {
    Assert.assertNull(testBlobChecker.getContent());
  }

  [TestMethod]
  public void testGetAccept() {
    Assert.assertEquals(0, testBlobChecker.getAccept().size());
  }

  [TestMethod]
  public void testGetActionDescription() {
    Assert.assertEquals(
        "check BLOB exists for someServerUrl/someImageName with digest " + fakeDigest,
        testBlobChecker.getActionDescription());
  }

  [TestMethod]
  public void testGetHttpMethod() {
    Assert.assertEquals("HEAD", testBlobChecker.getHttpMethod());
  }
}
}
