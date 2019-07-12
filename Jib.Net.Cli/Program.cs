using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Jib.Net.Cli
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            TarCommandHandler tarCommandHandler = new TarCommandHandler();
            DaemonCommandHandler daemonCommandHandler = new DaemonCommandHandler();
            PushCommandHandler pushCommandHandler = new PushCommandHandler();
            RootCommand rootCommand = CreateCommand(tarCommandHandler, daemonCommandHandler, pushCommandHandler);
            return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        private static RootCommand CreateCommand(
            TarCommandHandler tarCommandHandler,
            DaemonCommandHandler daemonCommandHandler,
            PushCommandHandler pushCommandHandler)
        {
            return new RootCommand("Build a container image with Jib.NET")
            {
                new Option("--tool-name", "The name of the tool.")
                {
                    Argument = new Argument<string>(() => "jib.net.cli")
                    {
                        Arity = ArgumentArity.ExactlyOne
                    }
                },
                CreateTarCommand(tarCommandHandler),
                CreateDaemonCommand(daemonCommandHandler),
                CreatePushCommand(pushCommandHandler)
            };
        }

        private static Command CreatePushCommand(PushCommandHandler pushCommandHandler)
        {
            Command pushConfigFileCommand = CreateConfigFileCommand();
            Func<string, FileInfo, Task<int>> handlerFunc = pushCommandHandler.FromConfigFileAsync;
            pushConfigFileCommand.Handler = CommandHandler.Create(handlerFunc);
            return new Command("push", "Build an image and push to a remote registry.")
            {
                pushConfigFileCommand
            };
        }

        private static Command CreateDaemonCommand(DaemonCommandHandler daemonCommandHandler)
        {
            Command daemonConfigFileCommand = CreateConfigFileCommand();
            Func<string, FileInfo, Task<int>> handlerFunc = daemonCommandHandler.FromConfigFileAsync;
            daemonConfigFileCommand.Handler = CommandHandler.Create(handlerFunc);
            return new Command("daemon", "Build an image and push to the local docker daemon.")
            {
                daemonConfigFileCommand
            };
        }

        private static Command CreateTarCommand(TarCommandHandler tarCommandHandler)
        {
            Command tarConfigFileCommand = CreateConfigFileCommand();
            Func<string, FileInfo, FileInfo, Task<int>> handlerFunc = tarCommandHandler.BuildFromConfigFileAsync;
            tarConfigFileCommand.Handler = CommandHandler.Create(handlerFunc);
            return new Command("tar", "Build an image to a compressed tar file.")
            {
                new Argument<FileInfo>("--output-file")
                {
                    Description = "The file to write the resulting tar file to.",
                    Arity = ArgumentArity.ExactlyOne,
                },
                tarConfigFileCommand,
            };
        }

        private static Command CreateConfigFileCommand()
        {
            return new Command("--config-file", "Take the configuration as json in a file")
            {
                new Argument<FileInfo>("--config-file")
                {
                    Description = "The json file of the configuration.",
                    Arity = ArgumentArity.ExactlyOne,
                }.ExistingOnly(),
            };
        }
    }
}
