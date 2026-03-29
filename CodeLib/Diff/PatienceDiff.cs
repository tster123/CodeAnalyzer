
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeLib.Diff;

public class TextFilePatch
{
}

public record struct PatchRange
{

}

public class Insertion
{

}

public class PatienceDiff
{
    
}

internal record struct PatienceSection
{
    internal readonly uint ALow, AHigh, BLow, BHigh;

    public PatienceSection(uint aLow, uint aHigh, uint bLow, uint bHigh)
    {
        ALow  = aLow;
        AHigh = aHigh;
        BLow  = bLow;
        BHigh = bHigh;
    }
}