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

using com.google.cloud.tools.jib.docker;
using Jib.Net.Core;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Runs a local registry. */
    public sealed class LocalRegistry : IDisposable
    {
        private readonly string containerName =
            "registry-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).ToLowerInvariant();
        private readonly int port;
        private readonly string username;
        private readonly string password;

        public LocalRegistry(int port) : this(port, null, null) { }

        public LocalRegistry(int port, string username, string password)
        {
            this.port = port;
            this.username = username;
            this.password = password;
        }

        /** Starts the registry */
        public async Task startAsync()
        {
            // Runs the Docker registry.
            List<string> dockerTokens = new List<string> {
                "run",
                "--rm",
                "-d",
                "-p",
                port + ":5000",
                "--name",
                containerName
            };
            if (username != null && password != null)
            {
                // Generate the htpasswd file to store credentials
                string credentialString =
                    new Command(
                            "docker",
                            "run",
                            "--rm",
                            "--entrypoint",
                            "htpasswd",
                            "registry",
                            "-Bbn",
                            username,
                            password)
                        .run();
                // Creates the temporary directory in /tmp since that is one of the default directories
                // mounted into Docker.
                // See: https://docs.docker.com/docker-for-mac/osxfs
                SystemPath tempFolder = Files.createTempDirectory(Paths.get(Path.GetTempPath()), "");
                Files.write(
                    tempFolder.Resolve("htpasswd"), credentialString.getBytes(Encoding.UTF8));

                // Run the Docker registry
                dockerTokens.addAll(
                    Arrays.asList(
                        "-v",
                        // Volume mount used for storing credentials
                        tempFolder + ":/auth",
                        "-e",
                        "REGISTRY_AUTH=htpasswd",
                        "-e",
                        "REGISTRY_AUTH_HTPASSWD_REALM=\"Registry Realm\"",
                        "-e",
                        "REGISTRY_AUTH_HTPASSWD_PATH=/auth/htpasswd"));
            }
            dockerTokens.add("registry");
            new Command("docker", dockerTokens).run();
            await waitUntilReadyAsync().ConfigureAwait(false);
        }

        /** Stops the registry. */
        public void stop()
        {
            try
            {
                logout();
                new Command("docker", "stop", containerName).run();
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is IOException)
            {
                throw new Exception("Could not stop local registry fully: " + containerName, ex);
            }
        }

        /**
         * Pulls an image.
         *
         * @param from the image reference to pull
         * @throws IOException if the pull command fails
         * @throws InterruptedException if the pull command is interrupted
         */
        public void pull(string from)
        {
            login();
            new Command("docker", "pull", from).run();
            logout();
        }

        /**
         * Pulls an image and pushes it to the local registry under a new tag.
         *
         * @param from the image reference to pull
         * @param to the new location of the image (i.e. {@code localhost:[port]/[to]}
         * @throws IOException if the commands fail
         * @throws InterruptedException if the commands are interrupted
         */
        public void pullAndPushToLocal(string from, string to)
        {
            login();
            new Command("docker", "pull", from).run();
            new Command("docker", "tag", from, "localhost:" + port + "/" + to).run();
            new Command("docker", "push", "localhost:" + port + "/" + to).run();
            logout();
        }

        private void login()
        {
            if (username != null && password != null)
            {
                new Command("docker", string.Join(' ', new[] { "login", "localhost:" + port, "-u", username, "--password-stdin" }))
                    .run(password.getBytes(Encoding.UTF8));
            }
        }

        private void logout()
        {
            if (username != null && password != null)
            {
                new Command("docker", "logout", "localhost:" + port).run();
            }
        }

        private async Task waitUntilReadyAsync()
        {
            Uri queryUrl = new Uri("http://localhost:" + port + "/v2/_catalog");

            for (int i = 0; i < 40; i++)
            {
                try
                {
                    var client = new HttpClient();
                    var message = await client.GetAsync(queryUrl).ConfigureAwait(false);
                    var code = message.StatusCode;
                    if (code == HttpStatusCode.OK || code == HttpStatusCode.Unauthorized)
                    {
                        return;
                    }
                }
                catch (IOException) { }
                catch (HttpRequestException) { }
                Thread.Sleep(250);
            }
        }

        public void Dispose()
        {
            stop();
        }
    }
}
