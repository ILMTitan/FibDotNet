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

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.http
{












    /** Simple local web server for testing. */
    public class TestWebServer : IDisposable
    {
        private readonly bool https;
        private readonly TcpListener serverSocket;
        private readonly Task serveTask;
        private readonly SemaphoreSlim threadStarted = new SemaphoreSlim(0);
        private readonly StringBuilder inputRead = new StringBuilder();

        public TestWebServer(bool https)
        {
            this.https = https;
            serverSocket = createServerSocket(https);
            serveTask = serve200Async();
            threadStarted.acquire();
        }
        public string GetAddressAndPort()
        {
            var host = serverSocket.LocalEndpoint;
            return host.ToString();
        }

        public void Dispose()
        {
            if (serveTask.IsFaulted)
            {
                TestContext.Out.WriteLine("-----------------Server Error---------------");
                TestContext.Out.WriteLine(serveTask.Exception);
            }
            serverSocket.Stop();
        }

        private TcpListener createServerSocket(bool https)
        {
            return new TcpListener(IPAddress.Loopback, 0);
        }

        private async Task serve200Async()
        {
            threadStarted.release();
            serverSocket.Start();
            using (var socket = await serverSocket.AcceptTcpClientAsync().ConfigureAwait(false))
            {
                socket.NoDelay = true;
                Stream socketStream;

                if (https)
                {
                    var sslStream = new SslStream(socket.GetStream(), true);
                    SystemPath certFile = Resources.getResource("localhost.2.pfx");
                    X509Certificate2 serverCertificate = new X509Certificate2(certFile, "password");

                    await sslStream.AuthenticateAsServerAsync(serverCertificate, false, false).ConfigureAwait(false);
                    socketStream = sslStream;
                }
                else
                {
                    socketStream = socket.GetStream();
                }
                TextReader reader = new StreamReader(socketStream);
                string line;
                while (!string.IsNullOrWhiteSpace(line = await reader.ReadLineAsync().ConfigureAwait(false)))
                {
                    inputRead.AppendLine(line);
                }
                const string response = "HTTP/1.1 200 OK\nContent-Length:12\n\nHello World!";
                await socketStream.WriteAsync(response.getBytes(StandardCharsets.UTF_8)).ConfigureAwait(false);
            }
        }

        public string getInputRead()
        {
            return inputRead.ToString();
        }
    }
}
