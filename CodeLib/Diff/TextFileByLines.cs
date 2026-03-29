using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Wrapped.System.IO;

namespace CodeLib.Diff;

public interface IFileTokenizer
{
    
}

public class TextFileByLines : IFileTokenizer
{
    public List<ulong> Tokenize(string filePath, IStreamWrap bytes)
    {
        using ITextReaderWrap textReader = new TextReaderWrap(new StreamReader(bytes.WrappedStream));
        return Tokenize(filePath, textReader);
    }

    public List<ulong> Tokenize(string filePath, ITextReaderWrap reader)
    {
        List<ulong> ret = new();
        while (true)
        {
            string? line = reader.ReadLine();
            if (line == null) break;
            byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(line));
            Debug.Assert(hash.Length == 16);
            ret.Add(BitConverter.ToUInt64(hash, 0) ^ BitConverter.ToUInt64(hash, 8));
        }

        return ret;
    }
}
