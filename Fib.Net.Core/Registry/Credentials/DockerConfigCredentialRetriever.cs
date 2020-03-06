// Copyright 2018 Google LLC.
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
using Fib.Net.Core.Events;
using Fib.Net.Core.FileSystem;
using Fib.Net.Core.Json;
using Fib.Net.Core.Registry.Credentials.Json;
using System;
using System.IO;
using System.Text;

namespace Fib.Net.Core.Registry.Credentials
{
    /**
     * Retrieves registry credentials from the Docker config.
     *
     * <p>The credentials are searched in the following order (stopping when credentials are found):
     *
     * <ol>
     *   <li>If there is an {@code auth} defined for a registry.
     *   <li>Using the {@code credsStore} credential helper, if available.
     *   <li>Using the credential helper from {@code credHelpers}, if available.
     * </ol>
     *
     * @see <a
     *     href="https://docs.docker.com/engine/reference/commandline/login/">https://docs.docker.com/engine/reference/commandline/login/</a>
     */
    public class DockerConfigCredentialRetriever : IDockerConfigCredentialRetriever
    {
        /**
         * @see <a
         *     href="https://docs.docker.com/engine/reference/commandline/login/#privileged-user-requirement">https://docs.docker.com/engine/reference/commandline/login/#privileged-user-requirement</a>
         */
        private static readonly SystemPath DOCKER_CONFIG_FILE =
            Paths.Get(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docker", "config.json");

        private readonly string registry;
        private readonly SystemPath dockerConfigFile;

        public DockerConfigCredentialRetriever(string registry) : this(registry, DOCKER_CONFIG_FILE)
        {
        }

        public DockerConfigCredentialRetriever(string registry, SystemPath dockerConfigFile)
        {
            this.registry = registry;
            this.dockerConfigFile = dockerConfigFile;
        }

        /**
         * Retrieves credentials for a registry. Tries all possible known aliases.
         *
         * @param logger a consumer for handling log events
         * @return {@link Credential} found for {@code registry}, or {@link Optional#empty} if not found
         * @throws IOException if failed to parse the config JSON
         */
        public Maybe<Credential> Retrieve(Action<LogEvent> logger)
        {
            if (!Files.Exists(dockerConfigFile))
            {
                return Maybe.Empty<Credential>();
            }
            DockerConfig dockerConfig =
                new DockerConfig(
                    JsonTemplateMapper.ReadJsonFromFile<DockerConfigTemplate>(dockerConfigFile));
            return Retrieve(dockerConfig, logger);
        }

        /**
         * Retrieves credentials for a registry alias from a {@link DockerConfig}.
         *
         * @param dockerConfig the {@link DockerConfig} to retrieve from
         * @param logger a consumer for handling log events
         * @return the retrieved credentials, or {@code Optional#empty} if none are found
         */

        public Maybe<Credential> Retrieve(IDockerConfig dockerConfig, Action<LogEvent> logger)
        {
            logger = logger ?? throw new ArgumentNullException(nameof(logger));
            foreach (string registryAlias in RegistryAliasGroup.GetAliasesGroup(registry))
            {
                // First, tries to find defined auth.
                string auth = dockerConfig.GetAuthFor(registryAlias);
                if (auth != null)
                {
                    // 'auth' is a basic authentication token that should be parsed back into credentials
                    string usernameColonPassword = Encoding.UTF8.GetString(Convert.FromBase64String(auth));
                    int colonIndex = usernameColonPassword.IndexOf(":", StringComparison.Ordinal);
                    string username = usernameColonPassword.Substring(0, colonIndex);

                    string password = usernameColonPassword.Substring(colonIndex + 1);
                    return Maybe.Of(Credential.From(username, password));
                }

                // Then, tries to use a defined credHelpers credential helper.
                IDockerCredentialHelper dockerCredentialHelper =
                    dockerConfig.GetCredentialHelperFor(registryAlias);
                if (dockerCredentialHelper != null)
                {
                    try
                    {
                        // Tries with the given registry alias (may be the original registry).
                        return Maybe.Of(dockerCredentialHelper.Retrieve());
                    }
                    catch (Exception ex) when (ex is IOException || ex is CredentialHelperUnhandledServerUrlException || ex is CredentialHelperNotFoundException)
                    {
                        // Warns the user that the specified credential helper cannot be used.
                        if (ex.Message != null)
                        {
                            logger(LogEvent.Warn(ex.Message));
                            if (ex.InnerException?.Message != null)
                            {
                                logger(LogEvent.Warn("  Caused by: " + ex.InnerException.Message));
                            }
                        }
                    }
                }
            }
            return Maybe.Empty<Credential>();
        }
    }
}
