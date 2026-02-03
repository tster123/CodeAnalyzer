using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    public int Lambdas { get; set; }
}

public class CSharpMetrics
{
    private readonly Stack<Class> _classStack = new();
    private Class CurrentClass => _classStack.Peek();

    private readonly Stack<Method> _methodStack = new();
    private Method CurrentMethod => _methodStack.Peek();

    public List<Class> Classes = new();

    private Dictionary<Type, MethodInfo> _visitors = new();

    public CSharpMetrics()
    {
        foreach (MethodInfo m in typeof(CSharpMetrics).GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            if (m.Name == "Visit")
            {
                Debug.Assert(m.GetParameters().Length == 1);
                Type paramType = m.GetParameters()[0].ParameterType;
                Debug.Assert(!_visitors.ContainsKey(paramType));
                _visitors[paramType] = m;
            }
        }
    }

    public void Walk(SyntaxNode node)
    {
        if (_visitors.TryGetValue(node.GetType(), out MethodInfo visitor))
        {
            visitor.Invoke(this, [node]);
        }
        else
        {
            WalkChildren(node);
        }
    }

    public void Visit(ClassDeclarationSyntax node)
    {
        Class m = new();
        m.Name = node.Identifier.ValueText;
        _classStack.Push(m);

        WalkChildren(node);
        
        Class m2 = _classStack.Pop();
        Debug.Assert(m == m2);
        Classes.Add(m);
    }

    public void Visit(MethodDeclarationSyntax node)
    {
        Method m = new();
        m.Name = node.Identifier.ValueText;
        _methodStack.Push(m);
        
        WalkChildren(node);
        
        Method m2 = _methodStack.Pop();
        Debug.Assert(m == m2);
        CurrentClass.Methods.Add(m);
    }

    public void Visit(DocumentationCommentTriviaSyntax comment)
    {
    }

    private void WalkChildren(SyntaxNode node)
    {
        foreach (SyntaxTrivia t in node.GetLeadingTrivia())
        {
            SyntaxNode? trivia = t.GetStructure();
            if (trivia != null) Walk(trivia);
        }
        foreach (SyntaxTrivia t in node.GetTrailingTrivia())
        {
            SyntaxNode? trivia = t.GetStructure();
            if (trivia != null) Walk(trivia);
        }
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

        CSharpMetrics w = new();
        w.Walk(node);
        Console.WriteLine(w.Classes[0].ToString());
    }

    

    public void Print(SyntaxNode node, string indent, uint[] lineNums)
    {
        string str = node.ToString().Replace("\n", "\\n").Replace("\r", "");
        string pr = str.Length > 35 ? str.Substring(0, 30) : str;
        string lineString;
        lineString = "lines " + lineNums[node.Span.Start] + "-" + lineNums[node.Span.End];
        string nodePrint = $"{indent}{node.GetType().Name}:{str.Length}:{lineString}:{pr}";

        
        if (node is DocumentationCommentTriviaSyntax)
        {
            Console.WriteLine(nodePrint);
            return; // don't descend into XML comments
        }

        SyntaxTriviaList list = node.GetLeadingTrivia();
        foreach (SyntaxTrivia t in list)
        {
            if (t.Kind() == SyntaxKind.MultiLineCommentTrivia || 
                t.Kind() == SyntaxKind.SingleLineCommentTrivia ||
                t.Kind() == SyntaxKind.XmlComment)
            {
                PrintTrivia(t, indent + ":LEAD:", lineNums);
                continue;
            }
            SyntaxNode? trivia = t.GetStructure();
            if (trivia != null)
                Print(trivia, indent + "  ", lineNums);
        }
        Console.WriteLine(nodePrint);
        SyntaxTriviaList list2 = node.GetTrailingTrivia();
        foreach (SyntaxTrivia t in list2)
        {
            if (t.Kind() == SyntaxKind.MultiLineCommentTrivia ||
                t.Kind() == SyntaxKind.SingleLineCommentTrivia ||
                t.Kind() == SyntaxKind.XmlComment)
            {
                PrintTrivia(t, indent + ":TRAIL:", lineNums);
                continue;
            }
            SyntaxNode? trivia = t.GetStructure();
            if (trivia != null)
                Print(trivia, indent + "  ", lineNums);
        }
        foreach (var child in node.ChildNodes())
        {
            Print(child, indent + "  ", lineNums);
        }
        
    }

    private void PrintTrivia(SyntaxTrivia syntaxTrivia, string indent, uint[] lineNums)
    {
        string str = syntaxTrivia.ToString().Replace("\n", "\\n").Replace("\r", "");
        string pr = str.Length > 35 ? str.Substring(0, 30) : str;
        string lineString;
        lineString = "lines " + lineNums[syntaxTrivia.Span.Start] + "-" + lineNums[syntaxTrivia.Span.End];


        Console.WriteLine($"{indent}{syntaxTrivia.Kind()}:{str.Length}:{lineString}:{pr}");
    }
}