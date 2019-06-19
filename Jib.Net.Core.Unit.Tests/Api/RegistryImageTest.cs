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

using com.google.cloud.tools.jib.cache;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link RegistryImage}. */
    public class RegistryImageTest
    {
        private readonly CredentialRetriever mockCredentialRetriever = Mock.Of<CredentialRetriever>();

        [Test]
        public void testGetters_default()
        {
            RegistryImage image = RegistryImage.named("registry/image");

            Assert.AreEqual("registry/image", image.getImageReference().toString());
            Assert.AreEqual(0, image.getCredentialRetrievers().size());
        }

        [Test]
        public void testGetters()
        {
            RegistryImage image =
                RegistryImage.named("registry/image")
                    .addCredentialRetriever(mockCredentialRetriever)
                    .addCredential("username", "password");

            Assert.AreEqual(2, image.getCredentialRetrievers().size());
            Assert.AreSame(mockCredentialRetriever, image.getCredentialRetrievers().get(0));
            Assert.AreEqual(
                Credential.from("username", "password"),
                image.getCredentialRetrievers().get(1).retrieve().get());
        }
    }
}