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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Http;

namespace com.google.cloud.tools.jib.registry {



























/** Tests for {@link ManifestPuller}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class ManifestPullerTest {

  private static Stream stringToInputStreamUtf8(string @string) {
    return new MemoryStream(@string.getBytes(StandardCharsets.UTF_8));
  }

  private HttpResponseMessage mockResponse = Mock.Of<HttpResponseMessage>();

  private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
      new RegistryEndpointRequestProperties("someServerUrl", "someImageName");
        private readonly ManifestPuller<ManifestTemplate> testManifestPuller;
        public ManifestPullerTest()
        {
            testManifestPuller =
         new ManifestPuller<ManifestTemplate>(
             fakeRegistryEndpointRequestProperties, "test-image-tag");

        }

  [Test]
  public void testHandleResponse_v21()
      {
    SystemPath v21ManifestFile = Paths.get(Resources.getResource("core/json/v21manifest.json").toURI());
    Stream v21Manifest = new MemoryStream(Files.readAllBytes(v21ManifestFile));

    Mock.Get(mockResponse).Setup(m => m.getBody()).Returns(v21Manifest);

    ManifestTemplate manifestTemplate =
        new ManifestPuller<V21ManifestTemplate>(
                fakeRegistryEndpointRequestProperties, "test-image-tag")
            .handleResponse(mockResponse);

    Assert.IsInstanceOf<V21ManifestTemplate>(manifestTemplate);

  }

  [Test]
  public void testHandleResponse_v22()
      {
    SystemPath v22ManifestFile = Paths.get(Resources.getResource("core/json/v22manifest.json").toURI());
    Stream v22Manifest = new MemoryStream(Files.readAllBytes(v22ManifestFile));

    Mock.Get(mockResponse).Setup(m => m.getBody()).Returns(v22Manifest);

    ManifestTemplate manifestTemplate =
        new ManifestPuller<V22ManifestTemplate>(
                fakeRegistryEndpointRequestProperties, "test-image-tag")
            .handleResponse(mockResponse);

    Assert.IsInstanceOf<V22ManifestTemplate>(manifestTemplate);

  }

  [Test]
  public void testHandleResponse_noSchemaVersion() {
    Mock.Get(mockResponse).Setup(m => m.getBody()).Returns(stringToInputStreamUtf8("{}"));

    try {
      testManifestPuller.handleResponse(mockResponse);
      Assert.Fail("An empty manifest should throw an error");

    } catch (UnknownManifestFormatException ex) {
      Assert.AreEqual("Cannot find field 'schemaVersion' in manifest", ex.getMessage());
    }
  }

  [Test]
  public void testHandleResponse_invalidSchemaVersion() {
    Mock.Get(mockResponse).Setup(m => m.getBody()).Returns(stringToInputStreamUtf8("{\"schemaVersion\":\"not valid\"}"));

    try {
      testManifestPuller.handleResponse(mockResponse);
      Assert.Fail("A non-integer schemaVersion should throw an error");

    } catch (UnknownManifestFormatException ex) {
      Assert.AreEqual("`schemaVersion` field is not an integer", ex.getMessage());
    }
  }

  [Test]
  public void testHandleResponse_unknownSchemaVersion() {
    Mock.Get(mockResponse).Setup(m => m.getBody()).Returns(stringToInputStreamUtf8("{\"schemaVersion\":0}"));

    try {
      testManifestPuller.handleResponse(mockResponse);
      Assert.Fail("An unknown manifest schemaVersion should throw an error");

    } catch (UnknownManifestFormatException ex) {
      Assert.AreEqual("Unknown schemaVersion: 0 - only 1 and 2 are supported", ex.getMessage());
    }
  }

  [Test]
  public void testGetApiRoute() {
    Assert.AreEqual(
        new Uri("http://someApiBase/someImageName/manifests/test-image-tag"),
        testManifestPuller.getApiRoute("http://someApiBase/"));
  }

  [Test]
  public void testGetHttpMethod() {
    Assert.AreEqual("GET", testManifestPuller.getHttpMethod());
  }

  [Test]
  public void testGetActionDescription() {
    Assert.AreEqual(
        "pull image manifest for someServerUrl/someImageName:test-image-tag",
        testManifestPuller.getActionDescription());
  }

  [Test]
  public void testGetContent() {
    Assert.IsNull(testManifestPuller.getContent());
  }

  [Test]
  public void testGetAccept() {
    Assert.AreEqual(
        Arrays.asList(
            OCIManifestTemplate.MANIFEST_MEDIA_TYPE,
            V22ManifestTemplate.MANIFEST_MEDIA_TYPE,
            V21ManifestTemplate.MEDIA_TYPE),
        testManifestPuller.getAccept());

    Assert.AreEqual(
        Collections.singletonList(OCIManifestTemplate.MANIFEST_MEDIA_TYPE),
        new ManifestPuller<OCIManifestTemplate>(
                fakeRegistryEndpointRequestProperties, "test-image-tag")
            .getAccept());
    Assert.AreEqual(
        Collections.singletonList(V22ManifestTemplate.MANIFEST_MEDIA_TYPE),
        new ManifestPuller<V22ManifestTemplate>(
                fakeRegistryEndpointRequestProperties, "test-image-tag")
            .getAccept());
    Assert.AreEqual(
        Collections.singletonList(V21ManifestTemplate.MEDIA_TYPE),
        new ManifestPuller<V21ManifestTemplate>(
                fakeRegistryEndpointRequestProperties, "test-image-tag")
            .getAccept());
  }
}
}
