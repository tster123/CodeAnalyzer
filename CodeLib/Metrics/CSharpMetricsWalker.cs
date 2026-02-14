using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeLib.Metrics;

public class CSharpMetricsWalker
{
    private readonly Stack<Class> classStack = new();
    private Class? CurrentClass => classStack.TryPeek(out var v) ? v : null;

    private readonly Stack<Method> methodStack = new();
    private Method? CurrentMethod => methodStack.TryPeek(out var v) ? v : null;

    private readonly Namespace blankNamespace = new("");
    private Dictionary<string, Namespace> namespaceHash = new();
    private readonly LinkedList<Namespace> namespaceStack = new();
    private Namespace CurrentNamespace => namespaceStack.Last();

    public IEnumerable<Namespace> Namespaces => namespaceHash.Values.ToList();

    private static readonly Dictionary<Type, MethodInfo> Visitors = new();

    static CSharpMetricsWalker()
    {
        foreach (MethodInfo m in typeof(CSharpMetricsWalker).GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            if (m.Name == "Visit")
            {
                Debug.Assert(m.GetParameters().Length == 1);
                Type paramType = m.GetParameters()[0].ParameterType;
                Debug.Assert(!Visitors.ContainsKey(paramType));
                Visitors[paramType] = m;
            }
        }
    }

    public SyntaxTree Tree { get; }
    private readonly Dictionary<SyntaxNode, List<SyntaxTrivia>> commentLookup;
    private uint[] lineNums;

    public CSharpMetricsWalker(SyntaxTree tree)
    {
        namespaceHash[""] = blankNamespace;
        namespaceStack.AddLast(blankNamespace);

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
        if (Visitors.TryGetValue(node.GetType(), out MethodInfo? visitor))
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

        if (node is GenericNameSyntax g)
        {
            tokenCount++; // List<bool> the "List" part isn't a child
        }

        if (node is PostfixUnaryExpressionSyntax or PrefixUnaryExpressionSyntax)
        {
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
                if (insideMethodContract)
                {
                    m.ReturnAndParameterComplexity += tokenCount;
                }
                else
                {
                    m.CodeTokens += tokenCount;
                }
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

    public void Visit(ParameterSyntax node)
    {
        var m = CurrentMethod;
        if (m != null) m.Parameters++;
        else
        {
            //TODO: dewbug    
        }
        DefaultVisit(node);
    }

    #region Namespace
    private void VisitNamespaceDeclaration(BaseNamespaceDeclarationSyntax node)
    {
        string name = node.Name.ToString();
        var llNode = namespaceStack.Last;
        while (llNode != null)
        {
            if (!string.IsNullOrEmpty(llNode.Value.Name))
            {
                name = llNode.Value.Name + "." + name;
            }
            llNode = llNode.Previous;
        }

        Namespace n;
        if (namespaceHash.TryGetValue(name, out Namespace? existing))
        {
            n = existing;
        }
        else
        {
            n = new Namespace(name);
            namespaceHash[n.Name] = n;
        }
        namespaceStack.AddLast(n);
        WalkChildren(node);
    }

    [UsedImplicitly]
    public void Visit(NamespaceDeclarationSyntax node)
    {
        VisitNamespaceDeclaration(node);
        namespaceStack.RemoveLast();
    }

    [UsedImplicitly]
    public void Visit(FileScopedNamespaceDeclarationSyntax node)
    {
        VisitNamespaceDeclaration(node);
    }
    #endregion
    #region Type Containers (class, struct, record, enum)
    private void VisitTypeDeclaration(BaseTypeDeclarationSyntax node, ClassType type)
    {
        Class m = new(type, node.Identifier.ValueText);
        classStack.Push(m);

        WalkChildren(node);

        Class m2 = classStack.Pop();
        Debug.Assert(m == m2);
        CurrentNamespace.Classes.Add(m);
    }

    [UsedImplicitly]
    public void Visit(InterfaceDeclarationSyntax node)
    {
        VisitTypeDeclaration(node, ClassType.Interface);
    }

    [UsedImplicitly]
    public void Visit(StructDeclarationSyntax node)
    {
        VisitTypeDeclaration(node, ClassType.Struct);
    }

    [UsedImplicitly]
    public void Visit(ClassDeclarationSyntax node)
    {
        VisitTypeDeclaration(node, ClassType.Class);
    }

    [UsedImplicitly]
    public void Visit(RecordDeclarationSyntax node)
    {
        VisitTypeDeclaration(node, ClassType.Record);
    }

    [UsedImplicitly]
    public void Visit(EnumDeclarationSyntax node)
    {
        VisitTypeDeclaration(node, ClassType.Enum);
    }
    #endregion

    [UsedImplicitly]
    public void Visit(MethodDeclarationSyntax node)
    {
        Method m = new(node.Identifier.ValueText);
        methodStack.Push(m);
        
        WalkChildren(node);
        
        Method m2 = methodStack.Pop();
        Debug.Assert(m == m2);
        CurrentClass!.Methods.Add(m);
    }

    private bool insideMethodContract = false;
    [UsedImplicitly]
    public void Visit(ParameterListSyntax node)
    {
        insideMethodContract = true;
        WalkChildren(node);
        insideMethodContract = false;

        //TODO handle return complexity
    }
}