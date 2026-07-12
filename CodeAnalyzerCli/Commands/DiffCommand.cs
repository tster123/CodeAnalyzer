using System.ComponentModel;
using Spectre.Console.Cli;

namespace CodeAnalyzerCli.Commands;

public class DiffCommand : Command<DiffOptions>
{
    protected override int Execute(CommandContext context, DiffOptions settings, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public class DiffOptions : CommandSettings
{
    [CommandArgument(0, "<filename>")] 
    [Description("Left (usually old) file")]
    public required string LeftFile { get; set; }
    
    [CommandArgument(1, "<filename>")] 
    [Description("Right (usually new) file")]
    public required string RightFile { get; set; }
}
