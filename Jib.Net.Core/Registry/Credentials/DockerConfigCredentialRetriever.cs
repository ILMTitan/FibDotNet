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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry.credentials.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System;
using System.IO;

namespace com.google.cloud.tools.jib.registry.credentials
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
            Paths.get(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docker", "config.json");

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
        public Optional<Credential> retrieve(Action<LogEvent> logger)
        {
            if (!Files.exists(dockerConfigFile))
            {
                return Optional.empty<Credential>();
            }
            DockerConfig dockerConfig =
                new DockerConfig(
                    JsonTemplateMapper.readJsonFromFile<DockerConfigTemplate>(dockerConfigFile));
            return retrieve(dockerConfig, logger);
        }

        /**
         * Retrieves credentials for a registry alias from a {@link DockerConfig}.
         *
         * @param dockerConfig the {@link DockerConfig} to retrieve from
         * @param logger a consumer for handling log events
         * @return the retrieved credentials, or {@code Optional#empty} if none are found
         */

        public Optional<Credential> retrieve(IDockerConfig dockerConfig, Action<LogEvent> logger)
        {

            logger = logger ?? throw new ArgumentNullException(nameof(logger));
            foreach (string registryAlias in RegistryAliasGroup.getAliasesGroup(registry))
            {
                // First, tries to find defined auth.
                string auth = dockerConfig.getAuthFor(registryAlias);
                if (auth != null)
                {
                    // 'auth' is a basic authentication token that should be parsed back into credentials
                    string usernameColonPassword = StandardCharsets.UTF_8.GetString(Convert.FromBase64String(auth));
                    string username = usernameColonPassword.substring(0, usernameColonPassword.indexOf(":"));
                    string password = usernameColonPassword.substring(usernameColonPassword.indexOf(":") + 1);
                    return Optional.of(Credential.from(username, password));
                }

                // Then, tries to use a defined credHelpers credential helper.
                IDockerCredentialHelper dockerCredentialHelper =
                    dockerConfig.getCredentialHelperFor(registryAlias);
                if (dockerCredentialHelper != null)
                {
                    try
                    {
                        // Tries with the given registry alias (may be the original registry).
                        return Optional.of(dockerCredentialHelper.retrieve());
                    }
                    catch (Exception ex) when (ex is IOException || ex is CredentialHelperUnhandledServerUrlException || ex is CredentialHelperNotFoundException)
                    {
                        // Warns the user that the specified credential helper cannot be used.
                        if (ex.getMessage() != null)
                        {
                            logger(LogEvent.warn(ex.getMessage()));
                            if (ex.getCause()?.getMessage() != null)
                            {
                                logger(LogEvent.warn("  Caused by: " + ex.getCause().getMessage()));
                            }
                        }
                    }
                }
            }
            return Optional.empty<Credential>();
        }
    }
}
