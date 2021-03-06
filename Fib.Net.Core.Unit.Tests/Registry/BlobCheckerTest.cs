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
using Fib.Net.Core.Blob;
using Fib.Net.Core.Json;
using Fib.Net.Core.Registry;
using Fib.Net.Core.Registry.Json;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fib.Net.Core.Unit.Tests.Registry
{
    /** Tests for {@link BlobChecker}. */
    public class BlobCheckerTest
    {
        private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
            new RegistryEndpointRequestProperties("someServerUrl", "someImageName");

        private BlobChecker testBlobChecker;
        private DescriptorDigest fakeDigest;

        [SetUp]
        public void SetUpFakes()
        {
            fakeDigest =
                DescriptorDigest.FromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            testBlobChecker = new BlobChecker(fakeRegistryEndpointRequestProperties, new BlobDescriptor(fakeDigest));
        }

        [Test]
        public async Task TestHandleResponseAsync()
        {
            using (HttpResponseMessage mockHttpResponseException = new HttpResponseMessage()
            {
                Content = new StringContent("")
            })
            {
                bool result = await testBlobChecker.HandleResponseAsync(mockHttpResponseException).ConfigureAwait(false);

                Assert.IsTrue(result);
            }
        }

        [Test]
        public async Task TestHandleResponse_noContentLengthAsync()
        {
            using (HttpResponseMessage mockResponse = new HttpResponseMessage()
            {
                Content = new StringContent("")
                {
                    Headers = { ContentLength = null }
                }
            })
            {
                try
                {
                    await testBlobChecker.HandleResponseAsync(mockResponse).ConfigureAwait(false);
                    Assert.Fail("Should throw exception if Content-Length header is not present");
                }
                catch (RegistryErrorException ex)
                {
                    Assert.That(
                        ex.Message, Does.Contain("Did not receive Content-Length header"));
                }
            }
        }

        [Test]
        public async Task TestHandleHttpResponseExceptionAsync()
        {
            ErrorResponseTemplate emptyErrorResponseTemplate =
                new ErrorResponseTemplate()
                    .AddError(new ErrorEntryTemplate(ErrorCode.BlobUnknown, "some message"));
            using (HttpResponseMessage mockHttpResponseException = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonTemplateMapper.ToUtf8String(emptyErrorResponseTemplate))
            })
            {
                bool result =
                    await testBlobChecker.HandleHttpResponseExceptionAsync(mockHttpResponseException).ConfigureAwait(false);

                Assert.IsFalse(result);
            }
        }

        [Test]
        public async Task TestHandleHttpResponseException_hasOtherErrorsAsync()
        {
            ErrorResponseTemplate emptyErrorResponseTemplate =
                new ErrorResponseTemplate()
                    .AddError(new ErrorEntryTemplate(ErrorCode.BlobUnknown, "some message"))
                    .AddError(new ErrorEntryTemplate(ErrorCode.ManifestUnknown, "some message"));
            using (HttpResponseMessage mockHttpResponseException = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonTemplateMapper.ToUtf8String(emptyErrorResponseTemplate))
            })
            {
                try
                {
                    await testBlobChecker.HandleHttpResponseExceptionAsync(mockHttpResponseException).ConfigureAwait(false);
                    Assert.Fail("Non-BLOB_UNKNOWN errors should not be handled");
                }
                catch (HttpResponseException ex)
                {
                    Assert.AreEqual(mockHttpResponseException, ex.Cause);
                }
            }
        }

        [Test]
        public async Task TestHandleHttpResponseException_notBlobUnknownAsync()
        {
            ErrorResponseTemplate emptyErrorResponseTemplate = new ErrorResponseTemplate();
            using (HttpResponseMessage mockHttpResponseException = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonTemplateMapper.ToUtf8String(emptyErrorResponseTemplate))
            })
            {
                try
                {
                    await testBlobChecker.HandleHttpResponseExceptionAsync(mockHttpResponseException).ConfigureAwait(false);
                    Assert.Fail("Non-BLOB_UNKNOWN errors should not be handled");
                }
                catch (HttpResponseException ex)
                {
                    Assert.AreEqual(mockHttpResponseException, ex.Cause);
                }
            }
        }

        [Test]
        public async Task TestHandleHttpResponseException_invalidStatusCodeAsync()
        {
            using (HttpResponseMessage mockHttpResponseException = new HttpResponseMessage(HttpStatusCode.InternalServerError))
            {
                try
                {
                    await testBlobChecker.HandleHttpResponseExceptionAsync(mockHttpResponseException).ConfigureAwait(false);
                    Assert.Fail("Non-404 status codes should not be handled");
                }
                catch (HttpResponseException ex)
                {
                    Assert.AreEqual(mockHttpResponseException, ex.Cause);
                }
            }
        }

        [Test]
        public void TestGetApiRoute()
        {
            Assert.AreEqual(
                new Uri("http://someApiBase/someImageName/blobs/" + fakeDigest),
                testBlobChecker.GetApiRoute("http://someApiBase/"));
        }

        [Test]
        public void TestGetContent()
        {
            Assert.IsNull(testBlobChecker.GetContent());
        }

        [Test]
        public void TestGetAccept()
        {
            Assert.AreEqual(0, testBlobChecker.GetAccept().Count);
        }

        [Test]
        public void TestGetActionDescription()
        {
            Assert.AreEqual(
                "check BLOB exists for someServerUrl/someImageName with digest " + fakeDigest,
                testBlobChecker.GetActionDescription());
        }

        [Test]
        public void TestGetHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Head, testBlobChecker.GetHttpMethod());
        }
    }
}
