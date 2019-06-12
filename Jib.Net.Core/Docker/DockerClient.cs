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
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace com.google.cloud.tools.jib.docker
{






    /** Calls out to the {@code docker} CLI. */
    public class DockerClient
    {
        /** Builds a {@link DockerClient}. */
        public class Builder
        {
            private SystemPath dockerExecutable = DEFAULT_DOCKER_CLIENT;
            private ImmutableDictionary<string, string> dockerEnvironment = ImmutableDictionary.Create<string, string>();

            public Builder() { }

            /**
             * Sets a path for a {@code docker} executable.
             *
             * @param dockerExecutable path to {@code docker}
             * @return this
             */
            public Builder setDockerExecutable(SystemPath dockerExecutable)
            {
                this.dockerExecutable = dockerExecutable;
                return this;
            }

            /**
             * Sets environment variables to use when executing the {@code docker} executable.
             *
             * @param dockerEnvironment environment variables for {@code docker}
             * @return this
             */
            public Builder setDockerEnvironment(ImmutableDictionary<string, string> dockerEnvironment)
            {
                this.dockerEnvironment = dockerEnvironment;
                return this;
            }

            public DockerClient build()
            {
                return new DockerClient(dockerExecutable, dockerEnvironment);
            }
        }

        /**
         * Gets a new {@link Builder} for {@link DockerClient} with defaults.
         *
         * @return a new {@link Builder}
         */
        public static Builder builder()
        {
            return new Builder();
        }

        /**
         * Instantiates with the default {@code docker} executable.
         *
         * @return a new {@link DockerClient}
         */
        public static DockerClient newDefaultClient()
        {
            return builder().build();
        }

        /**
         * Gets a function that takes a {@code docker} subcommand and gives back a {@link ProcessBuilder}
         * for that {@code docker} command.
         *
         * @param dockerExecutable path to {@code docker}
         * @return the default {@link ProcessBuilder} factory for running a {@code docker} subcommand
         */

        public static Func<IList<string>, ProcessBuilder> defaultProcessBuilderFactory(
            string dockerExecutable, ImmutableDictionary<string, string> dockerEnvironment)
        {
            return dockerSubCommand =>
            {
                IList<string> dockerCommand = new List<string>(1 + dockerSubCommand.size());
                dockerCommand.add(dockerExecutable);
                dockerCommand.addAll(dockerSubCommand);

                ProcessBuilder processBuilder = new ProcessBuilder(dockerCommand);
                IDictionary<string, string> environment = processBuilder.environment();
                environment.putAll(dockerEnvironment);

                return processBuilder;
            };
        }

        private static readonly SystemPath DEFAULT_DOCKER_CLIENT = Paths.get("docker");

        /** Factory for generating the {@link ProcessBuilder} for running {@code docker} commands. */
        private readonly Func<List<string>, ProcessBuilder> processBuilderFactory;

        public DockerClient(Func<IList<string>, ProcessBuilder> processBuilderFactory)
        {
            this.processBuilderFactory = processBuilderFactory;
        }

        /**
         * Instantiates with a {@code docker} executable and environment variables.
         *
         * @param dockerExecutable path to {@code docker}
         * @param dockerEnvironment environment variables for {@code docker}
         */
        private DockerClient(SystemPath dockerExecutable, ImmutableDictionary<string, string> dockerEnvironment) :
          this(defaultProcessBuilderFactory(dockerExecutable.toString(), dockerEnvironment))
        {
        }

        /**
         * Checks if Docker is installed on the user's system and accessible by running the default {@code
         * docker} command.
         *
         * @return {@code true} if Docker is installed on the user's system and accessible
         */
        public static bool isDefaultDockerInstalled()
        {
            return isDockerInstalled(DEFAULT_DOCKER_CLIENT);
        }

        /**
         * Checks if Docker is installed on the user's system and accessible by running the given {@code
         * docker} executable.
         *
         * @param dockerExecutable path to the executable to test running
         * @return {@code true} if Docker is installed on the user's system and accessible
         */
        public static bool isDockerInstalled(SystemPath dockerExecutable)
        {
            try
            {
                new ProcessBuilder(dockerExecutable.toString()).start();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /**
         * Loads an image tarball into the Docker daemon.
         *
         * @see <a
         *     href="https://docs.docker.com/engine/reference/commandline/load/">https://docs.docker.com/engine/reference/commandline/load</a>
         * @param imageTarball the built container tarball.
         * @return stdout from {@code docker}.
         * @throws InterruptedException if the 'docker load' process is interrupted.
         * @throws IOException if streaming the blob to 'docker load' fails.
         */
        public string load(ImageTarball imageTarball)
        {
            // Runs 'docker load'.
            Process dockerProcess = docker("load");

            using (Stream stdin = dockerProcess.getOutputStream())
            {
                try
                {
                    imageTarball.writeTo(stdin);
                }
                catch (IOException ex)
                {
                    // Tries to read from stderr.
                    string error;
                    try
                    {
                        using (StreamReader stderr =
                            new StreamReader(dockerProcess.getErrorStream(), StandardCharsets.UTF_8))
                        {
                            error = CharStreams.toString(stderr);
                        }
                    }
                    catch (IOException)
                    {
                        // This ignores exceptions from reading stderr and throws the original exception from
                        // writing to stdin.
                        throw ex;
                    }
                    throw new IOException("'docker load' command failed with error: " + error, ex);
                }
            }

            using (StreamReader stdout =
                new StreamReader(dockerProcess.getInputStream(), StandardCharsets.UTF_8))
            {
                string output = CharStreams.toString(stdout);

                if (dockerProcess.waitFor() != 0)
                {
                    using (StreamReader stderr =
                        new StreamReader(dockerProcess.getErrorStream(), StandardCharsets.UTF_8))
                    {
                        throw new IOException(
                            "'docker load' command failed with output: " + CharStreams.toString(stderr));
                    }
                }

                return output;
            }
        }

        /**
         * Tags the image referenced by {@code originalImageReference} with a new image reference {@code
         * newImageReference}.
         *
         * @param originalImageReference the existing image reference on the Docker daemon
         * @param newImageReference the new image reference
         * @see <a
         *     href="https://docs.docker.com/engine/reference/commandline/tag/">https://docs.docker.com/engine/reference/commandline/tag/</a>
         * @throws InterruptedException if the 'docker tag' process is interrupted.
         * @throws IOException if an I/O exception occurs or {@code docker tag} failed
         */
        public void tag(ImageReference originalImageReference, ImageReference newImageReference)
        {
            // Runs 'docker tag'.
            Process dockerProcess =
                docker("tag", originalImageReference.toString(), newImageReference.toString());

            if (dockerProcess.waitFor() != 0)
            {
                using (StreamReader stderr =
                    new StreamReader(dockerProcess.getErrorStream(), StandardCharsets.UTF_8))
                {
                    throw new IOException(
                        "'docker tag' command failed with error: " + CharStreams.toString(stderr));
                }
            }
        }

        /** Runs a {@code docker} command. */
        private Process docker(params string[] subCommand)
        {
            return processBuilderFactory.apply(Arrays.asList(subCommand)).start();
        }
    }
}
