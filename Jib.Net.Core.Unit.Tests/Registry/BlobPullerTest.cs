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
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.hash;
using Jib.Net.Core.Global;
using Jib.Net.Core.Api;
using NUnit.Framework;
using System.IO;
using System.Net.Http;
using Moq;
using System;

namespace com.google.cloud.tools.jib.registry {




















/** Tests for {@link BlobPuller}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class BlobPullerTest {

  private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
      new RegistryEndpointRequestProperties("someServerUrl", "someImageName");
  private DescriptorDigest fakeDigest;

  private readonly MemoryStream layerContentOutputStream = new MemoryStream();
  private readonly CountingDigestOutputStream layerOutputStream;

  private BlobPuller testBlobPuller;
        public BlobPullerTest()
        {
            layerOutputStream =
      new CountingDigestOutputStream(layerContentOutputStream);
        }

  [SetUp]
  public void setUpFakes() {
    fakeDigest =
        DescriptorDigest.fromHash(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    testBlobPuller =
        new BlobPuller(
            fakeRegistryEndpointRequestProperties,
            fakeDigest,
            layerOutputStream,
            ignored => {},
            ignored => {});
  }

  [Test]
  public void testHandleResponse() {
            MemoryStream blobContent =
        new MemoryStream("some BLOB content".getBytes(StandardCharsets.UTF_8));
    DescriptorDigest testBlobDigest = Digests.computeDigest(blobContent).getDigest();
    blobContent.Position = 0;

    HttpResponseMessage mockResponse = Mock.Of<HttpResponseMessage>();
    Mock.Get(mockResponse).Setup(m => m.getContentLength()).Returns((long) "some BLOB content".length());

    Mock.Get(mockResponse).Setup(m => m.getBody()).Returns(blobContent);

    LongAdder byteCount = new LongAdder();
    BlobPuller blobPuller =
        new BlobPuller(
            fakeRegistryEndpointRequestProperties,
            testBlobDigest,
            layerOutputStream,
            size => Assert.AreEqual("some BLOB content".length(), size.longValue()),
            byteCount.add);
    blobPuller.handleResponse(mockResponse);
    Assert.AreEqual(
        "some BLOB content",
        StandardCharsets.UTF_8.GetString(layerContentOutputStream.toByteArray()));
    Assert.AreEqual(testBlobDigest, layerOutputStream.computeDigest().getDigest());
    Assert.AreEqual("some BLOB content".length(), byteCount.sum());
  }

  [Test]
  public void testHandleResponse_unexpectedDigest() {
            MemoryStream blobContent =
        new MemoryStream("some BLOB content".getBytes(StandardCharsets.UTF_8));
    DescriptorDigest testBlobDigest = Digests.computeDigest(blobContent).getDigest();
            blobContent.Position = 0;

    HttpResponseMessage mockResponse = Mock.Of<HttpResponseMessage>();
    Mock.Get(mockResponse).Setup(m => m.getBody()).Returns(blobContent);

    try {
      testBlobPuller.handleResponse(mockResponse);
      Assert.Fail("Receiving an unexpected digest should fail");

    } catch (UnexpectedBlobDigestException ex) {
      Assert.AreEqual(
          "The pulled BLOB has digest '"
              + testBlobDigest
              + "', but the request digest was '"
              + fakeDigest
              + "'",
          ex.getMessage());
    }
  }

  [Test]
  public void testGetApiRoute() {
    Assert.AreEqual(
        new Uri("http://someApiBase/someImageName/blobs/" + fakeDigest),
        testBlobPuller.getApiRoute("http://someApiBase/"));
  }

  [Test]
  public void testGetActionDescription() {
    Assert.AreEqual(
        "pull BLOB for someServerUrl/someImageName with digest " + fakeDigest,
        testBlobPuller.getActionDescription());
  }

  [Test]
  public void testGetHttpMethod() {
    Assert.AreEqual("GET", testBlobPuller.getHttpMethod());
  }

  [Test]
  public void testGetContent() {
    Assert.IsNull(testBlobPuller.getContent());
  }

  [Test]
  public void testGetAccept() {
    Assert.AreEqual(0, testBlobPuller.getAccept().size());
  }
}
}
