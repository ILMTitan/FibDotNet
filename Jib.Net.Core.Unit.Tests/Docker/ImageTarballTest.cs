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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.docker.json;
using com.google.cloud.tools.jib.image;
using com.google.cloud.tools.jib.image.json;
using com.google.cloud.tools.jib.json;
using ICSharpCode.SharpZipLib.Tar;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.docker
{
    /** Tests for {@link ImageTarball}. */
    public class ImageTarballTest
    {
        private readonly Layer mockLayer1 = Mock.Of<Layer>();
        private readonly Layer mockLayer2 = Mock.Of<Layer>();

        [Test]
        public async Task testWriteToAsync()
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

            Mock.Get(mockLayer1).Setup(m => m.getBlob()).Returns(Blobs.from(fileA));

            Mock.Get(mockLayer1).Setup(m => m.getBlobDescriptor()).Returns(new BlobDescriptor(fileASize, fakeDigestA));

            Mock.Get(mockLayer1).Setup(m => m.getDiffId()).Returns(fakeDigestA);

            Mock.Get(mockLayer2).Setup(m => m.getBlob()).Returns(Blobs.from(fileB));

            Mock.Get(mockLayer2).Setup(m => m.getBlobDescriptor()).Returns(new BlobDescriptor(fileBSize, fakeDigestB));

            Mock.Get(mockLayer2).Setup(m => m.getDiffId()).Returns(fakeDigestB);

            Image testImage =
                Image.builder(ManifestFormat.V22).addLayer(mockLayer1).addLayer(mockLayer2).build();

            ImageTarball imageToTarball = new ImageTarball(testImage, ImageReference.parse("my/image:tag"));

            MemoryStream @out = new MemoryStream();
            await imageToTarball.writeToAsync(@out).ConfigureAwait(false);
            MemoryStream @in = new MemoryStream(@out.toByteArray());
            using (TarInputStream tarArchiveInputStream = new TarInputStream(@in))
            {
                // Verifies layer with fileA was added.
                TarEntry headerFileALayer = tarArchiveInputStream.getNextTarEntry();
                Assert.AreEqual(fakeDigestA.getHash() + ".tar.gz", headerFileALayer.getName());
                string fileAString =
                    CharStreams.toString(
                        new StreamReader(tarArchiveInputStream, StandardCharsets.UTF_8));
                Assert.AreEqual(await Blobs.writeToStringAsync(Blobs.from(fileA)).ConfigureAwait(false), fileAString);

                // Verifies layer with fileB was added.
                TarEntry headerFileBLayer = tarArchiveInputStream.getNextTarEntry();
                Assert.AreEqual(fakeDigestB.getHash() + ".tar.gz", headerFileBLayer.getName());
                string fileBString =
                    CharStreams.toString(
                        new StreamReader(tarArchiveInputStream, StandardCharsets.UTF_8));
                Assert.AreEqual(await Blobs.writeToStringAsync(Blobs.from(fileB)).ConfigureAwait(false), fileBString);

                // Verifies container configuration was added.
                TarEntry headerContainerConfiguration = tarArchiveInputStream.getNextTarEntry();
                Assert.AreEqual("config.json", headerContainerConfiguration.getName());
                string containerConfigJson =
                    CharStreams.toString(
                        new StreamReader(tarArchiveInputStream, StandardCharsets.UTF_8));
                JsonTemplateMapper.readJson<ContainerConfigurationTemplate>(containerConfigJson);

                // Verifies manifest was added.
                TarEntry headerManifest = tarArchiveInputStream.getNextTarEntry();
                Assert.AreEqual("manifest.json", headerManifest.getName());
                string manifestJson =
                    CharStreams.toString(
                        new StreamReader(tarArchiveInputStream, StandardCharsets.UTF_8));
                JsonTemplateMapper.readListOfJson<DockerLoadManifestEntryTemplate>(manifestJson);
            }
        }
    }
}
