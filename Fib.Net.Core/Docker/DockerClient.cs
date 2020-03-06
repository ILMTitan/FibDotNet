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
using Fib.Net.Core.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fib.Net.Core.Docker
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
            public Builder SetDockerExecutable(SystemPath dockerExecutable)
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
            public Builder SetDockerEnvironment(ImmutableDictionary<string, string> dockerEnvironment)
            {
                this.dockerEnvironment = dockerEnvironment;
                return this;
            }

            public DockerClient Build()
            {
                return new DockerClient(dockerExecutable, dockerEnvironment);
            }
        }

        /**
         * Gets a new {@link Builder} for {@link DockerClient} with defaults.
         *
         * @return a new {@link Builder}
         */
        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        /**
         * Instantiates with the default {@code docker} executable.
         *
         * @return a new {@link DockerClient}
         */
        public static DockerClient NewDefaultClient()
        {
            return CreateBuilder().Build();
        }

        /**
         * Gets a function that takes a {@code docker} subcommand and gives back a {@link ProcessBuilder}
         * for that {@code docker} command.
         *
         * @param dockerExecutable path to {@code docker}
         * @return the default {@link ProcessBuilder} factory for running a {@code docker} subcommand
         */

        public static Func<IList<string>, ProcessBuilder> DefaultProcessBuilderFactory(
            string dockerExecutable, ImmutableDictionary<string, string> dockerEnvironment)
        {
            return dockerSubCommand =>
            {
                List<string> dockerCommand = new List<string>(1 + dockerSubCommand.Count);
                dockerCommand.Add(dockerExecutable);
                dockerCommand.AddRange(dockerSubCommand);

                return new ProcessBuilder(dockerExecutable, string.Join(" ", dockerSubCommand), dockerEnvironment);
            };
        }

        private static readonly SystemPath DEFAULT_DOCKER_CLIENT = Paths.Get("docker");

        /** Factory for generating the {@link ProcessBuilder} for running {@code docker} commands. */
        private readonly Func<List<string>, IProcessBuilder> processBuilderFactory;

        public DockerClient(Func<IList<string>, IProcessBuilder> processBuilderFactory)
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
          this(DefaultProcessBuilderFactory(dockerExecutable.ToString(), dockerEnvironment))
        {
        }

        /**
         * Checks if Docker is installed on the user's system and accessible by running the default {@code
         * docker} command.
         *
         * @return {@code true} if Docker is installed on the user's system and accessible
         */
        public static bool IsDefaultDockerInstalled()
        {
            return IsDockerInstalled(DEFAULT_DOCKER_CLIENT);
        }

        /**
         * Checks if Docker is installed on the user's system and accessible by running the given {@code
         * docker} executable.
         *
         * @param dockerExecutable path to the executable to test running
         * @return {@code true} if Docker is installed on the user's system and accessible
         */
        public static bool IsDockerInstalled(SystemPath dockerExecutable)
        {
            dockerExecutable = dockerExecutable ?? throw new ArgumentNullException(nameof(dockerExecutable));
            try
            {
                new ProcessBuilder(dockerExecutable.ToString()).Start();
                return true;
            }
            catch (Win32Exception e) when (e.NativeErrorCode == Win32ErrorCodes.FileNotFound)
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
        public async Task<string> LoadAsync(IImageTarball imageTarball)
        {
            imageTarball = imageTarball ?? throw new ArgumentNullException(nameof(imageTarball));
            // Runs 'docker load'.
            IProcess dockerProcess = Docker("load");

            using (Stream stdin = dockerProcess.GetOutputStream())
            {
                try
                {
                    await imageTarball.WriteToAsync(stdin).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    // Tries to read from stderr.
                    string error;
                    try
                    {
                        using (TextReader stderr = dockerProcess.GetErrorReader())
                        {
                            error = await stderr.ReadToEndAsync().ConfigureAwait(false);
                        }
                    }
                    catch (IOException e)
                    {
                        // This ignores exceptions from reading stderr and throws the original exception from
                        // writing to stdin.
                        Debug.WriteLine(e);
                        error = null;
                    }
                    if (error is null)
                    {
                        throw;
                    }
                    throw new IOException("'docker load' command failed with error: " + error, ex);
                }
            }

            using (Stream stdoutStream = dockerProcess.GetInputStream())
            using (StreamReader stdout = new StreamReader(stdoutStream, Encoding.UTF8))
            {
                string output = await stdout.ReadToEndAsync().ConfigureAwait(false);

                if (dockerProcess.WaitFor() != 0)
                {
                    string errMessage = await GetErrorMessageAsync(dockerProcess).ConfigureAwait(false);
                    throw new IOException("'docker load' command failed with output: " + errMessage);
                }

                return output;
            }
        }

        private static async Task<string> GetErrorMessageAsync(IProcess dockerProcess)
        {
            using (Stream stderrStream = dockerProcess.GetErrorStream())
            using (StreamReader stderr = new StreamReader(stderrStream))
            {
                return await stderr.ReadToEndAsync().ConfigureAwait(false);
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
        public async Task TagAsync(IImageReference originalImageReference, ImageReference newImageReference)
        {
            originalImageReference = originalImageReference ?? throw new ArgumentNullException(nameof(originalImageReference));

            newImageReference = newImageReference ?? throw new ArgumentNullException(nameof(newImageReference));
            // Runs 'docker tag'.
            IProcess dockerProcess =
                Docker("tag", originalImageReference.ToString(), newImageReference.ToString());

            if (dockerProcess.WaitFor() != 0)
            {
                using (StreamReader stderr =
                    new StreamReader(dockerProcess.GetErrorStream(), Encoding.UTF8))
                {
                    string errorMessage = await stderr.ReadToEndAsync().ConfigureAwait(false);
                    throw new IOException(
                        "'docker tag' command failed with error: " + errorMessage);
                }
            }
        }

        /** Runs a {@code docker} command. */
        private IProcess Docker(params string[] subCommand)
        {
            return processBuilderFactory(subCommand.ToList()).Start();
        }
    }
}
