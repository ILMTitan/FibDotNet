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

using Jib.Net.Core.Http;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jib.Net.Core.Unit.Tests.Http
{
    /**
     * Mock {@link Connection} used for testing. Normally, you would use {@link
     * org.mockito.Mockito#mock}; this class is intended to examine the {@link Request) object by
     * calling its non-public package-protected methods.
     */
    public sealed class MockConnection : IConnection
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseSupplier;

        public MockConnection(Func<HttpRequestMessage, HttpResponseMessage> responseSupplier)
        {
            this.responseSupplier = responseSupplier;
        }

        public void Dispose()
        {
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return Task.FromResult(responseSupplier(request));
        }
    }
}
