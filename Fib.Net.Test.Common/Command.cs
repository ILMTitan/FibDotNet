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

using Fib.Net.Core.Docker;
using System;
using System.Collections.Generic;
using System.IO;

namespace Fib.Net.Test.Common
{
    /** Test utility to run shell commands for integration tests. */
    public class Command
    {
        private readonly string command;
        private readonly string args;

        public Command(string command, params string[] args) : this(command, (IEnumerable<string>)args)
        {
        }

        public Command(string command, IEnumerable<string> args)
        {
            this.command = command;
            this.args = string.Join(" ", args);
        }

        public static string Run(string command, params string[] args)
        {
            return new Command(command, args).Run();
        }

        /** Runs the command. */
        public string Run()
        {
            return Run((byte[]) null);
        }

        /** Runs the command and pipes in {@code stdin}. */
        public string Run(byte[] stdin)
        {
            IProcess process = new ProcessBuilder(command, args).Start();

            if (stdin != null)
            {
                // Write out stdin.
                using (Stream outputStream = process.GetOutputStream())
                {
                    outputStream.Write(stdin, 0, stdin.Length);
                }
            }

            // Read in stdout.
            using (StreamReader inputStreamReader =
                new StreamReader(process.GetInputStream()))
            {
                string output = inputStreamReader.ReadToEnd();

                if (process.WaitFor() != 0)
                {
                    string stderr;
                    using (StreamReader errorStreamReader = new StreamReader(process.GetErrorStream()))
                    {
                        stderr = errorStreamReader.ReadToEnd();
                    }
                    throw new Exception("Command '" + command + " " + string.Join(" ", args) + "' failed: " + stderr);
                }

                return output;
            }
        }
    }
}
