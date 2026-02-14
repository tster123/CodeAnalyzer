using CodeLib;
using SystemWrapper.IO;

namespace CodeAnalyzerCli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool metrics = false, printAst = false;
            if (args.Length == 0)
            {
                Usage();
                return;
            }
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].ToLower() == "--metrics") metrics = true;
                else if (args[i].ToLower() == "--ast") printAst = true;
                else
                {
                    Usage();
                    return;
                }
            }
            if (args.Length == 1)
            {
                metrics = true;
            }

            CodeStreamer s = new(metrics, printAst);
            s.ProcessFolder(new DirectoryInfo(args.Last()));
            Console.WriteLine("Errors: " + s.Errors);
        }

        private static void Usage()
        {
            Console.WriteLine("CodeAnalyzerCli.exe [--metrics] [--ast] <dir>");
        }
    }
}
