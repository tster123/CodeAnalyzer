using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Wrapped.System.IO;

namespace CodeLib.Diff;

public class TokenizerSettings
{
    public bool IgnoreStartingWhitespace { get; set; }
    public bool IgnoreEndingWhitespace { get; set; }
}

public interface IFileTokenizer
{
    List<ulong> Tokenize(TokenizerSettings settings, string filePath, IStreamWrap bytes);
}

public class TextFileByLines : IFileTokenizer
{
    public List<string>? Accumulator { get; set; } = null;
    
    public List<ulong> Tokenize(TokenizerSettings settings, string filePath, IStreamWrap bytes)
    {
        using ITextReaderWrap textReader = new TextReaderWrap(new StreamReader(bytes.WrappedStream));
        return Tokenize(settings, filePath, textReader);
    }

    public List<ulong> Tokenize(TokenizerSettings settings, string filePath, ITextReaderWrap reader)
    {
        List<ulong> ret = new();
        while (true)
        {
            string? line = reader.ReadLine();
            if (line == null) break;
            Accumulator?.Add(line);
            // TODO: faster trimming
            if (settings.IgnoreEndingWhitespace)
                line = line.TrimEnd();
            if (settings.IgnoreStartingWhitespace) 
                line = line.TrimStart();
            byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(line));
            Debug.Assert(hash.Length == 16);
            ret.Add(BitConverter.ToUInt64(hash, 0) ^ BitConverter.ToUInt64(hash, 8));
        }

        return ret;
    }
}
