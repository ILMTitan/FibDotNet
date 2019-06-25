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

using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.registry.json;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System.Net.Http;

namespace com.google.cloud.tools.jib.registry
{
    /** Tests for {@link RegistryErrorExceptionBuilder}. */
    public class RegistryErrorExceptionBuilderTest
    {
        private readonly HttpResponseMessage mockHttpResponseException = Mock.Of<HttpResponseMessage>();

        [Test]
        public void testAddErrorEntry()
        {
            RegistryErrorExceptionBuilder builder =
                new RegistryErrorExceptionBuilder("do something", mockHttpResponseException);

            builder.addReason(
                new ErrorEntryTemplate(ErrorCode.ManifestInvalid.name(), "manifest invalid"));
            builder.addReason(new ErrorEntryTemplate(ErrorCode.BlobUnknown.name(), "blob unknown"));
            builder.addReason(
                new ErrorEntryTemplate(ErrorCode.ManifestUnknown.name(), "manifest unknown"));
            builder.addReason(new ErrorEntryTemplate(ErrorCode.TagInvalid.name(), "tag invalid"));
            builder.addReason(
                new ErrorEntryTemplate(ErrorCode.ManifestUnverified.name(), "manifest unverified"));
            builder.addReason(
                new ErrorEntryTemplate(ErrorCode.Unsupported.name(), "some other error happened"));
            builder.addReason(new ErrorEntryTemplate(null, "some unknown error happened"));

            try
            {
                throw builder.build();
            }
            catch (RegistryErrorException ex)
            {
                Assert.AreEqual(
                    "Tried to do something but failed because: manifest invalid (something went wrong), blob unknown (something went wrong), manifest unknown, tag invalid, manifest unverified, other: some other error happened, unknown: some unknown error happened | If this is a bug, please file an issue at https://github.com/GoogleContainerTools/jib/issues/new",
                    ex.getMessage());
            }
        }
    }
}
