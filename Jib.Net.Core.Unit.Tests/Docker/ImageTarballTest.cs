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

namespace com.google.cloud.tools.jib.docker {

































/** Tests for {@link ImageTarball}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class ImageTarballTest {

  [Mock] private Layer mockLayer1;
  [Mock] private Layer mockLayer2;

  [TestMethod]
  public void testWriteTo()
      {
    SystemPath fileA = Paths.get(Resources.getResource("core/fileA").toURI());
    SystemPath fileB = Paths.get(Resources.getResource("core/fileB").toURI());
    long fileASize = Files.size(fileA);
    long fileBSize = Files.size(fileB);

    DescriptorDigest fakeDigestA =
        DescriptorDigest.fromHash(
            "5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5");
    DescriptorDigest fakeDigestB =
        DescriptorDigest.fromHash(
            "5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc6");

    Mockito.when(mockLayer1.getBlob()).thenReturn(Blobs.from(fileA));
    Mockito.when(mockLayer1.getBlobDescriptor())
        .thenReturn(new BlobDescriptor(fileASize, fakeDigestA));
    Mockito.when(mockLayer1.getDiffId()).thenReturn(fakeDigestA);
    Mockito.when(mockLayer2.getBlob()).thenReturn(Blobs.from(fileB));
    Mockito.when(mockLayer2.getBlobDescriptor())
        .thenReturn(new BlobDescriptor(fileBSize, fakeDigestB));
    Mockito.when(mockLayer2.getDiffId()).thenReturn(fakeDigestB);
    Image testImage =
        Image.builder(typeof(V22ManifestTemplate)).addLayer(mockLayer1).addLayer(mockLayer2).build();

    ImageTarball imageToTarball = new ImageTarball(testImage, ImageReference.parse("my/image:tag"));

    MemoryStream out = new MemoryStream();
    imageToTarball.writeTo(out);
    ByteArrayInputStream in = new MemoryStream(out.toByteArray());
    using (TarArchiveInputStream tarArchiveInputStream = new TarArchiveInputStream(in)) {

      // Verifies layer with fileA was added.
      TarEntry headerFileALayer = tarArchiveInputStream.getNextTarEntry();
      Assert.assertEquals(fakeDigestA.getHash() + ".tar.gz", headerFileALayer.getName());
      string fileAString =
          CharStreams.toString(
              new StreamReader(tarArchiveInputStream, StandardCharsets.UTF_8));
      Assert.assertEquals(Blobs.writeToString(Blobs.from(fileA)), fileAString);

      // Verifies layer with fileB was added.
      TarEntry headerFileBLayer = tarArchiveInputStream.getNextTarEntry();
      Assert.assertEquals(fakeDigestB.getHash() + ".tar.gz", headerFileBLayer.getName());
      string fileBString =
          CharStreams.toString(
              new StreamReader(tarArchiveInputStream, StandardCharsets.UTF_8));
      Assert.assertEquals(Blobs.writeToString(Blobs.from(fileB)), fileBString);

      // Verifies container configuration was added.
      TarEntry headerContainerConfiguration = tarArchiveInputStream.getNextTarEntry();
      Assert.assertEquals("config.json", headerContainerConfiguration.getName());
      string containerConfigJson =
          CharStreams.toString(
              new StreamReader(tarArchiveInputStream, StandardCharsets.UTF_8));
      JsonTemplateMapper.readJson(containerConfigJson, typeof(ContainerConfigurationTemplate));

      // Verifies manifest was added.
      TarEntry headerManifest = tarArchiveInputStream.getNextTarEntry();
      Assert.assertEquals("manifest.json", headerManifest.getName());
      string manifestJson =
          CharStreams.toString(
              new StreamReader(tarArchiveInputStream, StandardCharsets.UTF_8));
      JsonTemplateMapper.readListOfJson(manifestJson, typeof(DockerLoadManifestEntryTemplate));
    }
  }
}
}
