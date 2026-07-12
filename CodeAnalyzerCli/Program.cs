using CodeAnalyzerCli.Commands;
using Spectre.Console.Cli;

namespace CodeAnalyzerCli;

public class Program
{
    static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<DiffCommand>("diff");
            config.AddCommand<MetricsCommand>("metrics");
        });
        return app.Run(args);
    }
}