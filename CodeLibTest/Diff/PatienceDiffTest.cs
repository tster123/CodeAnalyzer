using System.Diagnostics;
using CodeLib.Diff;

namespace CodeLibTest.Diff;

[TestClass]
public class PatienceDiffTest
{
    private void AssertSingles(PatienceMatch[] singles, (int, int)[] expected)
    {
        foreach ((int, int) e in expected)
        {
            bool found = false;
            foreach (PatienceMatch o in singles)
            {
                if (e.Item1 == o.LocA && e.Item2 == o.LocB)
                {
                    found = true;
                    break;
                }
            }

            if (!found) Assert.Fail($"Cannot find (Loc={e.Item1}, Val={e.Item2}) in [{singles}]");
        }
    }
    /*
    private void AssertSingles(SingleOccurence[] singles, (int, int, ulong)[] expected)
    {
        foreach ((int, int, ulong) e in expected)
        {
            bool found = false;
            foreach (SingleOccurence o in singles)
            {
                if (e.Item1 == o.LocationA && e.Item2 == o.LocationB && e.Item3 == o.Value)
                {
                    found = true;
                    break;
                }
            }

            if (!found) Assert.Fail($"Cannot find (Loc={e.Item1}, Val={e.Item2}) in [{singles}]");
        }
    }*/

    [TestMethod]
    public void TestSingleOccurence()
    {
        //               0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14
        //                        2     3     9              0
        List<ulong> a = [1, 5, 4, 2, 4, 3, 1, 9, 5, 4, 4, 1, 0];
        //                  6           0           9              3
        List<ulong> b = [1, 6, 4, 1, 4, 0, 1, 4, 5, 9, 4, 1, 1, 1, 3];

        PatienceMatch[] ret = PatienceDiff.GetSingles(a, b);
        AssertSingles(ret, [(12, 5), (7, 9), (5, 14)]);
    }
    /*
    [TestMethod]
    public void TestSingleOccurenceAlt()
    {
        List<ulong> list = [1, 5, 4, 2, 4, 10, 1, 9, 5, 4, 4, 1, 0];

        SingleOccurence[] ret = PatienceDiff.GetSinglesAlt(list);
        AssertSingles(ret, [(3, 2), (5, 10), (7, 9), (12, 0)]);
    }
    */
    [TestMethod]
    public void PerfSingleOccurence()
    {
        int length = 1000;
        int maxRand = 500;
        List<ulong> a = new(length);
        List<ulong> b = new(length);
        Random r = new Random();
        for (int i = 0; i < length; i++)
        {
            a.Add((ulong)r.NextInt64(maxRand));
            b.Add((ulong)r.NextInt64(maxRand));
        }

        int runCount = 100000;
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < runCount; i++)
        {
            PatienceMatch[] ret = PatienceDiff.GetSingles(a, b);
        }
    /*
        TimeSpan a = sw.Elapsed;
        sw.Restart();
        for (int i = 0; i < runCount; i++)
        {
            SingleOccurence[] ret = PatienceDiff.GetSinglesAlt(a, b);
        }
        TimeSpan b = sw.Elapsed;
        Console.WriteLine($"a={a}, b={b}");
    */
    }
}