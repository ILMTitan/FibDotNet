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
