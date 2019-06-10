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

























/** Tests for {@link BlobPusher}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class BlobPusherTest {

  private static readonly string TEST_BLOB_CONTENT = "some BLOB content";
  private static readonly Blob TEST_BLOB = Blobs.from(TEST_BLOB_CONTENT);

  [Mock] private Uri mockURL;
  [Mock] private HttpResponseMessage mockResponse;

  private DescriptorDigest fakeDescriptorDigest;
  private BlobPusher testBlobPusher;

  [TestInitialize]
  public void setUpFakes() {
    fakeDescriptorDigest =
        DescriptorDigest.fromHash(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    testBlobPusher =
        new BlobPusher(
            new RegistryEndpointRequestProperties("someServerUrl", "someImageName"),
            fakeDescriptorDigest,
            TEST_BLOB,
            null);
  }

  [TestMethod]
  public void testInitializer_getContent() {
    Assert.assertNull(testBlobPusher.initializer().getContent());
  }

  [TestMethod]
  public void testGetAccept() {
    Assert.assertEquals(0, testBlobPusher.initializer().getAccept().size());
  }

  [TestMethod]
  public void testInitializer_handleResponse_created() {
    Mockito.when(mockResponse.getStatusCode()).thenReturn(201); // Created
    Assert.assertNull(testBlobPusher.initializer().handleResponse(mockResponse));
  }

  [TestMethod]
  public void testInitializer_handleResponse_accepted() {
    Mockito.when(mockResponse.getStatusCode()).thenReturn(202); // Accepted
    Mockito.when(mockResponse.getHeader("Location"))
        .thenReturn(Collections.singletonList("location"));
    GenericUrl requestUrl = new GenericUrl("https://someurl");
    Mockito.when(mockResponse.getRequestUrl()).thenReturn(requestUrl);
    Assert.assertEquals(
        new Uri("https://someurl/location"),
        testBlobPusher.initializer().handleResponse(mockResponse));
  }

  [TestMethod]
  public void testInitializer_handleResponse_accepted_multipleLocations()
      {
    Mockito.when(mockResponse.getStatusCode()).thenReturn(202); // Accepted
    Mockito.when(mockResponse.getHeader("Location"))
        .thenReturn(Arrays.asList("location1", "location2"));
    try {
      testBlobPusher.initializer().handleResponse(mockResponse);
      Assert.fail("Multiple 'Location' headers should be a registry error");

    } catch (RegistryErrorException ex) {
      Assert.assertThat(
          ex.getMessage(),
          CoreMatchers.containsString("Expected 1 'Location' header, but found 2"));
    }
  }

  [TestMethod]
  public void testInitializer_handleResponse_unrecognized() {
    Mockito.when(mockResponse.getStatusCode()).thenReturn(-1); // Unrecognized
    try {
      testBlobPusher.initializer().handleResponse(mockResponse);
      Assert.fail("Multiple 'Location' headers should be a registry error");

    } catch (RegistryErrorException ex) {
      Assert.assertThat(
          ex.getMessage(), CoreMatchers.containsString("Received unrecognized status code -1"));
    }
  }

  [TestMethod]
  public void testInitializer_getApiRoute_nullSource() {
    Assert.assertEquals(
        new Uri("http://someApiBase/someImageName/blobs/uploads/"),
        testBlobPusher.initializer().getApiRoute("http://someApiBase/"));
  }

  [TestMethod]
  public void testInitializer_getApiRoute_sameSource() {
    testBlobPusher =
        new BlobPusher(
            new RegistryEndpointRequestProperties("someServerUrl", "someImageName"),
            fakeDescriptorDigest,
            TEST_BLOB,
            "sourceImageName");

    Assert.assertEquals(
        new Uri(
            "http://someApiBase/someImageName/blobs/uploads/?mount="
                + fakeDescriptorDigest
                + "&from=sourceImageName"),
        testBlobPusher.initializer().getApiRoute("http://someApiBase/"));
  }

  [TestMethod]
  public void testInitializer_getHttpMethod() {
    Assert.assertEquals("POST", testBlobPusher.initializer().getHttpMethod());
  }

  [TestMethod]
  public void testInitializer_getActionDescription() {
    Assert.assertEquals(
        "push BLOB for someServerUrl/someImageName with digest " + fakeDescriptorDigest,
        testBlobPusher.initializer().getActionDescription());
  }

  [TestMethod]
  public void testWriter_getContent() {
    LongAdder byteCount = new LongAdder();
    BlobHttpContent body = testBlobPusher.writer(mockURL, byteCount.add).getContent();

    Assert.assertNotNull(body);
    Assert.assertEquals("application/octet-stream", body.getType());

    MemoryStream byteArrayOutputStream = new MemoryStream();
    body.writeTo(byteArrayOutputStream);

    Assert.assertEquals(
        TEST_BLOB_CONTENT, new string(byteArrayOutputStream.toByteArray(), StandardCharsets.UTF_8));
    Assert.assertEquals(TEST_BLOB_CONTENT.length(), byteCount.sum());
  }

  [TestMethod]
  public void testWriter_GetAccept() {
    Assert.assertEquals(0, testBlobPusher.writer(mockURL, ignored => {}).getAccept().size());
  }

  [TestMethod]
  public void testWriter_handleResponse() {
    Mockito.when(mockResponse.getHeader("Location"))
        .thenReturn(Collections.singletonList("https://somenewurl/location"));
    GenericUrl requestUrl = new GenericUrl("https://someurl");
    Mockito.when(mockResponse.getRequestUrl()).thenReturn(requestUrl);
    Assert.assertEquals(
        new Uri("https://somenewurl/location"),
        testBlobPusher.writer(mockURL, ignored => {}).handleResponse(mockResponse));
  }

  [TestMethod]
  public void testWriter_getApiRoute() {
    Uri fakeUrl = new Uri("http://someurl");
    Assert.assertEquals(fakeUrl, testBlobPusher.writer(fakeUrl, ignored => {}).getApiRoute(""));
  }

  [TestMethod]
  public void testWriter_getHttpMethod() {
    Assert.assertEquals("PATCH", testBlobPusher.writer(mockURL, ignored => {}).getHttpMethod());
  }

  [TestMethod]
  public void testWriter_getActionDescription() {
    Assert.assertEquals(
        "push BLOB for someServerUrl/someImageName with digest " + fakeDescriptorDigest,
        testBlobPusher.writer(mockURL, ignored => {}).getActionDescription());
  }

  [TestMethod]
  public void testCommitter_getContent() {
    Assert.assertNull(testBlobPusher.committer(mockURL).getContent());
  }

  [TestMethod]
  public void testCommitter_GetAccept() {
    Assert.assertEquals(0, testBlobPusher.committer(mockURL).getAccept().size());
  }

  [TestMethod]
  public void testCommitter_handleResponse() {
    Assert.assertNull(
        testBlobPusher.committer(mockURL).handleResponse(Mockito.mock(typeof(HttpResponseMessage))));
  }

  [TestMethod]
  public void testCommitter_getApiRoute() {
    Assert.assertEquals(
        new Uri("https://someurl?somequery=somevalue&digest=" + fakeDescriptorDigest),
        testBlobPusher.committer(new Uri("https://someurl?somequery=somevalue")).getApiRoute(""));
  }

  [TestMethod]
  public void testCommitter_getHttpMethod() {
    Assert.assertEquals("PUT", testBlobPusher.committer(mockURL).getHttpMethod());
  }

  [TestMethod]
  public void testCommitter_getActionDescription() {
    Assert.assertEquals(
        "push BLOB for someServerUrl/someImageName with digest " + fakeDescriptorDigest,
        testBlobPusher.committer(mockURL).getActionDescription());
  }
}
}
