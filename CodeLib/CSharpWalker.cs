using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeLib;

public class Class
{
    public string Name { get; set; }
    public List<Method> Methods { get; } = new();
}

public class Method
{
    public string Name { get; set; }
    public int CyclomaticComplexity { get; set; }
    public int CommentLines { get; set; }
    public int CodeTokens { get; set; }
}

public class CSharpMetricsWalker
{
    private readonly Stack<Class> _classStack = new();
    private Class CurrentClass => _classStack.Peek();

    private readonly Stack<Method> _methodStack = new();
    private Method CurrentMethod => _methodStack.Peek();

    public List<Class> Classes = new();

    public void Walk(SyntaxNode node)
    {
        if (node is MethodDeclarationSyntax syntax)
        {
            Walk(syntax);
        }
        else if (node is ClassDeclarationSyntax methodSyntax)
        {
            Walk(methodSyntax);
        }
        else
        {
            WalkChildren(node);
        }
    }

    public void Walk(ClassDeclarationSyntax node)
    {
        Class m = new();
        m.Name = node.Identifier.ValueText;
        _classStack.Push(m);
        WalkChildren(node);
        Class m2 = _classStack.Pop();
        Debug.Assert(m == m2);
        Classes.Add(m);
    }

    public void Walk(MethodDeclarationSyntax node)
    {
        Method m = new();
        m.Name = node.Identifier.ValueText;
        _methodStack.Push(m);
        WalkChildren(node);
        Method m2 = _methodStack.Pop();
        Debug.Assert(m == m2);
        CurrentClass.Methods.Add(m);
    }

    private void WalkChildren(SyntaxNode node)
    {
        foreach (var c in node.ChildNodes())
        {
            Walk(c);
        }
    }
}

public class CSharpWalker
{
    

    public void Walk(SyntaxNode node)
    {
        string text = node.ToFullString();
        uint[] lineNums = new uint[text.Length + 1];
        uint currentLine = 1;
        for (int i = 0; i < text.Length + 1; i++)
        {
            if (i < text.Length && text[i] == '\n') currentLine++;
            lineNums[i] = currentLine;
        }
        Print(node, "", lineNums);

        CSharpMetricsWalker w = new();
        w.Walk(node);
    }

    

    public void Print(SyntaxNode node, string indent, uint[] lineNums)
    {
        string str = node.ToString().Replace("\n", "\\n").Replace("\r", "");
        string pr = str.Length > 35 ? str.Substring(0, 30) : str;
        string lineString;
        lineString = "lines " + lineNums[node.Span.Start] + "-" + lineNums[node.Span.End];


        Console.WriteLine($"{indent}{node.GetType().Name}:{str.Length}:{lineString}:{pr}");
        foreach (var child in node.ChildNodes())
        {
            Print(child, indent + "  ", lineNums);
        }
    }
}