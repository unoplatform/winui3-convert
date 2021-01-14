using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Uno.WinUI3Convert
{
    class Program
    {
        static Task<int> Main(string[] args)
        {
            var command = new RootCommand("Migrate UWP projects to WinUI 3")
            {
                new Argument<DirectoryInfo>("source", "Source directory")
                {
                    Arity = ArgumentArity.ExactlyOne
                }
                .ExistingOnly(),

                new Argument<DirectoryInfo>("destination", "Destination directory")
                {
                    Arity = ArgumentArity.ExactlyOne
                },

                new Option<bool>("--overwrite", "Overwrite destination"),
            };

            command.Handler = CommandHandler.Create<DirectoryInfo, DirectoryInfo, bool, IHost>(ConvertCommand.Execute);

            return new CommandLineBuilder(command)
                .UseHost(_ => Host.CreateDefaultBuilder())
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }
    }
}
