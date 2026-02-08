using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CodeLib;

public class CodeStreamer
{
    public uint Errors;
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
        CSharpMetrics w;
        try
        {
            using FileStream s = f.OpenRead();
            SourceText source = SourceText.From(s);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: f.FullName);
            w = new CSharpMetrics(tree);
            w.WalkTree();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Error in file: " + f.FullName + "\n" + e);
            Errors++;
            return;
        }

        foreach (Namespace n in w.Namespaces)
        {
            Console.WriteLine(n);
            foreach (Class c in n.Classes)
            {
                Console.WriteLine(c);
                foreach (Method m in c.Methods)
                {
                    Console.WriteLine("  " + m);
                }
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