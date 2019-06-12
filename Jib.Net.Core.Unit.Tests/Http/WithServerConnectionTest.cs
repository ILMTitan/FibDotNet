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

namespace com.google.cloud.tools.jib.http {










/** Tests for {@link Connection} using an actual local server. */
public class WithServerConnectionTest {

  [Test]
  public void testGet()
      {
    using(TestWebServer server = new TestWebServer(false))
    using(Connection connection =
            Connection.getConnectionFactory().apply(new Uri(server.getEndpoint())))
    {

                HttpResponseMessage response = connection.send(new HttpRequestMessage());

      Assert.AreEqual(200, response.getStatusCode());
      CollectionAssert.AreEqual(
          "Hello World!".getBytes(StandardCharsets.UTF_8),
          ByteStreams.toByteArray(response.getBody()));
    }
  }

  [Test]
  public void testErrorOnSecondSend()
      {
    using(TestWebServer server = new TestWebServer(false))
    using(Connection connection =
            Connection.getConnectionFactory().apply(new Uri(server.getEndpoint())))
    {

      connection.send(new Request.Builder().build());
      try {
        connection.send(new Request.Builder().build());
        Assert.Fail("Should fail on the second send");
      } catch (InvalidOperationException ex) {
        Assert.AreEqual("Connection can send only one request", ex.getMessage());
      }
    }
  }

  [Test]
  public void testSecureConnectionOnInsecureHttpsServer()
      {
    using(TestWebServer server = new TestWebServer(true))
    using(Connection connection =
            Connection.getConnectionFactory().apply(new Uri(server.getEndpoint())))
    {

      try {
        connection.send(new Request.Builder().build());
        Assert.Fail("Should fail if cannot verify peer");
      } catch (SSLException ex) {
        Assert.IsNotNull(ex.getMessage());
      }
    }
  }

  [Test]
  public void testInsecureConnection()
      {
    using(TestWebServer server = new TestWebServer(true))
    using(Connection connection =
            Connection.getInsecureConnectionFactory().apply(new Uri(server.getEndpoint())))
    {

      HttpResponseMessage response = connection.send(new Request.Builder().build());

      Assert.AreEqual(200, response.getStatusCode());
      CollectionAssert.AreEqual(
          "Hello World!".getBytes(StandardCharsets.UTF_8),
          ByteStreams.toByteArray(response.getBody()));
    }
  }
}
}
