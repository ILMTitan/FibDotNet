/*
 * Copyright 2017 Google LLC.
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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry.credentials;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Jib.Net.Core.Registry.Credentials
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

        public DockerCredentialHelper(string registry, string credentialHelperSuffix) : this(registry, Paths.get(CredentialHelperPrefix + credentialHelperSuffix))
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
        public Credential retrieve()
        {
            try
            {
                IProcess process = new ProcessBuilder(credentialHelper.toString(), "get").start();
                using (Stream processStdin = process.getOutputStream())
                {
                    processStdin.write(registry.getBytes(Encoding.UTF8));
                }

                using (StreamReader processStdoutReader =
                    new StreamReader(process.getInputStream(), Encoding.UTF8))
                {
                    string output = CharStreams.toString(processStdoutReader);

                    // Throws an exception if the credential store does not have credentials for serverUrl.
                    if (output.contains("credentials not found in native keychain"))
                    {
                        throw new CredentialHelperUnhandledServerUrlException(
                            credentialHelper, registry, output);
                    }
                    if (output.isEmpty())
                    {
                        ThrowUnhandledUrlException(process);
                    }

                    try
                    {
                        DockerCredentialsTemplate dockerCredentials =
                            JsonTemplateMapper.readJson<DockerCredentialsTemplate>(output);
                        if (Strings.isNullOrEmpty(dockerCredentials.Username)
                            || Strings.isNullOrEmpty(dockerCredentials.Secret))
                        {
                            throw new CredentialHelperUnhandledServerUrlException(
                                credentialHelper, registry, output);
                        }

                        return Credential.from(dockerCredentials.Username, dockerCredentials.Secret);
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
                if (ex.getMessage() == null)
                {
                    throw;
                }

                // Checks if the failure is due to a nonexistent credential helper CLI.
                if (ex.getMessage().contains("No such file or directory")
                    || ex.getMessage().contains("cannot find the file"))
                {
                    throw new CredentialHelperNotFoundException(credentialHelper, ex);
                }

                throw;
            }
        }

        private void ThrowUnhandledUrlException(IProcess process)
        {
            using (StreamReader processStderrReader =
                new StreamReader(process.getErrorStream(), Encoding.UTF8))
            {
                string errorOutput = processStderrReader.ReadToEnd();
                throw new CredentialHelperUnhandledServerUrlException(
                    credentialHelper, registry, errorOutput);
            }
        }

        public SystemPath getCredentialHelper()
        {
            return credentialHelper;
        }
    }
}
