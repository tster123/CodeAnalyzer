using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

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
    private readonly Stack<Class> classStack = new();
    private Class CurrentClass => classStack.Peek();

    private readonly Stack<Method> methodStack = new();
    private Method CurrentMethod => methodStack.Peek();

    public List<Class> Classes = new();

    private readonly Dictionary<Type, MethodInfo> visitors = new();

    public CSharpMetrics()
    {
        foreach (MethodInfo m in typeof(CSharpMetrics).GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            if (m.Name == "Visit")
            {
                Debug.Assert(m.GetParameters().Length == 1);
                Type paramType = m.GetParameters()[0].ParameterType;
                Debug.Assert(!visitors.ContainsKey(paramType));
                visitors[paramType] = m;
            }
        }
    }

    public void Walk(SyntaxNode node)
    {
        if (visitors.TryGetValue(node.GetType(), out MethodInfo visitor))
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
        classStack.Push(m);

        WalkChildren(node);
        
        Class m2 = classStack.Pop();
        Debug.Assert(m == m2);
        Classes.Add(m);
    }

    public void Visit(MethodDeclarationSyntax node)
    {
        Method m = new();
        m.Name = node.Identifier.ValueText;
        methodStack.Push(m);
        
        WalkChildren(node);
        
        Method m2 = methodStack.Pop();
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

    private static readonly HashSet<SyntaxKind> commentKinds =
    [
        SyntaxKind.SingleLineCommentTrivia,
        SyntaxKind.MultiLineCommentTrivia,
        SyntaxKind.DocumentationCommentExteriorTrivia,
        SyntaxKind.SingleLineDocumentationCommentTrivia,
        SyntaxKind.MultiLineDocumentationCommentTrivia
    ];

    private SyntaxNode? GetParentNonTrivia(SyntaxNode? n)
    {
        while (n != null && n.GetType().Name.Contains("Trivia"))
        {
            n = n.Parent;
        }

        return n;
    }

    public void PrintTrivia(IEnumerable<SyntaxTrivia> triviaList, string indent, uint[] lineNums)
    {
        // trivia strategy is going to be:
        // 1) XML docs are attached to their parent method/class
        // 2) all other comments are loaded from top level doc and included in whatever method/class first closes that
        //    spans where the comment is.
        foreach (SyntaxTrivia t in triviaList)
        {
            if (commentKinds.Contains(t.Kind()))
            {
                string i = indent + GetParentNonTrivia(t.GetStructure()) + ":";
                PrintTrivia(t, i, lineNums);
                continue;
            }
            SyntaxNode? trivia = t.GetStructure();
            if (trivia != null)
                Print(trivia, indent + "  ", lineNums);
        }
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
            Console.WriteLine("XML" + node.Parent.GetType().Name + "-" + nodePrint);
            return; // don't descend into XML comments
        }

        PrintTrivia(node.GetLeadingTrivia(), indent + ":LEAD:", lineNums);
        Console.WriteLine(nodePrint);
        foreach (var child in node.ChildNodes())
        {
            Print(child, indent + "  ", lineNums);
        }
        PrintTrivia(node.DescendantTrivia(), indent + ":DESCEND:", lineNums);
        PrintTrivia(node.GetTrailingTrivia(), indent + ":TRAIL:", lineNums);
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