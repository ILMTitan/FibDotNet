// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using CommandLine;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fib.Net.Cli
{
    public class Program
    {
        internal TextWriter Output { get;  }
        internal TextWriter Error { get; }
        private Parser CommandParser { get; }

        public Program(TextWriter outputWriter, TextWriter errorWriter, Parser parser)
        {
            Output = outputWriter;
            Error = errorWriter;
            CommandParser = parser;
        }

        public static async Task<int> Main(string[] args)
        {
            var program = new Program(Console.Out, Console.Error, Parser.Default);
            return await program.ExecuteAsync(args).ConfigureAwait(false);
        }

        public async Task<int> ExecuteAsync(params string[] args)
        {
            var parsed = CommandParser.ParseArguments<TarCommand, DaemonCommand, PushCommand>(args);
            var result = parsed.MapResult(
                async (Command command) =>
                {
                    await command.ExecuteAsync(Output, Error).ConfigureAwait(false);
                    return 0;
                },
                errors =>
                {
                    if (errors.All(e =>
                        e.Tag == ErrorType.HelpVerbRequestedError ||
                        e.Tag == ErrorType.HelpRequestedError ||
                        e.Tag == ErrorType.VersionRequestedError))
                    {
                        return Task.FromResult(0);
                    }
                    else
                    {
                        return Task.FromResult(1);
                    }
                });

            return await result.ConfigureAwait(false);
        }
    }
}
