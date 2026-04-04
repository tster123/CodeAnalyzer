namespace CodeLib.Diff;

public record struct Patch
{
    public uint APos;
    public uint BPos;
    public uint Len;
    public PatchType Type;

    private Patch(uint sourcePosition, uint targetPosition, uint length, PatchType type)
    {
        APos = sourcePosition;
        BPos = targetPosition;
        Len = length;
        Type = type;
    }

    public static Patch Insert(uint aPos, uint bPos, uint len) => new(aPos, bPos, len, PatchType.Insert);
    public static Patch Remove(uint aPos, uint len) => new(aPos, 0, len, PatchType.Remove);
    public static Patch NoChange(uint aPos, uint len) => new(aPos, 0, len, PatchType.NoChange);
}

public enum PatchType : sbyte
{
    Remove,
    Insert,
    NoChange
}
public class PatienceDiff
{
    private List<ulong> a, b;

    public PatienceDiff(List<ulong> a, List<ulong> b)
    {
        this.a = a;
        this.b = b;
    }

    public List<Patch> Diff()
    {
        return DiffInternal(new PatienceSection(0, a.Count, 0, b.Count));
    }

    private List<Patch> DiffInternal(PatienceSection section)
    {
        throw new NotImplementedException();
    }

    internal static PatienceMatch[] GetSingles(List<ulong> a, List<ulong> b)
    {
        int numMultiple = 0;
        List<SingleOccurenceClass> occur = new(Math.Min(10, a.Count / 10));
        Dictionary<ulong, SingleOccurenceClass> seen = new();
        for (int i = 0; i < a.Count; i++)
        {
            ulong val = a[i];
            if (seen.TryGetValue(val, out SingleOccurenceClass? o))
            {
                if (!o.MultipleSeen)
                {
                    numMultiple++;
                    o.MultipleSeen = true;
                }
            }
            else
            {
                o = new SingleOccurenceClass(i, val, false);
                seen[val] = o;
                occur.Add(o);
            }
        }

        for (int i = 0; i < b.Count; i++)
        {
            ulong val = b[i];
            if (seen.TryGetValue(val, out SingleOccurenceClass? o))
            {
                if (!o.MultipleSeen)
                {
                    if (o.LocationB == -2)
                        o.LocationB = i; // first time in B.
                    else
                    {
                        numMultiple++;
                        o.LocationB = -1; // has already been in b, so mark it as -1
                    }
                }
            }
        }

        PatienceMatch[] ret = new PatienceMatch[occur.Count - numMultiple];
        int j = 0;
        foreach (SingleOccurenceClass o in occur)
        {
            if (!o.MultipleSeen && o.LocationB >= 0)
            {
                ret[j] = new PatienceMatch(o.LocationA, o.LocationB);
                j++;
            }
        }

        return ret;
    }

    //private static readonly int _sizeOfSingleOccurence = Unsafe.SizeOf<SingleOccurence>();

    /*internal static SingleOccurence[] GetSinglesAlt(List<ulong> list)
    {
        int numMultiple = 0;
        SingleOccurence[] occur = new SingleOccurence[Math.Min(10, list.Count / 10)];
        int occurIndex = 0;
        Dictionary<ulong, uint> seen = new();
        for (int i = 0; i < list.Count; i++)
        {
            ulong val = list[i];
            if (seen.TryGetValue(val, out uint timesSeen))
            {
                if (timesSeen == 2)
                {
                    numMultiple++;
                    for (int j = 0; j < occurIndex; j++)
                    {
                        ref SingleOccurence o = ref occur[j];
                        if (o.Value == val) o.MultipleSeen = true;
                    }
                }
            }
            else
            {
                if (occurIndex >= occur.Length)
                {
                    SingleOccurence[] newOccur = new SingleOccurence[Math.Min(list.Count, 2 * occur.Length)];
                    // TODO: can't copy this faster?
                    for (int a = 0; a < occur.Length; a++)
                    {
                        newOccur[a] = occur[a];
                    }

                    occur = newOccur;
                }
                occur[occurIndex] = new SingleOccurence(i, val, false);
                occurIndex++;
            }
        }

        SingleOccurence[] ret = new SingleOccurence[occurIndex - numMultiple];
        int k = 0;
        foreach (SingleOccurence o in occur)
        {
            if (!o.MultipleSeen)
            {
                ret[k] = o;
                k++;
            }
        }

        return ret;
    }
    */
}

//internal record struct SingleOccurence(int Location, ulong Value, bool MultipleSeen);

internal record PatienceMatch(int LocA, int LocB)
{
    internal PatienceMatch Prev, Next;
}

internal record SingleOccurenceClass(int LocationA, ulong Value, bool MultipleSeen)
{
    public bool MultipleSeen = MultipleSeen;
    public int LocationB = -2;
}

internal record struct PatienceSection
{
    internal readonly int ALow, AHigh, BLow, BHigh;

    public PatienceSection(int aLow, int aHigh, int bLow, int bHigh)
    {
        ALow  = aLow;
        AHigh = aHigh;
        BLow  = bLow;
        BHigh = bHigh;
    }
}