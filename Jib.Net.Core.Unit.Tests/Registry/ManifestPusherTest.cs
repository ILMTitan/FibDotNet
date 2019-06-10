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
































/** Tests for {@link ManifestPusher}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class ManifestPusherTest {

  [Mock] private HttpResponseMessage mockResponse;
  [Mock] private EventHandlers mockEventHandlers;

  private SystemPath v22manifestJsonFile;
  private V22ManifestTemplate fakeManifestTemplate;
  private ManifestPusher testManifestPusher;

  [TestInitialize]
  public void setUp() {
    v22manifestJsonFile = Paths.get(Resources.getResource("core/json/v22manifest.json").toURI());
    fakeManifestTemplate =
        JsonTemplateMapper.readJsonFromFile(v22manifestJsonFile, typeof(V22ManifestTemplate));

    testManifestPusher =
        new ManifestPusher(
            new RegistryEndpointRequestProperties("someServerUrl", "someImageName"),
            fakeManifestTemplate,
            "test-image-tag",
            mockEventHandlers);
  }

  [TestMethod]
  public void testGetContent() {
    BlobHttpContent body = testManifestPusher.getContent();

    Assert.assertNotNull(body);
    Assert.assertEquals(V22ManifestTemplate.MANIFEST_MEDIA_TYPE, body.getType());

    MemoryStream bodyCaptureStream = new MemoryStream();
    body.writeTo(bodyCaptureStream);
    string v22manifestJson =
        new string(Files.readAllBytes(v22manifestJsonFile), StandardCharsets.UTF_8);
    Assert.assertEquals(
        v22manifestJson, new string(bodyCaptureStream.toByteArray(), StandardCharsets.UTF_8));
  }

  [TestMethod]
  public void testHandleResponse_valid() {
    DescriptorDigest expectedDigest = Digests.computeJsonDigest(fakeManifestTemplate);
    Mockito.when(mockResponse.getHeader("Docker-Content-Digest"))
        .thenReturn(Collections.singletonList(expectedDigest.toString()));
    Assert.assertEquals(expectedDigest, testManifestPusher.handleResponse(mockResponse));
  }

  [TestMethod]
  public void testHandleResponse_noDigest() {
    DescriptorDigest expectedDigest = Digests.computeJsonDigest(fakeManifestTemplate);
    Mockito.when(mockResponse.getHeader("Docker-Content-Digest"))
        .thenReturn(Collections.emptyList());

    Assert.assertEquals(expectedDigest, testManifestPusher.handleResponse(mockResponse));
    Mockito.verify(mockEventHandlers)
        .dispatch(LogEvent.warn("Expected image digest " + expectedDigest + ", but received none"));
  }

  [TestMethod]
  public void testHandleResponse_multipleDigests() {
    DescriptorDigest expectedDigest = Digests.computeJsonDigest(fakeManifestTemplate);
    Mockito.when(mockResponse.getHeader("Docker-Content-Digest"))
        .thenReturn(Arrays.asList("too", "many"));

    Assert.assertEquals(expectedDigest, testManifestPusher.handleResponse(mockResponse));
    Mockito.verify(mockEventHandlers)
        .dispatch(
            LogEvent.warn("Expected image digest " + expectedDigest + ", but received: too, many"));
  }

  [TestMethod]
  public void testHandleResponse_invalidDigest() {
    DescriptorDigest expectedDigest = Digests.computeJsonDigest(fakeManifestTemplate);
    Mockito.when(mockResponse.getHeader("Docker-Content-Digest"))
        .thenReturn(Collections.singletonList("not valid"));

    Assert.assertEquals(expectedDigest, testManifestPusher.handleResponse(mockResponse));
    Mockito.verify(mockEventHandlers)
        .dispatch(
            LogEvent.warn("Expected image digest " + expectedDigest + ", but received: not valid"));
  }

  [TestMethod]
  public void testApiRoute() {
    Assert.assertEquals(
        new Uri("http://someApiBase/someImageName/manifests/test-image-tag"),
        testManifestPusher.getApiRoute("http://someApiBase/"));
  }

  [TestMethod]
  public void testGetHttpMethod() {
    Assert.assertEquals("PUT", testManifestPusher.getHttpMethod());
  }

  [TestMethod]
  public void testGetActionDescription() {
    Assert.assertEquals(
        "push image manifest for someServerUrl/someImageName:test-image-tag",
        testManifestPusher.getActionDescription());
  }

  [TestMethod]
  public void testGetAccept() {
    Assert.assertEquals(0, testManifestPusher.getAccept().size());
  }

  /** Docker Registry 2.0 and 2.1 return 400 / TAG_INVALID. */
  [TestMethod]
  public void testHandleHttpResponseException_dockerRegistry_tagInvalid()
      {
    HttpResponseException exception =
        new HttpResponseException.Builder(
                HttpStatusCode.BadRequest, "Bad Request", new HttpHeaders())
            .setContent(
                "{\"errors\":[{\"code\":\"TAG_INVALID\","
                    + "\"message\":\"manifest tag did not match URI\"}]}")
            .build();
    try {
      testManifestPusher.handleHttpResponseException(exception);
      Assert.fail();

    } catch (RegistryErrorException ex) {
      Assert.assertThat(
          ex.getMessage(),
          CoreMatchers.containsString(
              "Registry may not support pushing OCI Manifest or "
                  + "Docker Image Manifest Version 2, Schema 2"));
    }
  }

  /** Docker Registry 2.2 returns a 400 / MANIFEST_INVALID. */
  [TestMethod]
  public void testHandleHttpResponseException_dockerRegistry_manifestInvalid()
      {
    HttpResponseException exception =
        new HttpResponseException.Builder(
                HttpStatusCode.BadRequest, "Bad Request", new HttpHeaders())
            .setContent(
                "{\"errors\":[{\"code\":\"MANIFEST_INVALID\","
                    + "\"message\":\"manifest invalid\",\"detail\":{}}]}")
            .build();
    try {
      testManifestPusher.handleHttpResponseException(exception);
      Assert.fail();

    } catch (RegistryErrorException ex) {
      Assert.assertThat(
          ex.getMessage(),
          CoreMatchers.containsString(
              "Registry may not support pushing OCI Manifest or "
                  + "Docker Image Manifest Version 2, Schema 2"));
    }
  }

  /** Quay.io returns an undocumented 415 / MANIFEST_INVALID. */
  [TestMethod]
  public void testHandleHttpResponseException_quayIo() {
    HttpResponseException exception =
        new HttpResponseException.Builder(
                HttpStatusCode.UnsupportedMediaType, "UNSUPPORTED MEDIA TYPE", new HttpHeaders())
            .setContent(
                "{\"errors\":[{\"code\":\"MANIFEST_INVALID\","
                    + "\"detail\":{\"message\":\"manifest schema version not supported\"},"
                    + "\"message\":\"manifest invalid\"}]}")
            .build();
    try {
      testManifestPusher.handleHttpResponseException(exception);
      Assert.fail();

    } catch (RegistryErrorException ex) {
      Assert.assertThat(
          ex.getMessage(),
          CoreMatchers.containsString(
              "Registry may not support pushing OCI Manifest or "
                  + "Docker Image Manifest Version 2, Schema 2"));
    }
  }

  [TestMethod]
  public void testHandleHttpResponseException_otherError() {
    HttpResponseException exception =
        new HttpResponseException.Builder(
                HttpStatusCode.SC_UNAUTHORIZED, "Unauthorized", new HttpHeaders())
            .setContent("{\"errors\":[{\"code\":\"UNAUTHORIZED\",\"message\":\"Unauthorized\"]}}")
            .build();
    try {
      testManifestPusher.handleHttpResponseException(exception);
      Assert.fail();

    } catch (HttpResponseException ex) {
      Assert.assertSame(exception, ex);
    }
  }
}
}
