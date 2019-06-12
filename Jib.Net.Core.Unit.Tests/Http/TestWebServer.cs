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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.docker;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;

namespace com.google.cloud.tools.jib.http
{












    /** Simple local web server for testing. */
    public class TestWebServer : IDisposable
    {
        private readonly bool https;
        private readonly TcpListener serverSocket;
        private readonly object serveTask;
        private readonly SemaphoreSlim threadStarted = new SemaphoreSlim(0);
        private readonly StringBuilder inputRead = new StringBuilder();

        public TestWebServer(bool https)
        {
            this.https = https;
            serverSocket = createServerSocket(https);
            serveTask = serve200();
            threadStarted.acquire();
        }

        public string getEndpoint()
        {
            var host = serverSocket.LocalEndpoint as IPEndPoint;
            return (https ? "https" : "http") + "://" + host.Address + ":" + host.Port;
        }

        public void Dispose()
        {
            serverSocket.Stop();
        }

        private TcpListener createServerSocket(bool https)
        {
            return new TcpListener(IPAddress.Loopback, 0);
        }

        private async Task serve200()
        {
            threadStarted.release();
            serverSocket.Start();
            using (var socket = await serverSocket.AcceptTcpClientAsync())
            {
                Stream @in = socket.GetStream();
                TextReader reader = new StreamReader(@in, StandardCharsets.UTF_8);
                for (string line = await reader.ReadLineAsync();
                    line != null && !line.isEmpty(); // An empty line marks the end of an HTTP request.
                    line = await reader.ReadLineAsync())
                {
                    inputRead.append(line + "\n");
                }

                const string response = "HTTP/1.1 200 OK\nContent-Length:12\n\nHello World!";
                socket.GetStream().write(response.getBytes(StandardCharsets.UTF_8));
            }
        }

        public string getInputRead()
        {
            return inputRead.toString();
        }
    }
}
