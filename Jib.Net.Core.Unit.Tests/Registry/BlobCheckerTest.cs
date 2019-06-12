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
using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;

namespace com.google.cloud.tools.jib.registry {





















/** Tests for {@link BlobChecker}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class BlobCheckerTest {

  private HttpResponseMessage mockResponse = Mock.Of<HttpResponseMessage>();

  private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
      new RegistryEndpointRequestProperties("someServerUrl", "someImageName");

  private BlobChecker testBlobChecker;
  private DescriptorDigest fakeDigest;

  [SetUp]
  public void setUpFakes() {
    fakeDigest =
        DescriptorDigest.fromHash(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    testBlobChecker = new BlobChecker(fakeRegistryEndpointRequestProperties, fakeDigest);
  }

  [Test]
  public void testHandleResponse() {
    Mock.Get(mockResponse).Setup(m => m.getContentLength()).Returns(0L);

    BlobDescriptor expectedBlobDescriptor = new BlobDescriptor(0, fakeDigest);

    BlobDescriptor blobDescriptor = testBlobChecker.handleResponse(mockResponse);

    Assert.AreEqual(expectedBlobDescriptor, blobDescriptor);
  }

  [Test]
  public void testHandleResponse_noContentLength() {
    Mock.Get(mockResponse).Setup(m => m.getContentLength()).Returns(-1L);

    try {
      testBlobChecker.handleResponse(mockResponse);
      Assert.Fail("Should throw exception if Content-Length header is not present");

    } catch (RegistryErrorException ex) {
                StringAssert.Contains(
                    ex.getMessage(), "Did not receive Content-Length header");
    }
  }

  [Test]
  public void testHandleHttpResponseException() {
    HttpResponseMessage mockHttpResponseException = Mock.Of<HttpResponseMessage>();
    Mock.Get(mockHttpResponseException).Setup(m => m.getStatusCode()).Returns(HttpStatusCode.NotFound);

    ErrorResponseTemplate emptyErrorResponseTemplate =
        new ErrorResponseTemplate()
            .addError(new ErrorEntryTemplate(ErrorCodes.BLOB_UNKNOWN.name(), "some message"));
    Mock.Get(mockHttpResponseException).Setup(m => m.getContent()).Returns(JsonTemplateMapper.toUtf8String(emptyErrorResponseTemplate));

    BlobDescriptor blobDescriptor =
        testBlobChecker.handleHttpResponseException(mockHttpResponseException);

    Assert.IsNull(blobDescriptor);
  }

  [Test]
  public void testHandleHttpResponseException_hasOtherErrors()
      {
    HttpResponseMessage mockHttpResponseException = Mock.Of<HttpResponseMessage>();
    Mock.Get(mockHttpResponseException).Setup(m => m.getStatusCode()).Returns(HttpStatusCode.NotFound);

    ErrorResponseTemplate emptyErrorResponseTemplate =
        new ErrorResponseTemplate()
            .addError(new ErrorEntryTemplate(ErrorCodes.BLOB_UNKNOWN.name(), "some message"))
            .addError(new ErrorEntryTemplate(ErrorCodes.MANIFEST_UNKNOWN.name(), "some message"));
    Mock.Get(mockHttpResponseException).Setup(m => m.getContent()).Returns(JsonTemplateMapper.toUtf8String(emptyErrorResponseTemplate));

    try {
      testBlobChecker.handleHttpResponseException(mockHttpResponseException);
      Assert.Fail("Non-BLOB_UNKNOWN errors should not be handled");

    } catch (HttpResponseException ex) {
      Assert.AreEqual(mockHttpResponseException, ex);
    }
  }

  [Test]
  public void testHandleHttpResponseException_notBlobUnknown()
      {
            HttpResponseMessage mockHttpResponseException = Mock.Of<HttpResponseMessage>();
    Mock.Get(mockHttpResponseException).Setup(m => m.getStatusCode()).Returns(HttpStatusCode.NotFound);

    ErrorResponseTemplate emptyErrorResponseTemplate = new ErrorResponseTemplate();
    Mock.Get(mockHttpResponseException).Setup(m => m.getContent()).Returns(JsonTemplateMapper.toUtf8String(emptyErrorResponseTemplate));

    try {
      testBlobChecker.handleHttpResponseException(mockHttpResponseException);
      Assert.Fail("Non-BLOB_UNKNOWN errors should not be handled");

    } catch (HttpResponseException ex) {
      Assert.AreEqual(mockHttpResponseException, ex);
    }
  }

  [Test]
  public void testHandleHttpResponseException_invalidStatusCode() {
            HttpResponseMessage mockHttpResponseException = Mock.Of<HttpResponseMessage>();
    Mock.Get(mockHttpResponseException).Setup(m => m.getStatusCode()).Returns((HttpStatusCode)(-1));

    try {
      testBlobChecker.handleHttpResponseException(mockHttpResponseException);
      Assert.Fail("Non-404 status codes should not be handled");

    } catch (HttpResponseException ex) {
      Assert.AreEqual(mockHttpResponseException, ex);
    }
  }

  [Test]
  public void testGetApiRoute() {
    Assert.AreEqual(
        new Uri("http://someApiBase/someImageName/blobs/" + fakeDigest),
        testBlobChecker.getApiRoute("http://someApiBase/"));
  }

  [Test]
  public void testGetContent() {
    Assert.IsNull(testBlobChecker.getContent());
  }

  [Test]
  public void testGetAccept() {
    Assert.AreEqual(0, testBlobChecker.getAccept().size());
  }

  [Test]
  public void testGetActionDescription() {
    Assert.AreEqual(
        "check BLOB exists for someServerUrl/someImageName with digest " + fakeDigest,
        testBlobChecker.getActionDescription());
  }

  [Test]
  public void testGetHttpMethod() {
    Assert.AreEqual("HEAD", testBlobChecker.getHttpMethod());
  }
}
}
