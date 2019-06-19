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

using NUnit.Framework;
using Jib.Net.Core.Global;
using System;
using System.Net.Http;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.hash;
using com.google.cloud.tools.jib.registry;
using System.Net;
using System.Threading.Tasks;
using System.Security.Authentication;

namespace com.google.cloud.tools.jib.http
{
    /** Tests for {@link Connection} using an actual local server. */
    public class WithServerConnectionTest
    {
        [Test]
        public async Task testGetAsync()
        {
            using (TestWebServer server = new TestWebServer(false))
            using (Connection connection =
                    Connection.getConnectionFactory().apply(new Uri("http://" + server.GetAddressAndPort())))
            {
                HttpResponseMessage response = await connection.sendAsync(new HttpRequestMessage()).ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.OK, response.getStatusCode());
                CollectionAssert.AreEqual(
                    "Hello World!".getBytes(StandardCharsets.UTF_8),
                    ByteStreams.toByteArray(await response.getBodyAsync().ConfigureAwait(false)));
            }
        }

        [Test]
        public async Task testSecureConnectionOnInsecureHttpsServerAsync()
        {
            using (TestWebServer server = new TestWebServer(true))
            using (Connection connection =
                Connection.getConnectionFactory().apply(new Uri("https://" + server.GetAddressAndPort())))
            {
                try
                {
                    await connection.sendAsync(new HttpRequestMessage()).ConfigureAwait(false);
                    Assert.Fail("Should fail if cannot verify peer");
                }
                catch (HttpRequestException ex)
                {
                    Assert.IsInstanceOf<AuthenticationException>(ex.InnerException);
                }
            }
        }

        [Test]
        public async Task testInsecureConnectionAsync()
        {
            using (TestWebServer server = new TestWebServer(true))
            using (Connection connection =
                    Connection.getInsecureConnectionFactory().apply(new Uri("https://"+server.GetAddressAndPort())))
            {
                HttpResponseMessage response = await connection.sendAsync(new HttpRequestMessage()).ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.OK, response.getStatusCode());
                CollectionAssert.AreEqual(
                    "Hello World!".getBytes(StandardCharsets.UTF_8),
                    ByteStreams.toByteArray(await response.getBodyAsync().ConfigureAwait(false)));
            }
        }
    }
}
