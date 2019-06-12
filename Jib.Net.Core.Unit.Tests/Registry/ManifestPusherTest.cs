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
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.image.json;
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace com.google.cloud.tools.jib.registry {
































/** Tests for {@link ManifestPusher}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class ManifestPusherTest {

  private HttpResponseMessage mockResponse = Mock.Of<HttpResponseMessage>();
  private EventHandlers mockEventHandlers = Mock.Of<EventHandlers>();

  private SystemPath v22manifestJsonFile;
  private V22ManifestTemplate fakeManifestTemplate;
  private ManifestPusher testManifestPusher;

  [SetUp]
  public void setUp() {
    v22manifestJsonFile = Paths.get(Resources.getResource("core/json/v22manifest.json").toURI());
    fakeManifestTemplate =
        JsonTemplateMapper.readJsonFromFile<V22ManifestTemplate>(v22manifestJsonFile);


    testManifestPusher =
        new ManifestPusher(
            new RegistryEndpointRequestProperties("someServerUrl", "someImageName"),
            fakeManifestTemplate,
            "test-image-tag",
            mockEventHandlers);
  }

  [Test]
  public void testGetContent() {
    BlobHttpContent body = testManifestPusher.getContent();

    Assert.IsNotNull(body);
    Assert.AreEqual(V22ManifestTemplate.MANIFEST_MEDIA_TYPE, body.getType());

    MemoryStream bodyCaptureStream = new MemoryStream();
    body.writeTo(bodyCaptureStream);
    string v22manifestJson =
        StandardCharsets.UTF_8.GetString(Files.readAllBytes(v22manifestJsonFile));
    Assert.AreEqual(
        v22manifestJson, StandardCharsets.UTF_8.GetString(bodyCaptureStream.toByteArray()));
  }

  [Test]
  public void testHandleResponse_valid() {
    DescriptorDigest expectedDigest = Digests.computeJsonDigest(fakeManifestTemplate);
    Mock.Get(mockResponse).Setup(m => m.getHeader("Docker-Content-Digest")).Returns(Collections.singletonList(expectedDigest.toString()));

    Assert.AreEqual(expectedDigest, testManifestPusher.handleResponse(mockResponse));
  }

  [Test]
  public void testHandleResponse_noDigest() {
    DescriptorDigest expectedDigest = Digests.computeJsonDigest(fakeManifestTemplate);
    Mock.Get(mockResponse).Setup(m => m.getHeader("Docker-Content-Digest")).Returns(Collections.emptyList<string>());

    Assert.AreEqual(expectedDigest, testManifestPusher.handleResponse(mockResponse));
    Mock.Get(mockEventHandlers).Verify(m => m.dispatch(LogEvent.warn("Expected image digest " + expectedDigest + ", but received none")));

  }

  [Test]
  public void testHandleResponse_multipleDigests() {
    DescriptorDigest expectedDigest = Digests.computeJsonDigest(fakeManifestTemplate);
    Mock.Get(mockResponse).Setup(m => m.getHeader("Docker-Content-Digest")).Returns(Arrays.asList("too", "many"));

    Assert.AreEqual(expectedDigest, testManifestPusher.handleResponse(mockResponse));
    Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
            LogEvent.warn("Expected image digest " + expectedDigest + ", but received: too, many")));

  }

  [Test]
  public void testHandleResponse_invalidDigest() {
    DescriptorDigest expectedDigest = Digests.computeJsonDigest(fakeManifestTemplate);
    Mock.Get(mockResponse).Setup(m => m.getHeader("Docker-Content-Digest")).Returns(Collections.singletonList("not valid"));

    Assert.AreEqual(expectedDigest, testManifestPusher.handleResponse(mockResponse));
    Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
            LogEvent.warn("Expected image digest " + expectedDigest + ", but received: not valid")));

  }

  [Test]
  public void testApiRoute() {
    Assert.AreEqual(
        new Uri("http://someApiBase/someImageName/manifests/test-image-tag"),
        testManifestPusher.getApiRoute("http://someApiBase/"));
  }

  [Test]
  public void testGetHttpMethod() {
    Assert.AreEqual("PUT", testManifestPusher.getHttpMethod());
  }

  [Test]
  public void testGetActionDescription() {
    Assert.AreEqual(
        "push image manifest for someServerUrl/someImageName:test-image-tag",
        testManifestPusher.getActionDescription());
  }

  [Test]
  public void testGetAccept() {
    Assert.AreEqual(0, testManifestPusher.getAccept().size());
  }

  /** Docker Registry 2.0 and 2.1 return 400 / TAG_INVALID. */
  [Test]
  public void testHandleHttpResponseException_dockerRegistry_tagInvalid()
      {
    HttpResponseMessage exception =
        new HttpResponseException.Builder(
                HttpStatusCode.BadRequest)
            .setContent(
                "{\"errors\":[{\"code\":\"TAG_INVALID\","
                    + "\"message\":\"manifest tag did not match URI\"}]}")
            .build();
    try {
      testManifestPusher.handleHttpResponseException(exception);
      Assert.Fail();

    } catch (RegistryErrorException ex) {
                StringAssert.Contains(
                    ex.getMessage(),
                        "Registry may not support pushing OCI Manifest or "
                            + "Docker Image Manifest Version 2, Schema 2");
    }
  }

  /** Docker Registry 2.2 returns a 400 / MANIFEST_INVALID. */
  [Test]
  public void testHandleHttpResponseException_dockerRegistry_manifestInvalid()
      {
            HttpResponseMessage exception =
        new HttpResponseException.Builder(HttpStatusCode.BadRequest)
            .setContent(
                "{\"errors\":[{\"code\":\"MANIFEST_INVALID\","
                    + "\"message\":\"manifest invalid\",\"detail\":{}}]}")
            .build();
    try {
      testManifestPusher.handleHttpResponseException(exception);
      Assert.Fail();

    } catch (RegistryErrorException ex) {
                StringAssert.Contains(
                    ex.getMessage(),
                        "Registry may not support pushing OCI Manifest or "
                            + "Docker Image Manifest Version 2, Schema 2");
    }
  }

  /** Quay.io returns an undocumented 415 / MANIFEST_INVALID. */
  [Test]
  public void testHandleHttpResponseException_quayIo() {
            HttpResponseMessage exception =
        new HttpResponseException.Builder(HttpStatusCode.UnsupportedMediaType)
            .setContent(
                "{\"errors\":[{\"code\":\"MANIFEST_INVALID\","
                    + "\"detail\":{\"message\":\"manifest schema version not supported\"},"
                    + "\"message\":\"manifest invalid\"}]}")
            .build();
    try {
      testManifestPusher.handleHttpResponseException(exception);
      Assert.Fail();

    } catch (RegistryErrorException ex) {
                StringAssert.Contains(
                    ex.getMessage(),
                        "Registry may not support pushing OCI Manifest or "
                            + "Docker Image Manifest Version 2, Schema 2");
    }
  }

  [Test]
  public void testHandleHttpResponseException_otherError() {
            HttpResponseMessage exception =
        new HttpResponseException.Builder(                HttpStatusCode.Unauthorized)
            .setContent("{\"errors\":[{\"code\":\"UNAUTHORIZED\",\"message\":\"Unauthorized\"]}}")
            .build();
    try {
      testManifestPusher.handleHttpResponseException(exception);
      Assert.Fail();

    } catch (HttpResponseException ex) {
      Assert.AreSame(exception, ex);
    }
  }
}
}
