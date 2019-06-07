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



























/** Tests for {@link ManifestPuller}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class ManifestPullerTest {

  private static InputStream stringToInputStreamUtf8(string string) {
    return new ByteArrayInputStream(string.getBytes(StandardCharsets.UTF_8));
  }

  [Mock] private Response mockResponse;

  private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
      new RegistryEndpointRequestProperties("someServerUrl", "someImageName");
  private readonly ManifestPuller<ManifestTemplate> testManifestPuller =
      new ManifestPuller<>(
          fakeRegistryEndpointRequestProperties, "test-image-tag", typeof(ManifestTemplate));

  [TestMethod]
  public void testHandleResponse_v21()
      {
    Path v21ManifestFile = Paths.get(Resources.getResource("core/json/v21manifest.json").toURI());
    InputStream v21Manifest = new ByteArrayInputStream(Files.readAllBytes(v21ManifestFile));

    Mockito.when(mockResponse.getBody()).thenReturn(v21Manifest);
    ManifestTemplate manifestTemplate =
        new ManifestPuller<>(
                fakeRegistryEndpointRequestProperties, "test-image-tag", typeof(V21ManifestTemplate))
            .handleResponse(mockResponse);

    Assert.assertThat(manifestTemplate, CoreMatchers.instanceOf(typeof(V21ManifestTemplate)));
  }

  [TestMethod]
  public void testHandleResponse_v22()
      {
    Path v22ManifestFile = Paths.get(Resources.getResource("core/json/v22manifest.json").toURI());
    InputStream v22Manifest = new ByteArrayInputStream(Files.readAllBytes(v22ManifestFile));

    Mockito.when(mockResponse.getBody()).thenReturn(v22Manifest);
    ManifestTemplate manifestTemplate =
        new ManifestPuller<>(
                fakeRegistryEndpointRequestProperties, "test-image-tag", typeof(V22ManifestTemplate))
            .handleResponse(mockResponse);

    Assert.assertThat(manifestTemplate, CoreMatchers.instanceOf(typeof(V22ManifestTemplate)));
  }

  [TestMethod]
  public void testHandleResponse_noSchemaVersion() {
    Mockito.when(mockResponse.getBody()).thenReturn(stringToInputStreamUtf8("{}"));
    try {
      testManifestPuller.handleResponse(mockResponse);
      Assert.fail("An empty manifest should throw an error");

    } catch (UnknownManifestFormatException ex) {
      Assert.assertEquals("Cannot find field 'schemaVersion' in manifest", ex.getMessage());
    }
  }

  [TestMethod]
  public void testHandleResponse_invalidSchemaVersion() {
    Mockito.when(mockResponse.getBody())
        .thenReturn(stringToInputStreamUtf8("{\"schemaVersion\":\"not valid\"}"));
    try {
      testManifestPuller.handleResponse(mockResponse);
      Assert.fail("A non-integer schemaVersion should throw an error");

    } catch (UnknownManifestFormatException ex) {
      Assert.assertEquals("`schemaVersion` field is not an integer", ex.getMessage());
    }
  }

  [TestMethod]
  public void testHandleResponse_unknownSchemaVersion() {
    Mockito.when(mockResponse.getBody())
        .thenReturn(stringToInputStreamUtf8("{\"schemaVersion\":0}"));
    try {
      testManifestPuller.handleResponse(mockResponse);
      Assert.fail("An unknown manifest schemaVersion should throw an error");

    } catch (UnknownManifestFormatException ex) {
      Assert.assertEquals("Unknown schemaVersion: 0 - only 1 and 2 are supported", ex.getMessage());
    }
  }

  [TestMethod]
  public void testGetApiRoute() {
    Assert.assertEquals(
        new Uri("http://someApiBase/someImageName/manifests/test-image-tag"),
        testManifestPuller.getApiRoute("http://someApiBase/"));
  }

  [TestMethod]
  public void testGetHttpMethod() {
    Assert.assertEquals("GET", testManifestPuller.getHttpMethod());
  }

  [TestMethod]
  public void testGetActionDescription() {
    Assert.assertEquals(
        "pull image manifest for someServerUrl/someImageName:test-image-tag",
        testManifestPuller.getActionDescription());
  }

  [TestMethod]
  public void testGetContent() {
    Assert.assertNull(testManifestPuller.getContent());
  }

  [TestMethod]
  public void testGetAccept() {
    Assert.assertEquals(
        Arrays.asList(
            OCIManifestTemplate.MANIFEST_MEDIA_TYPE,
            V22ManifestTemplate.MANIFEST_MEDIA_TYPE,
            V21ManifestTemplate.MEDIA_TYPE),
        testManifestPuller.getAccept());

    Assert.assertEquals(
        Collections.singletonList(OCIManifestTemplate.MANIFEST_MEDIA_TYPE),
        new ManifestPuller<>(
                fakeRegistryEndpointRequestProperties, "test-image-tag", typeof(OCIManifestTemplate))
            .getAccept());
    Assert.assertEquals(
        Collections.singletonList(V22ManifestTemplate.MANIFEST_MEDIA_TYPE),
        new ManifestPuller<>(
                fakeRegistryEndpointRequestProperties, "test-image-tag", typeof(V22ManifestTemplate))
            .getAccept());
    Assert.assertEquals(
        Collections.singletonList(V21ManifestTemplate.MEDIA_TYPE),
        new ManifestPuller<>(
                fakeRegistryEndpointRequestProperties, "test-image-tag", typeof(V21ManifestTemplate))
            .getAccept());
  }
}
}
