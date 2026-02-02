using CodeLib;
using SystemWrapper.IO;

namespace CodeAnalyzerCli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CodeStreamer s = new();
            s.ProcessFolder(new DirectoryInfo(args[0]));
        }
    }
}
