// Copyright 2017 Google LLC.
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
using Fib.Net.Core.Docker;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Json;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Fib.Net.Core.Registry.Credentials
{
    /**
     * Retrieves Docker credentials with a Docker credential helper.
     *
     * @see <a
     *     href="https://github.com/docker/docker-credential-helpers">https://github.com/docker/docker-credential-helpers</a>
     */
    public class DockerCredentialHelper : IDockerCredentialHelper
    {
        public static readonly string CredentialHelperPrefix = "docker-credential-";

        private readonly string registry;
        private readonly SystemPath credentialHelper;

        /** Template for a Docker credential helper output. */
        [JsonObject]
        private class DockerCredentialsTemplate
        {
            public string Username { get; }

            public string Secret { get; }

            [JsonConstructor]
            public DockerCredentialsTemplate(string username, string secret)
            {
                Username = username;
                Secret = secret;
            }
        }

        /**
         * Constructs a new {@link DockerCredentialHelper}.
         *
         * @param serverUrl the server Uri to pass into the credential helper
         * @param credentialHelper the path to the credential helper executable
         */
        public DockerCredentialHelper(string registry, SystemPath credentialHelper)
        {
            this.registry = registry;
            this.credentialHelper = credentialHelper;
        }

        public DockerCredentialHelper(string registry, string credentialHelperSuffix) : this(registry, Paths.Get(CredentialHelperPrefix + credentialHelperSuffix))
        {
        }

        public static DockerCredentialHelper Create(string registry, SystemPath credentialHelper)
        {
            return new DockerCredentialHelper(registry, credentialHelper);
        }

        /**
         * Calls the credential helper CLI in the form:
         *
         * <pre>{@code
         * echo -n <server Uri> | docker-credential-<credential helper suffix> get
         * }</pre>
         *
         * @return the Docker credentials by calling the corresponding CLI
         * @throws IOException if writing/reading process input/output fails
         * @throws CredentialHelperUnhandledServerUrlException if no credentials could be found for the
         *     corresponding server
         * @throws CredentialHelperNotFoundException if the credential helper CLI doesn't exist
         */
        public Credential Retrieve()
        {
            try
            {
                IProcess process = new ProcessBuilder(credentialHelper, "get").Start();
                using (Stream processStdin = process.GetOutputStream())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(registry);
                    processStdin.Write(bytes, 0, bytes.Length);
                }

                using (StreamReader processStdoutReader =
                    new StreamReader(process.GetInputStream(), Encoding.UTF8))
                {
                    string output = processStdoutReader.ReadToEnd();

                    // Throws an exception if the credential store does not have credentials for serverUrl.
                    if (output.Contains("credentials not found in native keychain"))
                    {
                        throw new CredentialHelperUnhandledServerUrlException(
                            credentialHelper, registry, output);
                    }
                    if (string.IsNullOrEmpty(output))
                    {
                        ThrowUnhandledUrlException(process);
                    }

                    try
                    {
                        DockerCredentialsTemplate dockerCredentials =
                            JsonTemplateMapper.ReadJson<DockerCredentialsTemplate>(output);
                        if (string.IsNullOrEmpty(dockerCredentials.Username)
                            || string.IsNullOrEmpty(dockerCredentials.Secret))
                        {
                            throw new CredentialHelperUnhandledServerUrlException(
                                credentialHelper, registry, output);
                        }

                        return Credential.From(dockerCredentials.Username, dockerCredentials.Secret);
                    }
                    catch (JsonException ex)
                    {
                        throw new CredentialHelperUnhandledServerUrlException(
                            credentialHelper, registry, output, ex);
                    }
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == Win32ErrorCodes.FileNotFound)
            {
                if (ex.Message == null)
                {
                    throw;
                }

                // Checks if the failure is due to a nonexistent credential helper CLI.
                if (ex.Message.Contains("No such file or directory")
                    || ex.Message.Contains("cannot find the file"))
                {
                    throw new CredentialHelperNotFoundException(credentialHelper, ex);
                }

                throw;
            }
        }

        private void ThrowUnhandledUrlException(IProcess process)
        {
            using (StreamReader processStderrReader =
                new StreamReader(process.GetErrorStream(), Encoding.UTF8))
            {
                string errorOutput = processStderrReader.ReadToEnd();
                throw new CredentialHelperUnhandledServerUrlException(
                    credentialHelper, registry, errorOutput);
            }
        }

        public SystemPath GetCredentialHelper()
        {
            return credentialHelper;
        }
    }
}
