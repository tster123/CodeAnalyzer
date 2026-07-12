using System.ComponentModel;
using CodeLib;
using Spectre.Console.Cli;
using Wrapped.System.IO;

namespace CodeAnalyzerCli.Commands;

public class MetricsCommand: Command<MetricsOptions>
{
    protected override int Execute(CommandContext context, MetricsOptions settings, CancellationToken cancellationToken)
    {
        CodeStreamer s = new(settings.PrintMetrics, settings.PrintAst);
        s.ProcessFolder(new DirectoryInfoWrap(settings.Directory));
        Console.WriteLine("Errors: " + s.Errors);
        return 0;
    }
}

public class MetricsOptions : CommandSettings
{
    [CommandArgument(0, "<directory>")] 
    [Description("Code directory to run metrics on")]
    public required string Directory { get; set; }
    
    [CommandOption("-m|--metrics")]
    [Description("Generate metrics for the code in this folder")]
    public bool PrintMetrics { get; init; }
    
    [CommandOption("-a|--ast")]
    [Description("Print the abstract syntax tree (AST) of the code")]
    public bool PrintAst { get; init; }
}
