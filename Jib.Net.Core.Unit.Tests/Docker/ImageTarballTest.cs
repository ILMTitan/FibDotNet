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

using ICSharpCode.SharpZipLib.Tar;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Configuration;
using Jib.Net.Core.Docker;
using Jib.Net.Core.Docker.Json;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Images;
using Jib.Net.Core.Images.Json;
using Jib.Net.Core.Json;
using Jib.Net.Test.Common;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace Jib.Net.Core.Unit.Tests.Docker
{
    /** Tests for {@link ImageTarball}. */
    public class ImageTarballTest
    {
        private readonly ILayer mockLayer1 = Mock.Of<ILayer>();
        private readonly ILayer mockLayer2 = Mock.Of<ILayer>();

        [Test]
        public async Task TestWriteToAsync()
        {
            SystemPath fileA = Paths.Get(TestResources.GetResource("core/fileA").ToURI());
            SystemPath fileB = Paths.Get(TestResources.GetResource("core/fileB").ToURI());
            long fileASize = Files.Size(fileA);
            long fileBSize = Files.Size(fileB);

            DescriptorDigest fakeDigestA =
                DescriptorDigest.FromHash(
                    "5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5");
            DescriptorDigest fakeDigestB =
                DescriptorDigest.FromHash(
                    "5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc6");

            Mock.Get(mockLayer1).Setup(m => m.GetBlob()).Returns(Blobs.From(fileA));

            Mock.Get(mockLayer1).Setup(m => m.GetBlobDescriptor()).Returns(new BlobDescriptor(fileASize, fakeDigestA));

            Mock.Get(mockLayer1).Setup(m => m.GetDiffId()).Returns(fakeDigestA);

            Mock.Get(mockLayer2).Setup(m => m.GetBlob()).Returns(Blobs.From(fileB));

            Mock.Get(mockLayer2).Setup(m => m.GetBlobDescriptor()).Returns(new BlobDescriptor(fileBSize, fakeDigestB));

            Mock.Get(mockLayer2).Setup(m => m.GetDiffId()).Returns(fakeDigestB);

            Image testImage =
                Image.CreateBuilder(ManifestFormat.V22).AddLayer(mockLayer1).AddLayer(mockLayer2).Build();

            ImageTarball imageToTarball = new ImageTarball(testImage, ImageReference.Parse("my/image:tag"));

            MemoryStream @out = new MemoryStream();
            await imageToTarball.WriteToAsync(@out).ConfigureAwait(false);
            MemoryStream @in = new MemoryStream(@out.ToArray());
            using (TarInputStream tarArchiveInputStream = new TarInputStream(@in))
            {
                // Verifies layer with fileA was added.
                TarEntry headerFileALayer = tarArchiveInputStream.GetNextEntry();
                Assert.AreEqual(fakeDigestA.GetHash() + ".tar.gz", headerFileALayer.Name);
                string fileAString =
                    await new StreamReader(tarArchiveInputStream).ReadToEndAsync().ConfigureAwait(false);
                Assert.AreEqual(await Blobs.WriteToStringAsync(Blobs.From(fileA)).ConfigureAwait(false), fileAString);

                // Verifies layer with fileB was added.
                TarEntry headerFileBLayer = tarArchiveInputStream.GetNextEntry();
                Assert.AreEqual(fakeDigestB.GetHash() + ".tar.gz", headerFileBLayer.Name);
                string fileBString =await new StreamReader(tarArchiveInputStream).ReadToEndAsync().ConfigureAwait(false);
                Assert.AreEqual(await Blobs.WriteToStringAsync(Blobs.From(fileB)).ConfigureAwait(false), fileBString);

                // Verifies container configuration was added.
                TarEntry headerContainerConfiguration = tarArchiveInputStream.GetNextEntry();
                Assert.AreEqual("config.json", headerContainerConfiguration.Name);
                string containerConfigJson = await new StreamReader(tarArchiveInputStream).ReadToEndAsync().ConfigureAwait(false);
                JsonTemplateMapper.ReadJson<ContainerConfigurationTemplate>(containerConfigJson);

                // Verifies manifest was added.
                TarEntry headerManifest = tarArchiveInputStream.GetNextEntry();
                Assert.AreEqual("manifest.json", headerManifest.Name);
                string manifestJson = await new StreamReader(tarArchiveInputStream).ReadToEndAsync().ConfigureAwait(false);
                JsonTemplateMapper.ReadListOfJson<DockerLoadManifestEntryTemplate>(manifestJson);
            }
        }
    }
}
