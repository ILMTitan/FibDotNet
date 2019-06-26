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
using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Tests for {@link BlobChecker}. */
    public class BlobCheckerTest
    {
        private readonly RegistryEndpointRequestProperties fakeRegistryEndpointRequestProperties =
            new RegistryEndpointRequestProperties("someServerUrl", "someImageName");

        private BlobChecker testBlobChecker;
        private DescriptorDigest fakeDigest;

        [SetUp]
        public void setUpFakes()
        {
            fakeDigest =
                DescriptorDigest.fromHash(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            testBlobChecker = new BlobChecker(fakeRegistryEndpointRequestProperties, new BlobDescriptor(fakeDigest));
        }

        [Test]
        public async Task testHandleResponseAsync()
        {
            HttpResponseMessage mockHttpResponseException = new HttpResponseMessage()
            {
                Content = new StringContent("")
            };

            bool result = await testBlobChecker.handleResponseAsync(mockHttpResponseException).ConfigureAwait(false);

            Assert.IsTrue(result);
        }

        [Test]
        public async Task testHandleResponse_noContentLengthAsync()
        {
            HttpResponseMessage mockResponse = new HttpResponseMessage()
            {
                Content = new StringContent("")
                {
                    Headers = { ContentLength = null }
                }
            };

            try
            {
                await testBlobChecker.handleResponseAsync(mockResponse).ConfigureAwait(false);
                Assert.Fail("Should throw exception if Content-Length header is not present");
            }
            catch (RegistryErrorException ex)
            {
                Assert.That(
                    ex.getMessage(), Does.Contain("Did not receive Content-Length header"));
            }
        }

        [Test]
        public async Task testHandleHttpResponseExceptionAsync()
        {
            ErrorResponseTemplate emptyErrorResponseTemplate =
                new ErrorResponseTemplate()
                    .AddError(new ErrorEntryTemplate(ErrorCode.BlobUnknown, "some message"));
            HttpResponseMessage mockHttpResponseException = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonTemplateMapper.toUtf8String(emptyErrorResponseTemplate))
            };

            bool result =
                await testBlobChecker.handleHttpResponseExceptionAsync(mockHttpResponseException).ConfigureAwait(false);

            Assert.IsFalse(result);
        }

        [Test]
        public async Task testHandleHttpResponseException_hasOtherErrorsAsync()
        {
            ErrorResponseTemplate emptyErrorResponseTemplate =
                new ErrorResponseTemplate()
                    .AddError(new ErrorEntryTemplate(ErrorCode.BlobUnknown, "some message"))
                    .AddError(new ErrorEntryTemplate(ErrorCode.ManifestUnknown.name(), "some message"));
            HttpResponseMessage mockHttpResponseException = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonTemplateMapper.toUtf8String(emptyErrorResponseTemplate))
            };

            try
            {
                await testBlobChecker.handleHttpResponseExceptionAsync(mockHttpResponseException).ConfigureAwait(false);
                Assert.Fail("Non-BLOB_UNKNOWN errors should not be handled");
            }
            catch (HttpResponseException ex)
            {
                Assert.AreEqual(mockHttpResponseException, ex.Cause);
            }
        }

        [Test]
        public async Task testHandleHttpResponseException_notBlobUnknownAsync()
        {
            ErrorResponseTemplate emptyErrorResponseTemplate = new ErrorResponseTemplate();
            HttpResponseMessage mockHttpResponseException = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonTemplateMapper.toUtf8String(emptyErrorResponseTemplate))
            };

            try
            {
                await testBlobChecker.handleHttpResponseExceptionAsync(mockHttpResponseException).ConfigureAwait(false);
                Assert.Fail("Non-BLOB_UNKNOWN errors should not be handled");
            }
            catch (HttpResponseException ex)
            {
                Assert.AreEqual(mockHttpResponseException, ex.Cause);
            }
        }

        [Test]
        public async Task testHandleHttpResponseException_invalidStatusCodeAsync()
        {
                HttpResponseMessage mockHttpResponseException = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            try
            {
                await testBlobChecker.handleHttpResponseExceptionAsync(mockHttpResponseException).ConfigureAwait(false);
                Assert.Fail("Non-404 status codes should not be handled");
            }
            catch (HttpResponseException ex)
            {
                Assert.AreEqual(mockHttpResponseException, ex.Cause);
            }
        }

        [Test]
        public void testGetApiRoute()
        {
            Assert.AreEqual(
                new Uri("http://someApiBase/someImageName/blobs/" + fakeDigest),
                testBlobChecker.getApiRoute("http://someApiBase/"));
        }

        [Test]
        public void testGetContent()
        {
            Assert.IsNull(testBlobChecker.getContent());
        }

        [Test]
        public void testGetAccept()
        {
            Assert.AreEqual(0, testBlobChecker.getAccept().size());
        }

        [Test]
        public void testGetActionDescription()
        {
            Assert.AreEqual(
                "check BLOB exists for someServerUrl/someImageName with digest " + fakeDigest,
                testBlobChecker.getActionDescription());
        }

        [Test]
        public void testGetHttpMethod()
        {
            Assert.AreEqual(HttpMethod.Head, testBlobChecker.getHttpMethod());
        }
    }
}
