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

using Jib.Net.Core.Global;
using Jib.Net.Core.Registry;
using Jib.Net.Core.Registry.Json;
using Moq;
using NUnit.Framework;
using System.Net.Http;

namespace Jib.Net.Core.Unit.Tests.Registry
{
    /** Tests for {@link RegistryErrorExceptionBuilder}. */
    public class RegistryErrorExceptionBuilderTest
    {
        private readonly HttpResponseMessage mockHttpResponseException = Mock.Of<HttpResponseMessage>();

        [Test]
        public void TestAddErrorEntry()
        {
            RegistryErrorExceptionBuilder builder =
                new RegistryErrorExceptionBuilder("do something", mockHttpResponseException);

            builder.AddReason(
                new ErrorEntryTemplate(ErrorCode.ManifestInvalid, "manifest invalid"));
            builder.AddReason(new ErrorEntryTemplate(ErrorCode.BlobUnknown, "blob unknown"));
            builder.AddReason(
                new ErrorEntryTemplate(ErrorCode.ManifestUnknown, "manifest unknown"));
            builder.AddReason(new ErrorEntryTemplate(ErrorCode.TagInvalid, "tag invalid"));
            builder.AddReason(
                new ErrorEntryTemplate(ErrorCode.ManifestUnverified, "manifest unverified"));
            builder.AddReason(
                new ErrorEntryTemplate(ErrorCode.Unsupported, "some other error happened"));
            builder.AddReason(new ErrorEntryTemplate(null, "some unknown error happened"));

            try
            {
                throw builder.Build();
            }
            catch (RegistryErrorException ex)
            {
                Assert.AreEqual(
                    "Tried to do something but failed because: manifest invalid (something went wrong), blob unknown (something went wrong), manifest unknown, tag invalid, manifest unverified, other: some other error happened, unknown: some unknown error happened | If this is a bug, please file an issue at https://github.com/GoogleContainerTools/jib/issues/new",
                    ex.Message);
            }
        }
    }
}
