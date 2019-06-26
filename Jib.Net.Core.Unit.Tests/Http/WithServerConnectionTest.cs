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
using System.Text;
using System.Runtime.ExceptionServices;

namespace com.google.cloud.tools.jib.http
{
    /** Tests for {@link Connection} using an actual local server. */
    public class WithServerConnectionTest
    {
        [Test]
        public async Task TestGetAsync()
        {
            using (TestWebServer server = new TestWebServer(false))
            using (Connection connection =
                    Connection.GetConnectionFactory()(new Uri("http://" + server.GetAddressAndPort())))
            {
                HttpResponseMessage response = await connection.SendAsync(new HttpRequestMessage()).ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                CollectionAssert.AreEqual(
                    Encoding.UTF8.GetBytes("Hello World!"),
                    ByteStreams.ToByteArray(await response.Content.ReadAsStreamAsync().ConfigureAwait(false)));
            }
        }

        [Test]
        public async Task TestSecureConnectionOnInsecureHttpsServerAsync()
        {
            using (TestWebServer server = new TestWebServer(true))
            using (Connection connection =
                Connection.GetConnectionFactory()(new Uri("https://" + server.GetAddressAndPort())))
            {
                try
                {
                    await connection.SendAsync(new HttpRequestMessage()).ConfigureAwait(false);
                    Assert.Fail("Should fail if cannot verify peer");
                }
                catch (HttpRequestException ex)
                {
                    Assert.IsInstanceOf<AuthenticationException>(ex.InnerException);
                }
            }
        }

        [Test]
        public async Task TestInsecureConnectionAsync()
        {
            using (TestWebServer server = new TestWebServer(true))
            using (Connection connection =
                    Connection.GetInsecureConnectionFactory()(new Uri("https://" + server.GetAddressAndPort())))
            {
                HttpResponseMessage response = await connection.SendAsync(new HttpRequestMessage()).ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                CollectionAssert.AreEqual(
                    Encoding.UTF8.GetBytes("Hello World!"),
                    ByteStreams.ToByteArray(await response.Content.ReadAsStreamAsync().ConfigureAwait(false)));
            }
        }
    }
}
