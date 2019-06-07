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




















/** Tests for {@link BlobPuller}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class BlobPullerTest {

  private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
      new RegistryEndpointRequestProperties("someServerUrl", "someImageName");
  private DescriptorDigest fakeDigest;

  private readonly ByteArrayOutputStream layerContentOutputStream = new ByteArrayOutputStream();
  private readonly CountingDigestOutputStream layerOutputStream =
      new CountingDigestOutputStream(layerContentOutputStream);

  private BlobPuller testBlobPuller;

  [TestInitialize]
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

  [TestMethod]
  public void testHandleResponse() {
    InputStream blobContent =
        new ByteArrayInputStream("some BLOB content".getBytes(StandardCharsets.UTF_8));
    DescriptorDigest testBlobDigest = Digests.computeDigest(blobContent).getDigest();
    blobContent.reset();

    Response mockResponse = Mockito.mock(typeof(Response));
    Mockito.when(mockResponse.getContentLength()).thenReturn((long) "some BLOB content".length());
    Mockito.when(mockResponse.getBody()).thenReturn(blobContent);

    LongAdder byteCount = new LongAdder();
    BlobPuller blobPuller =
        new BlobPuller(
            fakeRegistryEndpointRequestProperties,
            testBlobDigest,
            layerOutputStream,
            size => Assert.assertEquals("some BLOB content".length(), size.longValue()),
            byteCount.add);
    blobPuller.handleResponse(mockResponse);
    Assert.assertEquals(
        "some BLOB content",
        new string(layerContentOutputStream.toByteArray(), StandardCharsets.UTF_8));
    Assert.assertEquals(testBlobDigest, layerOutputStream.computeDigest().getDigest());
    Assert.assertEquals("some BLOB content".length(), byteCount.sum());
  }

  [TestMethod]
  public void testHandleResponse_unexpectedDigest() {
    InputStream blobContent =
        new ByteArrayInputStream("some BLOB content".getBytes(StandardCharsets.UTF_8));
    DescriptorDigest testBlobDigest = Digests.computeDigest(blobContent).getDigest();
    blobContent.reset();

    Response mockResponse = Mockito.mock(typeof(Response));
    Mockito.when(mockResponse.getBody()).thenReturn(blobContent);

    try {
      testBlobPuller.handleResponse(mockResponse);
      Assert.fail("Receiving an unexpected digest should fail");

    } catch (UnexpectedBlobDigestException ex) {
      Assert.assertEquals(
          "The pulled BLOB has digest '"
              + testBlobDigest
              + "', but the request digest was '"
              + fakeDigest
              + "'",
          ex.getMessage());
    }
  }

  [TestMethod]
  public void testGetApiRoute() {
    Assert.assertEquals(
        new Uri("http://someApiBase/someImageName/blobs/" + fakeDigest),
        testBlobPuller.getApiRoute("http://someApiBase/"));
  }

  [TestMethod]
  public void testGetActionDescription() {
    Assert.assertEquals(
        "pull BLOB for someServerUrl/someImageName with digest " + fakeDigest,
        testBlobPuller.getActionDescription());
  }

  [TestMethod]
  public void testGetHttpMethod() {
    Assert.assertEquals("GET", testBlobPuller.getHttpMethod());
  }

  [TestMethod]
  public void testGetContent() {
    Assert.assertNull(testBlobPuller.getContent());
  }

  [TestMethod]
  public void testGetAccept() {
    Assert.assertEquals(0, testBlobPuller.getAccept().size());
  }
}
}
