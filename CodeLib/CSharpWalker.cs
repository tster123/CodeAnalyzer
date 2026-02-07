using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeLib;

public class CSharpWalker
{
    public void Walk(SyntaxTree tree)
    {
        SyntaxNode node = tree.GetRoot();
        Dictionary<SyntaxNode, List<SyntaxTrivia>> comments = BuildComments(tree);
        string text = node.ToFullString();
        uint[] lineNums = new uint[text.Length + 1];
        uint currentLine = 1;
        for (int i = 0; i < text.Length + 1; i++)
        {
            if (i < text.Length && text[i] == '\n') currentLine++;
            lineNums[i] = currentLine;
        }
        Print(node, "", lineNums, comments);
    }

    private Dictionary<SyntaxNode, List<SyntaxTrivia>> BuildComments(SyntaxTree tree)
    {
        Dictionary<SyntaxNode, List<SyntaxTrivia>> ret = new();
        SyntaxNode root = tree.GetRoot();
        foreach (SyntaxTrivia t in root.DescendantTrivia())
        {
            if (!CommentKinds.Contains(t.Kind())) continue;
            SyntaxNode n = t.Token.Parent ?? root;
            if (!ret.TryGetValue(n, out var list))
            {
                list = new();
                ret[n] = list;
            }
            list.Add(t);
        }

        return ret;
    }

    private static readonly HashSet<SyntaxKind> CommentKinds =
    [
        SyntaxKind.SingleLineCommentTrivia,
        SyntaxKind.MultiLineCommentTrivia,
        SyntaxKind.DocumentationCommentExteriorTrivia,
        SyntaxKind.SingleLineDocumentationCommentTrivia,
        SyntaxKind.MultiLineDocumentationCommentTrivia
    ];

    public void Print(SyntaxNode node, string indent, uint[] lineNums, Dictionary<SyntaxNode, List<SyntaxTrivia>> comments)
    {
        string str = node.ToString().Replace("\n", "\\n").Replace("\r", "");
        string pr = str.Length > 35 ? str.Substring(0, 30) : str;
        string lineString = "lines " + lineNums[node.Span.Start] + "-" + lineNums[node.Span.End];
        string nodePrint = $"{indent}{node.GetType().Name}:{str.Length}:{lineString}:{pr}";
        if (node is BinaryExpressionSyntax ||
            node is AssignmentExpressionSyntax)
        {
            nodePrint += $" ({node.Kind()})";
        }
        
        if (node is DocumentationCommentTriviaSyntax)
        {
            Console.WriteLine("XML" + node?.Parent?.GetType().Name + "-" + nodePrint);
            return; // don't descend into XML comments
        }

        Console.WriteLine(nodePrint);
        if (comments.TryGetValue(node, out var nodeComments))
        {
            foreach (SyntaxTrivia t in nodeComments)
            {
                Console.WriteLine(indent + ":" + t);
            }
        }
        foreach (var child in node.ChildNodes())
        {
            Print(child, indent + "  ", lineNums, comments);
        }
    }
}