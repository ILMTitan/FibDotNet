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
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.IO;

namespace com.google.cloud.tools.jib
{
    /** Test utility to run shell commands for integration tests. */
    public class Command
    {
        private readonly IList<string> command;

        /** Instantiate with a command. */
        public Command(params string[] command)
        {
            this.command = Arrays.asList(command);
        }

        /** Instantiate with a command. */
        public Command(IList<string> command)
        {
            this.command = command;
        }

        /** Runs the command. */
        public string run()
        {
            return run(null);
        }

        /** Runs the command and pipes in {@code stdin}. */
        public string run(byte[] stdin)
        {
            Process process = new ProcessBuilder(command).start();

            if (stdin != null)
            {
                // Write out stdin.
                using (Stream outputStream = process.getOutputStream())
                {
                    outputStream.write(stdin);
                }
            }

            // Read in stdout.
            using (StreamReader inputStreamReader =
                new StreamReader(process.getInputStream(), StandardCharsets.UTF_8))
            {
                string output = CharStreams.toString(inputStreamReader);

                if (process.waitFor() != 0)
                {
                    string stderr =
                        CharStreams.toString(
                            new StreamReader(process.getErrorStream(), StandardCharsets.UTF_8));
                    throw new Exception("Command '" + string.Join(" ", command) + "' failed: " + stderr);
                }

                return output;
            }
        }
    }
}
