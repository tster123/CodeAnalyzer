using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Spectre.Console.Cli;

public class DiffCommand : Command<DiffOptions>
{
}

public class DiffOptions : CommandSettings
{
    [CommandArgument(0, "<filename>")] 
    [Description("The name to greet")]
    private string LeftFile;
}
