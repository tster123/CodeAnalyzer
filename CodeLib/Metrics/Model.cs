namespace CodeLib.Metrics;

public class Namespace(string name)
{
    public string Name { get; } = name;
    public List<Class> Classes { get; } = new();

    public override string ToString() => $"namespace {Name}";
}

public enum ClassType
{
    Class, Interface, Enum, Struct, Record
}

public class Class(ClassType type, string name)
{
    public string Name { get; } = name;
    public ClassType Type { get; } = type;
    public List<Method> Methods { get; } = new();
    public uint CommentLines { get; set; }

    public override string ToString()
    {
        return $"{Type}: {Name}, {nameof(CommentLines)}: {CommentLines}";
    }
}

public class Method(string name)
{
    public string Name { get; } = name;
    public uint CyclomaticComplexity { get; set; } = 2;
    public uint CommentLines { get; set; }
    public uint CodeTokens { get; set; }
    public uint Lambdas { get; set; }
    public uint Parameters { get; set; }
    //TODO inspect parameters.   complexity means 1 point for every type present in the declaration
    public uint ReturnAndParameterComplexity { get; set; }

    public override string ToString()
    {
        return
            $"{nameof(Name)}: {Name}, {nameof(CyclomaticComplexity)}: {CyclomaticComplexity}, {nameof(CommentLines)}: {CommentLines}, {nameof(CodeTokens)}: {CodeTokens}, {nameof(Lambdas)}: {Lambdas}, {nameof(Parameters)}: {Parameters}, ContractComplexity={ReturnAndParameterComplexity}";
    }
}
