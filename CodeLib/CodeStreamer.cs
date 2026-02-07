using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CodeLib;

public class CodeStreamer
{

    public void ProcessFolder(DirectoryInfo dir)
    {
        foreach (FileInfo f in dir.GetFiles())
        {
            if (f.Extension == ".cs")
            {
                ProcessCSharp(f);
            }
        }

        foreach (var c in dir.GetDirectories())
        {
            if (c.Name == "obj" || c.Name == "bin") continue;
            ProcessFolder(c);
        }
    }

    public void ProcessCSharp(FileInfo f)
    {
        using FileStream s = f.OpenRead();
        SourceText source = SourceText.From(s);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: f.FullName);

        CSharpMetrics w = new(tree);
        w.WalkTree();
        foreach (Class c in w.Classes)
        {
            Console.WriteLine(c);
            foreach (Method m in c.Methods)
            {
                Console.WriteLine("  " + m);
            }
        }
        
        /*CSharpWalker w = new();
        Console.WriteLine(":::::::::::::::::::::::::::::::");
        Console.WriteLine("start: " + f.FullName);
        Console.WriteLine(":::::::::::::::::::::::::::::::");
        w.Walk(tree);
        Console.WriteLine(":::::::::::::::::::::::::::::::");
        Console.WriteLine("end: " + f.FullName);
        Console.WriteLine(":::::::::::::::::::::::::::::::");*/
    }
}
