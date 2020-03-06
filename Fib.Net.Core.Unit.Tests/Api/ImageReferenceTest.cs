// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using Fib.Net.Core.Api;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fib.Net.Core.Unit.Tests.Api
{
    /** Tests for {@link ImageReference}. */

    public class ImageReferenceTest
    {
        private static readonly IList<string> goodRegistries =
            new[] { "some.domain---name.123.com:8080", "gcr.io", "localhost", null, "" };

        private static readonly IList<string> goodRepositories =
            new[] { "some123_abc/repository__123-456/name---here", "distroless/java", "repository" };

        private static readonly IList<string> goodTags = new[] { "some-.-.Tag", "", "latest", null };

        private static readonly IList<string> goodDigests = new[]
        {
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            null
        };

        private static readonly IList<string> badImageReferences = new[]
        {
            "",
            ":justsometag",
            "@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "repository@sha256:a",
            "repository@notadigest",
            "Repositorywithuppercase",
            "registry:8080/Repositorywithuppercase/repository:sometag",
            "domain.name:nonnumberport/repository",
            "domain.name:nonnumberport//:no-repository"
        };

        [Test]
        public void TestParse_pass()
        {
            foreach (string goodRegistry in goodRegistries)
            {
                foreach (string goodRepository in goodRepositories)
                {
                    foreach (string goodTag in goodTags)
                    {
                        VerifyParse(goodRegistry, goodRepository, ":", goodTag);
                    }
                    foreach (string goodDigest in goodDigests)
                    {
                        VerifyParse(goodRegistry, goodRepository, "@", goodDigest);
                    }
                }
            }
        }

        [Test]
        public void TestParse_dockerHub_official()
        {
            const string imageReferenceString = "busybox";
            ImageReference imageReference = ImageReference.Parse(imageReferenceString);

            Assert.AreEqual("registry-1.docker.io", imageReference.GetRegistry());
            Assert.AreEqual("library/busybox", imageReference.GetRepository());
            Assert.AreEqual("latest", imageReference.GetTag());
        }

        [Test]
        public void TestParse_dockerHub_user()
        {
            const string imageReferenceString = "someuser/someimage";
            ImageReference imageReference = ImageReference.Parse(imageReferenceString);

            Assert.AreEqual("registry-1.docker.io", imageReference.GetRegistry());
            Assert.AreEqual("someuser/someimage", imageReference.GetRepository());
            Assert.AreEqual("latest", imageReference.GetTag());
        }

        [Test]
        public void TestParse_invalid()
        {
            foreach (string badImageReference in badImageReferences)
            {
                try
                {
                    ImageReference.Parse(badImageReference);
                    Assert.Fail(badImageReference + " should not be a valid image reference");
                }
                catch (InvalidImageReferenceException ex)
                {
                    Assert.That(ex.Message, Does.Contain(badImageReference))
;
                }
            }
        }

        [Test]
        public void TestOf_smoke()
        {
            const string expectedRegistry = "someregistry";
            const string expectedRepository = "somerepository";
            const string expectedTag = "sometag";

            Assert.AreEqual(
                expectedRegistry,
                ImageReference.Of(expectedRegistry, expectedRepository, expectedTag).GetRegistry());
            Assert.AreEqual(
                expectedRepository,
                ImageReference.Of(expectedRegistry, expectedRepository, expectedTag).GetRepository());
            Assert.AreEqual(
                expectedTag, ImageReference.Of(expectedRegistry, expectedRepository, expectedTag).GetTag());
            Assert.AreEqual(
                "registry-1.docker.io",
                ImageReference.Of(null, expectedRepository, expectedTag).GetRegistry());
            Assert.AreEqual(
                "registry-1.docker.io", ImageReference.Of(null, expectedRepository, null).GetRegistry());
            Assert.AreEqual(
                "latest", ImageReference.Of(expectedRegistry, expectedRepository, null).GetTag());
            Assert.AreEqual("latest", ImageReference.Of(null, expectedRepository, null).GetTag());
            Assert.AreEqual(
                expectedRepository, ImageReference.Of(null, expectedRepository, null).GetRepository());
        }

        [Test]
        public void TestToString()
        {
            Assert.AreEqual("someimage", ImageReference.Of(null, "someimage", null).ToString());
            Assert.AreEqual("someimage", ImageReference.Of("", "someimage", "").ToString());
            Assert.AreEqual(
                "someotherimage", ImageReference.Of(null, "library/someotherimage", null).ToString());
            Assert.AreEqual(
                "someregistry/someotherimage",
                ImageReference.Of("someregistry", "someotherimage", null).ToString());
            Assert.AreEqual(
                "anotherregistry/anotherimage:sometag",
                ImageReference.Of("anotherregistry", "anotherimage", "sometag").ToString());

            Assert.AreEqual(
                "someimage@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                ImageReference.Of(
                        null,
                        "someimage",
                        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa").ToString());
            Assert.AreEqual(
                "gcr.io/distroless/java@sha256:b430543bea1d8326e767058bdab3a2482ea45f59d7af5c5c61334cd29ede88a1",
                ImageReference.Parse(
                        "gcr.io/distroless/java@sha256:b430543bea1d8326e767058bdab3a2482ea45f59d7af5c5c61334cd29ede88a1")
                    .ToString());
        }

        [Test]
        public void TestToStringWithTag()
        {
            Assert.AreEqual(
                "someimage:latest", ImageReference.Of(null, "someimage", null).ToStringWithTag());
            Assert.AreEqual(
                "someimage:latest", ImageReference.Of("", "someimage", "").ToStringWithTag());
            Assert.AreEqual(
                "someotherimage:latest",
                ImageReference.Of(null, "library/someotherimage", null).ToStringWithTag());
            Assert.AreEqual(
                "someregistry/someotherimage:latest",
                ImageReference.Of("someregistry", "someotherimage", null).ToStringWithTag());
            Assert.AreEqual(
                "anotherregistry/anotherimage:sometag",
                ImageReference.Of("anotherregistry", "anotherimage", "sometag").ToStringWithTag());
        }

        [Test]
        public void TestIsTagDigest()
        {
            Assert.IsFalse(ImageReference.Of(null, "someimage", null).IsTagDigest());
            Assert.IsFalse(ImageReference.Of(null, "someimage", "latest").IsTagDigest());
            Assert.IsTrue(
                ImageReference.Of(
                        null,
                        "someimage",
                        "sha256:b430543bea1d8326e767058bdab3a2482ea45f59d7af5c5c61334cd29ede88a1")
                    .IsTagDigest());
            Assert.IsTrue(
                ImageReference.Parse(
                        "someimage@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
                    .IsTagDigest());
        }

        [Test]
        public void TestIsScratch()
        {
            Assert.IsTrue(ImageReference.Scratch().IsScratch());
            Assert.IsFalse(ImageReference.Of("", "scratch", "").IsScratch());
            Assert.IsFalse(ImageReference.Of(null, "scratch", null).IsScratch());
        }

        [Test]
        public void TestGetRegistry()
        {
            Assert.AreEqual(
                "registry-1.docker.io", ImageReference.Of(null, "someimage", null).GetRegistry());
            Assert.AreEqual(
                "registry-1.docker.io", ImageReference.Of("docker.io", "someimage", null).GetRegistry());
            Assert.AreEqual(
                "index.docker.io", ImageReference.Of("index.docker.io", "someimage", null).GetRegistry());
            Assert.AreEqual(
                "registry.hub.docker.com",
                ImageReference.Of("registry.hub.docker.com", "someimage", null).GetRegistry());
            Assert.AreEqual("gcr.io", ImageReference.Of("gcr.io", "someimage", null).GetRegistry());
        }

        private void VerifyParse(string registry, string repository, string tagSeparator, string tag)
        {
            // Gets the expected parsed components.
            string expectedRegistry = registry;
            if (string.IsNullOrEmpty(expectedRegistry))
            {
                expectedRegistry = "registry-1.docker.io";
            }
            string expectedRepository = repository;
            if ("registry-1.docker.io" == expectedRegistry && repository.IndexOf('/', StringComparison.Ordinal) < 0)
            {
                expectedRepository = "library/" + expectedRepository;
            }
            string expectedTag = tag;
            if (string.IsNullOrEmpty(expectedTag))
            {
                expectedTag = "latest";
            }

            // Builds the image reference to parse.
            StringBuilder imageReferenceBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(registry))
            {
                imageReferenceBuilder.Append(registry).Append('/');
            }
            imageReferenceBuilder.Append(repository);
            if (!string.IsNullOrEmpty(tag))
            {
                imageReferenceBuilder.Append(tagSeparator).Append(tag);
            }

            ImageReference imageReference = ImageReference.Parse(imageReferenceBuilder.ToString());

            Assert.AreEqual(expectedRegistry, imageReference.GetRegistry());
            Assert.AreEqual(expectedRepository, imageReference.GetRepository());
            Assert.AreEqual(expectedTag, imageReference.GetTag());
        }
    }
}