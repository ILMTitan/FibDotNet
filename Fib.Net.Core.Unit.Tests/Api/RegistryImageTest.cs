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
using Moq;
using NUnit.Framework;

namespace Fib.Net.Core.Unit.Tests.Api
{
    /** Tests for {@link RegistryImage}. */
    public class RegistryImageTest
    {
        private readonly CredentialRetriever mockCredentialRetriever = Mock.Of<CredentialRetriever>();

        [Test]
        public void TestGetters_default()
        {
            RegistryImage image = RegistryImage.Named("registry/image");

            Assert.AreEqual("registry/image", image.GetImageReference().ToString());
            Assert.AreEqual(0, image.GetCredentialRetrievers().Count);
        }

        [Test]
        public void TestGetters()
        {
            RegistryImage image =
                RegistryImage.Named("registry/image")
                    .AddCredentialRetriever(mockCredentialRetriever)
                    .AddCredential("username", "password");

            Assert.AreEqual(2, image.GetCredentialRetrievers().Count);
            Assert.AreSame(mockCredentialRetriever, image.GetCredentialRetrievers()[0]);
            Assert.AreEqual(
                Credential.From("username", "password"),
                image.GetCredentialRetrievers()[1].Retrieve().Get());
        }
    }
}