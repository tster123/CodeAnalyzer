namespace CodeLib.Diff;

public enum Operation : byte
{
    Insert, Delete, Keep
}
public struct DiffPart
{
    public Operation Operation;
    public uint APos;
    public uint BPos;
    public uint Length;

    public DiffPart(Operation operation, uint aPos, uint bPos, uint length)
    {
        Operation = operation;
        APos      = aPos;
        BPos      = bPos;
        Length    = length;
    }

    public DiffPart(Operation operation, int aPos, int bPos, uint length)
        : this(operation, (uint) aPos, (uint) bPos, length)
    {
    }

    public override string ToString() => $"{Operation} {APos}->{BPos}, L={Length}";
}

public interface IDiffAlgorithm
{
    public List<DiffPart> Diff(Span<ulong> a, Span<ulong> b);
}