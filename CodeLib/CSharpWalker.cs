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
    public uint CommentLines { get; set; }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Methods)}: [{string.Join(", ", Methods.Select(m => m.ToString()))}], {nameof(CommentLines)}: {CommentLines}";
    }
}

public class Method
{
    public string Name { get; set; }
    public uint CyclomaticComplexity { get; set; }
    public uint CommentLines { get; set; }
    public uint CodeTokens { get; set; }
    public uint Lambdas { get; set; }

    public override string ToString()
    {
        return
            $"{nameof(Name)}: {Name}, {nameof(CyclomaticComplexity)}: {CyclomaticComplexity}, {nameof(CommentLines)}: {CommentLines}, {nameof(CodeTokens)}: {CodeTokens}, {nameof(Lambdas)}: {Lambdas}";
    }
}

public class CSharpMetrics
{
    private readonly Stack<Class> classStack = new();
    private Class? CurrentClass => classStack.TryPeek(out var v) ? v : null;

    private readonly Stack<Method> methodStack = new();
    private Method? CurrentMethod => methodStack.TryPeek(out var v) ? v : null;

    public List<Class> Classes = new();

    private static readonly Dictionary<Type, MethodInfo> visitors = new();

    static CSharpMetrics()
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

    public SyntaxTree Tree { get; }
    private Dictionary<SyntaxNode, List<SyntaxTrivia>> commentLookup { get; }
    private uint[] lineNums;

    public CSharpMetrics(SyntaxTree tree)
    {
        Tree          = tree;
        commentLookup = BuildComments();

        string text = tree.GetRoot().ToFullString();
        lineNums = new uint[text.Length + 1];
        uint currentLine = 1;
        for (int i = 0; i < text.Length + 1; i++)
        {
            if (i < text.Length && text[i] == '\n') currentLine++;
            lineNums[i] = currentLine;
        }
    }

    private static readonly HashSet<SyntaxKind> CommentKinds =
    [
        SyntaxKind.SingleLineCommentTrivia,
        SyntaxKind.MultiLineCommentTrivia,
        SyntaxKind.DocumentationCommentExteriorTrivia,
        SyntaxKind.SingleLineDocumentationCommentTrivia,
        SyntaxKind.MultiLineDocumentationCommentTrivia
    ];
    private Dictionary<SyntaxNode, List<SyntaxTrivia>> BuildComments()
    {
        Dictionary<SyntaxNode, List<SyntaxTrivia>> ret = new();
        SyntaxNode root = Tree.GetRoot();
        foreach (SyntaxTrivia t in root.DescendantTrivia())
        {
            if (!CommentKinds.Contains(t.Kind())) continue;
            SyntaxNode n = t.Token.Parent ?? root;
            if (!ret.TryGetValue(n, out var list))
            {
                list   = new();
                ret[n] = list;
            }
            list.Add(t);
        }

        return ret;
    }

    public void WalkTree()
    {
        Walk(Tree.GetRoot());
    }

    private void Walk(SyntaxNode node)
    {
        if (visitors.TryGetValue(node.GetType(), out MethodInfo visitor))
        {
            visitor.Invoke(this, [node]);
        }
        else
        {
            DefaultVisit(node);
        }
    }

    private void WalkChildren(SyntaxNode node)
    {
        foreach (var c in node.ChildNodes())
        {
            Walk(c);
        }
    }

    public void DefaultVisit(SyntaxNode node)
    {
        uint tokenCount = 0, commentLines = 0, branchCount = 0, lambdaCount = 0;
        if (!node.ChildNodes().Any())
        {
            // leaf nodes are tokens
            tokenCount++;
        }

        if (node is BinaryExpressionSyntax b)
        {
            tokenCount++;
            var kind = b.Kind();
            if (kind == SyntaxKind.CoalesceExpression ||
                kind == SyntaxKind.LogicalOrExpression ||
                kind == SyntaxKind.LogicalAndExpression)
                branchCount++;
        }

        if (node is AssignmentExpressionSyntax)
        {
            var kind = node.Kind();
            if (kind == SyntaxKind.CoalesceAssignmentExpression)
            {
                branchCount++;
            }
        }

        if (node is IfStatementSyntax ||
            node is WhileStatementSyntax ||
            node is ForEachStatementSyntax ||
            node is ForStatementSyntax ||
            node is CaseSwitchLabelSyntax ||
            node is ConditionalAccessExpressionSyntax ||
            node is ConditionalExpressionSyntax)
        {
            branchCount++;
        }

        if (node is LambdaExpressionSyntax)
        {
            lambdaCount++;
        }

        if (commentLookup.TryGetValue(node, out var comments))
        {
            foreach (SyntaxTrivia comment in comments)
            {
                commentLines += 1 + lineNums[comment.Span.End] - lineNums[comment.Span.Start];
            }
        }

        if (lambdaCount + branchCount + tokenCount + commentLines > 0)
        {
            Method? m = CurrentMethod;
            Class? c = CurrentClass;
            if (m != null)
            {
                m.CodeTokens           += tokenCount;
                m.CommentLines         += commentLines;
                m.CyclomaticComplexity += branchCount;
                m.Lambdas              += lambdaCount;
            }
            else if (c != null)
            {
                c.CommentLines += commentLines;
            }
        }

        WalkChildren(node);
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
        m.Name                 = node.Identifier.ValueText;
        m.CyclomaticComplexity = 2;
        methodStack.Push(m);
        
        WalkChildren(node);
        
        Method m2 = methodStack.Pop();
        Debug.Assert(m == m2);
        CurrentClass.Methods.Add(m);
    }
}

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

        CSharpMetrics w = new(tree);
        w.WalkTree();
        Console.WriteLine(w.Classes[0].ToString());
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
        string lineString;
        lineString = "lines " + lineNums[node.Span.Start] + "-" + lineNums[node.Span.End];
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