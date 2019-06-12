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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.image.json {
































/** Tests for {@link ImageToJsonTranslator}. */
public class ImageToJsonTranslatorTest {

  private ImageToJsonTranslator imageToJsonTranslator;
        private static DescriptorDigest fakeDigest = DescriptorDigest.fromHash(new string('a', 32));
        private void setUp(Type t) 
        {
            setUp(new Class<BuildableManifestTemplate>(t));
        }
        private void setUp(IClass<BuildableManifestTemplate> imageFormat)
        {
    Image.Builder testImageBuilder =
        Image.builder(imageFormat)
            .setCreated(Instant.FromUnixTimeSeconds(20))
            .setArchitecture("wasm")
            .setOs("js")
            .addEnvironmentVariable("VAR1", "VAL1")
            .addEnvironmentVariable("VAR2", "VAL2")
            .setEntrypoint(Arrays.asList("some", "entrypoint", "command"))
            .setProgramArguments(Arrays.asList("arg1", "arg2"))
            .setHealthCheck(
                DockerHealthCheck.fromCommand(ImmutableArray.Create("CMD-SHELL", "/checkhealth"))
                    .setInterval(Duration.FromSeconds(3))
                    .setTimeout(Duration.FromSeconds(1))
                    .setStartPeriod(Duration.FromSeconds(2))
                    .setRetries(3)
                    .build())
            .addExposedPorts(ImmutableHashSet.Create(Port.tcp(1000), Port.tcp(2000), Port.udp(3000)))
            .addVolumes(
                ImmutableHashSet.Create(
                    AbsoluteUnixPath.get("/var/job-result-data"),
                    AbsoluteUnixPath.get("/var/log/my-app-logs")))
            .addLabels(ImmutableDic.of("key1", "value1", "key2", "value2"))
            .setWorkingDirectory("/some/workspace")
            .setUser("tomcat");

    DescriptorDigest fakeDigest =
        DescriptorDigest.fromDigest(
            "sha256:8c662931926fa990b41da3c9f42663a537ccd498130030f9149173a0493832ad");
    testImageBuilder.addLayer(
        new FakeLayer());
    testImageBuilder.addHistory(
        HistoryEntry.builder()
            .setCreationTimestamp(Instant.FromUnixTimeSeconds(0))
            .setAuthor("Bazel")
            .setCreatedBy("bazel build ...")
            .setEmptyLayer(true)
            .build());
    testImageBuilder.addHistory(
        HistoryEntry.builder()
            .setCreationTimestamp(Instant.FromUnixTimeSeconds(20))
            .setAuthor("Jib")
            .setCreatedBy("jib")
            .build());
    imageToJsonTranslator = new ImageToJsonTranslator(testImageBuilder.build());
  }

        class FakeLayer : Layer
        {

            public Blob getBlob()
            {
                return Blobs.from("ignored");
            }

            public BlobDescriptor getBlobDescriptor()
            {
                return new BlobDescriptor(1000, fakeDigest);
            }

            public DescriptorDigest getDiffId()
            {
                return fakeDigest;
            }
        }

        [Test]
  public void testGetContainerConfiguration()
      {
    setUp(typeof(V22ManifestTemplate));

    // Loads the expected JSON string.
    SystemPath jsonFile = Paths.get(Resources.getResource("core/json/containerconfig.json").toURI());
    string expectedJson = StandardCharsets.UTF_8.GetString(Files.readAllBytes(jsonFile));

    // Translates the image to the container configuration and writes the JSON string.
    JsonTemplate containerConfiguration = imageToJsonTranslator.getContainerConfiguration();

    Assert.AreEqual(expectedJson, JsonTemplateMapper.toUtf8String(containerConfiguration));
  }

  [Test]
  public void testGetManifest_v22() {
    setUp(typeof(V22ManifestTemplate));
    testGetManifest(typeof(V22ManifestTemplate), "core/json/translated_v22manifest.json");
  }

  [Test]
  public void testGetManifest_oci() {
    setUp(typeof(OCIManifestTemplate));
    testGetManifest(typeof(OCIManifestTemplate), "core/json/translated_ocimanifest.json");
  }

  [Test]
  public void testPortListToMap() {
    ImmutableHashSet<Port> input = ImmutableHashSet.Create(Port.tcp(1000), Port.udp(2000));
    ImmutableSortedDictionary<string, IDictionary<object, object>> expected =
        new Dictionary<string, IDictionary<object, object>>
        {
            ["1000/tcp"] = ImmutableDictionary.Create<object, object>(),
            ["2000/udp"] = ImmutableDictionary.Create<object, object>()
        }.ToImmutableSortedDictionary();
    Assert.AreEqual(expected, ImageToJsonTranslator.portSetToMap(input));
  }

        [Test]
        public void testVolumeListToMap()
        {
            ImmutableHashSet<AbsoluteUnixPath> input =
                ImmutableHashSet.Create(
                    AbsoluteUnixPath.get("/var/job-result-data"),
                    AbsoluteUnixPath.get("/var/log/my-app-logs"));
            ImmutableSortedDictionary<string, IDictionary<object, object>> expected =
                new Dictionary<string, IDictionary<object, object>>
                {
                    ["/var/job-result-data"] = ImmutableDictionary.Create<object, object>(),
                    ["/var/log/my-app-logs"] = ImmutableDictionary.Create<object, object>()
                }.ToImmutableSortedDictionary();
            Assert.AreEqual(expected, ImageToJsonTranslator.volumesSetToMap(input));
        }

  [Test]
  public void testEnvironmentMapToList() {
    ImmutableDictionary<string, string> input = ImmutableDic.of("NAME1", "VALUE1", "NAME2", "VALUE2");
    ImmutableArray<string> expected = ImmutableArray.Create("NAME1=VALUE1", "NAME2=VALUE2");
    Assert.AreEqual(expected, ImageToJsonTranslator.environmentMapToList(input));
  }

        private void testGetManifest(Type t, string translatedJsonFilename)
        {
            testGetManifest<BuildableManifestTemplate>(new Class<BuildableManifestTemplate>(t), translatedJsonFilename);
        }
  /** Tests translation of image to {@link BuildableManifestTemplate}. */
  private void testGetManifest<T>(
      IClass<T> manifestTemplateClass, string translatedJsonFilename) where T : BuildableManifestTemplate {
    // Loads the expected JSON string.
    SystemPath jsonFile = Paths.get(Resources.getResource(translatedJsonFilename).toURI());
    string expectedJson = StandardCharsets.UTF_8.GetString(Files.readAllBytes(jsonFile));

    // Translates the image to the manifest and writes the JSON string.
    JsonTemplate containerConfiguration = imageToJsonTranslator.getContainerConfiguration();
    BlobDescriptor blobDescriptor = Digests.computeDigest(containerConfiguration);
    T manifestTemplate =
        imageToJsonTranslator.getManifestTemplate(manifestTemplateClass, blobDescriptor);

    Assert.AreEqual(expectedJson, JsonTemplateMapper.toUtf8String(manifestTemplate));
  }
}
}
