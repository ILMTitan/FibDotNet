// Copyright 2018 Google Inc.
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

using Fib.Net.Core.Registry;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fib.Net.Core.Unit.Tests.Registry
{
    /** Test for {@link ErrorReponseUtil}. */
    public class ErrorResponseUtilTest
    {
        [Test]
        public async Task TestGetErrorCode_knownErrorCodeAsync()
        {
            using (HttpResponseMessage httpResponseException = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"errors\":[{\"code\":\"MANIFEST_INVALID\",\"message\":\"manifest invalid\",\"detail\":{}}]}")
            })
            {
                Assert.AreEqual(
                    ErrorCode.ManifestInvalid, await ErrorResponseUtil.GetErrorCodeAsync(httpResponseException).ConfigureAwait(false));
            }
        }

        /** An unknown {@link ErrorCodes} should cause original exception to be rethrown. */
        [Test]
        public async Task TestGetErrorCode_unknownErrorCodeAsync()
        {
            using (HttpResponseMessage httpResponseException = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                "{\"errors\":[{\"code\":\"INVALID_ERROR_CODE\",\"message\":\"invalid code\",\"detail\":{}}]}")
            })
            {
                try
                {
                    await ErrorResponseUtil.GetErrorCodeAsync(httpResponseException).ConfigureAwait(false);
                    Assert.Fail();
                }
                catch (HttpResponseException ex)
                {
                    Assert.AreSame(httpResponseException, ex.Cause);
                }
            }
        }

        /** Multiple error objects should cause original exception to be rethrown. */
        [Test]
        public async Task TestGetErrorCode_multipleErrorsAsync()
        {
            using (HttpResponseMessage httpResponseException = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                "{\"errors\":["
                    + "{\"code\":\"MANIFEST_INVALID\",\"message\":\"message 1\",\"detail\":{}},"
                    + "{\"code\":\"TAG_INVALID\",\"message\":\"message 2\",\"detail\":{}}"
                    + "]}")
            })
            {
                try
                {
                    await ErrorResponseUtil.GetErrorCodeAsync(httpResponseException).ConfigureAwait(false);
                    Assert.Fail();
                }
                catch (HttpResponseException ex)
                {
                    Assert.AreSame(httpResponseException, ex.Cause);
                }
            }
        }

        /** An non-error object should cause original exception to be rethrown. */
        [Test]
        public async Task TestGetErrorCode_invalidErrorObjectAsync()
        {
            using (HttpResponseMessage httpResponseException = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"type\":\"other\",\"message\":\"some other object\"}")
            })
            {
                try
                {
                    await ErrorResponseUtil.GetErrorCodeAsync(httpResponseException).ConfigureAwait(false);
                    Assert.Fail();
                }
                catch (HttpResponseException ex)
                {
                    Assert.AreSame(httpResponseException, ex.Cause);
                }
            }
        }
    }
}
