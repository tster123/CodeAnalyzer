using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using CodeLib.Diff;
using Spectre.Console.Cli;
using Wrapped.System.IO;

namespace CodeAnalyzerCli.Commands;

public class DiffCommand : Command<DiffOptions>
{
    protected override int Execute(CommandContext context, DiffOptions settings, CancellationToken cancellationToken)
    {
        TextFileByLines tokenizer = new();
        TokenizerSettings ts = new()
        {
            IgnoreEndingWhitespace = true,
            IgnoreStartingWhitespace = true
        };
        using var leftReader = new StreamReaderWrap(new FileStreamWrap(settings.LeftFile, FileMode.Open));
        List<string> leftLines = new();
        tokenizer.Accumulator = leftLines;
        List<ulong> leftTokens = tokenizer.Tokenize(ts, settings.LeftFile, leftReader);
        
        using var rightReader = new StreamReaderWrap(new FileStreamWrap(settings.RightFile, FileMode.Open));
        List<string> rightLines = new();
        tokenizer.Accumulator = rightLines;
        List<ulong> rightTokens = tokenizer.Tokenize(ts, settings.RightFile, rightReader);

        IDiffAlgorithm diff = new QuadraticMeyersDiff();
        List<DiffPart> parts = diff.Diff(CollectionsMarshal.AsSpan(leftTokens), CollectionsMarshal.AsSpan(rightTokens));
        
        Console.Out.WriteLine("---" + settings.LeftFile);
        Console.Out.WriteLine("+++" + settings.RightFile);
        new DiffPrinter(settings, Console.Out, parts, leftLines, rightLines).Print();
        return 0;
    }
}

internal class DiffPrinter
{
    private List<DiffPart> parts;
    private List<string> leftLines;
    private List<string> rightLines;
    private TextWriter output;
        
    private readonly DiffOptions options;

    private int leftPos, rightPos, partsPos;
    public DiffPrinter(DiffOptions options, TextWriter output, List<DiffPart> parts, List<string> leftLines, List<string> rightLines)
    {
        this.options = options;
        this.output = output;
        this.parts = parts;
        this.leftLines = leftLines;
        this.rightLines = rightLines;
    }

    public void Print()
    {
        int numSections = 0;
        while (partsPos < parts.Count)
        {
            if (!AdvanceToNextChange()) break;
            numSections++;

            PrintSection();
        }

        if (numSections == 0)
            Console.WriteLine("Files are the same.");
    }

    private void PrintSection()
    {
        StringBuilder sb = new();
        int leftBefore = leftPos, rightBefore = rightPos;
        BuildSectionText(sb);
        output.WriteLine($"@@ -{leftBefore + 1},{leftPos - leftBefore} +{rightBefore + 1},{rightPos - rightBefore} @@");
        output.Write(sb.ToString());
    }

    private void BuildSectionText(StringBuilder sb)
    {
        int numNoChangesSeen = -1; // -1 so that it won't trigger on the first ContextLines seen (which was left from the last move)
        while (partsPos < parts.Count)
        {
            switch (parts[partsPos].Operation)
            {
                case Operation.Keep:
                    sb.Append(" ");
                    sb.AppendLine(leftLines[leftPos]);
                    numNoChangesSeen++;
                    leftPos++;
                    rightPos++;
                    break;
                case Operation.Delete:
                    numNoChangesSeen = 0;
                    sb.Append("-");
                    sb.AppendLine(leftLines[leftPos]);
                    leftPos++;
                    break;
                case Operation.Insert:
                    numNoChangesSeen = 0;
                    sb.Append("+");
                    sb.AppendLine(rightLines[rightPos]);
                    rightPos++;
                    break;
                default:
                    throw new NotImplementedException("Unknown operation: " + parts[partsPos].Operation);
            }
            partsPos++;

            if (numNoChangesSeen == options.ContextLines)
            {
                bool keepGoing = false;
                for (int lookAhead = partsPos + 1; lookAhead < Math.Min(parts.Count, partsPos + options.ContextLines + 1); lookAhead++)
                {
                    if (parts[lookAhead].Operation != Operation.Keep)
                    {
                        // change is coming soon enough that it isn't worth breaking this diff chunk
                        keepGoing = true;
                        break;
                    }
                }

                if (!keepGoing) return;
            }
        }
    }

    private bool AdvanceToNextChange()
    {
        while (partsPos < parts.Count)
        {
            if (parts[partsPos].Operation == Operation.Keep)
            {
                partsPos++;
                leftPos++;
                rightPos++;
            }
            else
            {
                partsPos = Math.Max(0, partsPos - options.ContextLines);
                leftPos = Math.Max(0, leftPos - options.ContextLines);
                rightPos = Math.Max(0, rightPos - options.ContextLines);
                return true;
            }
        }

        return false;
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

    [CommandOption("-l|--context-lines")]
    [Description("Number of lines to include before and after a change to show context")]
    [DefaultValue(3)]
    public int ContextLines { get; set; } = 3;
}
