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

using Jib.Net.Core.Global;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link ImageReference}. */

    public class ImageReferenceTest
    {
        private static readonly IList<string> goodRegistries =
            Arrays.asList("some.domain---name.123.com:8080", "gcr.io", "localhost", null, "");

        private static readonly IList<string> goodRepositories =
            Arrays.asList("some123_abc/repository__123-456/name---here", "distroless/java", "repository");

        private static readonly IList<string> goodTags = Arrays.asList("some-.-.Tag", "", "latest", null);

        private static readonly IList<string> goodDigests =
            Arrays.asList(
                "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", null);

        private static readonly IList<string> badImageReferences =
            Arrays.asList(
                "",
                ":justsometag",
                "@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "repository@sha256:a",
                "repository@notadigest",
                "Repositorywithuppercase",
                "registry:8080/Repositorywithuppercase/repository:sometag",
                "domain.name:nonnumberport/repository",
                "domain.name:nonnumberport//:no-repository");

        [Test]
        public void testParse_pass()
        {
            foreach (string goodRegistry in goodRegistries)
            {
                foreach (string goodRepository in goodRepositories)
                {
                    foreach (string goodTag in goodTags)
                    {
                        verifyParse(goodRegistry, goodRepository, ":", goodTag);
                    }
                    foreach (string goodDigest in goodDigests)
                    {
                        verifyParse(goodRegistry, goodRepository, "@", goodDigest);
                    }
                }
            }
        }

        [Test]
        public void testParse_dockerHub_official()
        {
            string imageReferenceString = "busybox";
            ImageReference imageReference = ImageReference.parse(imageReferenceString);

            Assert.AreEqual("registry-1.docker.io", imageReference.getRegistry());
            Assert.AreEqual("library/busybox", imageReference.getRepository());
            Assert.AreEqual("latest", imageReference.getTag());
        }

        [Test]
        public void testParse_dockerHub_user()
        {
            string imageReferenceString = "someuser/someimage";
            ImageReference imageReference = ImageReference.parse(imageReferenceString);

            Assert.AreEqual("registry-1.docker.io", imageReference.getRegistry());
            Assert.AreEqual("someuser/someimage", imageReference.getRepository());
            Assert.AreEqual("latest", imageReference.getTag());
        }

        [Test]
        public void testParse_invalid()
        {
            foreach (string badImageReference in badImageReferences)
            {
                try
                {
                    ImageReference.parse(badImageReference);
                    Assert.Fail(badImageReference + " should not be a valid image reference");
                }
                catch (InvalidImageReferenceException ex)
                {
                    StringAssert.Contains(ex.getMessage(), badImageReference);
                }
            }
        }

        [Test]
        public void testOf_smoke()
        {
            string expectedRegistry = "someregistry";
            string expectedRepository = "somerepository";
            string expectedTag = "sometag";

            Assert.AreEqual(
                expectedRegistry,
                ImageReference.of(expectedRegistry, expectedRepository, expectedTag).getRegistry());
            Assert.AreEqual(
                expectedRepository,
                ImageReference.of(expectedRegistry, expectedRepository, expectedTag).getRepository());
            Assert.AreEqual(
                expectedTag, ImageReference.of(expectedRegistry, expectedRepository, expectedTag).getTag());
            Assert.AreEqual(
                "registry-1.docker.io",
                ImageReference.of(null, expectedRepository, expectedTag).getRegistry());
            Assert.AreEqual(
                "registry-1.docker.io", ImageReference.of(null, expectedRepository, null).getRegistry());
            Assert.AreEqual(
                "latest", ImageReference.of(expectedRegistry, expectedRepository, null).getTag());
            Assert.AreEqual("latest", ImageReference.of(null, expectedRepository, null).getTag());
            Assert.AreEqual(
                expectedRepository, ImageReference.of(null, expectedRepository, null).getRepository());
        }

        [Test]
        public void testToString()
        {
            Assert.AreEqual("someimage", ImageReference.of(null, "someimage", null).toString());
            Assert.AreEqual("someimage", ImageReference.of("", "someimage", "").toString());
            Assert.AreEqual(
                "someotherimage", ImageReference.of(null, "library/someotherimage", null).toString());
            Assert.AreEqual(
                "someregistry/someotherimage",
                ImageReference.of("someregistry", "someotherimage", null).toString());
            Assert.AreEqual(
                "anotherregistry/anotherimage:sometag",
                ImageReference.of("anotherregistry", "anotherimage", "sometag").toString());

            Assert.AreEqual(
                "someimage@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                ImageReference.of(
                        null,
                        "someimage",
                        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
                    .toString());
            Assert.AreEqual(
                "gcr.io/distroless/java@sha256:b430543bea1d8326e767058bdab3a2482ea45f59d7af5c5c61334cd29ede88a1",
                ImageReference.parse(
                        "gcr.io/distroless/java@sha256:b430543bea1d8326e767058bdab3a2482ea45f59d7af5c5c61334cd29ede88a1")
                    .toString());
        }

        [Test]
        public void testToStringWithTag()
        {
            Assert.AreEqual(
                "someimage:latest", ImageReference.of(null, "someimage", null).toStringWithTag());
            Assert.AreEqual(
                "someimage:latest", ImageReference.of("", "someimage", "").toStringWithTag());
            Assert.AreEqual(
                "someotherimage:latest",
                ImageReference.of(null, "library/someotherimage", null).toStringWithTag());
            Assert.AreEqual(
                "someregistry/someotherimage:latest",
                ImageReference.of("someregistry", "someotherimage", null).toStringWithTag());
            Assert.AreEqual(
                "anotherregistry/anotherimage:sometag",
                ImageReference.of("anotherregistry", "anotherimage", "sometag").toStringWithTag());
        }

        [Test]
        public void testIsTagDigest()
        {
            Assert.IsFalse(ImageReference.of(null, "someimage", null).isTagDigest());
            Assert.IsFalse(ImageReference.of(null, "someimage", "latest").isTagDigest());
            Assert.IsTrue(
                ImageReference.of(
                        null,
                        "someimage",
                        "sha256:b430543bea1d8326e767058bdab3a2482ea45f59d7af5c5c61334cd29ede88a1")
                    .isTagDigest());
            Assert.IsTrue(
                ImageReference.parse(
                        "someimage@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
                    .isTagDigest());
        }

        [Test]
        public void testIsScratch()
        {
            Assert.IsTrue(ImageReference.scratch().isScratch());
            Assert.IsFalse(ImageReference.of("", "scratch", "").isScratch());
            Assert.IsFalse(ImageReference.of(null, "scratch", null).isScratch());
        }

        [Test]
        public void testGetRegistry()
        {
            Assert.AreEqual(
                "registry-1.docker.io", ImageReference.of(null, "someimage", null).getRegistry());
            Assert.AreEqual(
                "registry-1.docker.io", ImageReference.of("docker.io", "someimage", null).getRegistry());
            Assert.AreEqual(
                "index.docker.io", ImageReference.of("index.docker.io", "someimage", null).getRegistry());
            Assert.AreEqual(
                "registry.hub.docker.com",
                ImageReference.of("registry.hub.docker.com", "someimage", null).getRegistry());
            Assert.AreEqual("gcr.io", ImageReference.of("gcr.io", "someimage", null).getRegistry());
        }

        private void verifyParse(string registry, string repository, string tagSeparator, string tag)
        {
            // Gets the expected parsed components.
            string expectedRegistry = registry;
            if (Strings.isNullOrEmpty(expectedRegistry))
            {
                expectedRegistry = "registry-1.docker.io";
            }
            string expectedRepository = repository;
            if ("registry-1.docker.io".Equals(expectedRegistry) && repository.indexOf('/') < 0)
            {
                expectedRepository = "library/" + expectedRepository;
            }
            string expectedTag = tag;
            if (Strings.isNullOrEmpty(expectedTag))
            {
                expectedTag = "latest";
            }

            // Builds the image reference to parse.
            StringBuilder imageReferenceBuilder = new StringBuilder();
            if (!Strings.isNullOrEmpty(registry))
            {
                imageReferenceBuilder.append(registry).append('/');
            }
            imageReferenceBuilder.append(repository);
            if (!Strings.isNullOrEmpty(tag))
            {
                imageReferenceBuilder.append(tagSeparator).append(tag);
            }

            ImageReference imageReference = ImageReference.parse(imageReferenceBuilder.toString());

            Assert.AreEqual(expectedRegistry, imageReference.getRegistry());
            Assert.AreEqual(expectedRepository, imageReference.getRepository());
            Assert.AreEqual(expectedTag, imageReference.getTag());
        }
    }
}