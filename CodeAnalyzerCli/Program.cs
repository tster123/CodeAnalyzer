using CodeAnalyzerCli.Commands;
using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace CodeAnalyzerCli;

[UsedImplicitly]
public class Program
{
    static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<DiffCommand>("diff");
            config.AddCommand<MetricsCommand>("metrics");
            config.UseStrictParsing();
        });
        return app.Run(args);
    }
}