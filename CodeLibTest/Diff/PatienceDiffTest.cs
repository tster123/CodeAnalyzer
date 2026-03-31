using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text;
using CodeLib.Diff;

namespace CodeLibTest.Diff;

[TestClass]
public class PatienceDiffTest
{
    private void AssertSingles(SingleOccurenceClass[] singles, (int, ulong)[] expected)
    {
        foreach ((int, ulong) e in expected)
        {
            bool found = false;
            foreach (SingleOccurenceClass o in singles)
            {
                if (e.Item1 == o.Location && e.Item2 == o.Value)
                {
                    found = true;
                    break;
                }
            }

            if (!found) Assert.Fail($"Cannot find (Loc={e.Item1}, Val={e.Item2}) in [{singles}]");
        }
    }

    private void AssertSingles(SingleOccurence[] singles, (int, ulong)[] expected)
    {
        foreach ((int, ulong) e in expected)
        {
            bool found = false;
            foreach (SingleOccurence o in singles)
            {
                if (e.Item1 == o.Location && e.Item2 == o.Value)
                {
                    found = true;
                    break;
                }
            }

            if (!found) Assert.Fail($"Cannot find (Loc={e.Item1}, Val={e.Item2}) in [{singles}]");
        }
    }

    [TestMethod]
    public void TestSingleOccurence()
    {
        List<ulong> list = [1, 5, 4, 2, 4, 10, 1, 9, 5, 4, 4, 1, 0];

        SingleOccurenceClass[] ret = PatienceDiff.GetSingles(list);
        AssertSingles(ret, [(3, 2), (5, 10), (7, 9), (12, 0)]);
    }

    [TestMethod]
    public void TestSingleOccurenceAlt()
    {
        List<ulong> list = [1, 5, 4, 2, 4, 10, 1, 9, 5, 4, 4, 1, 0];

        SingleOccurence[] ret = PatienceDiff.GetSinglesAlt(list);
        AssertSingles(ret, [(3, 2), (5, 10), (7, 9), (12, 0)]);
    }

    [TestMethod]
    public void PerfSingleOccurence()
    {
        int length = 1000;
        int maxRand = 300;
        List<ulong> list = new(length);
        Random r = new Random();
        for (int i = 0; i < length; i++)
        {
            list.Add((ulong)r.NextInt64(maxRand));
        }

        int runCount = 100000;
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < runCount; i++)
        {
            SingleOccurenceClass[] ret = PatienceDiff.GetSingles(list);
        }

        TimeSpan a = sw.Elapsed;
        sw.Restart();
        for (int i = 0; i < runCount; i++)
        {
            SingleOccurence[] ret = PatienceDiff.GetSinglesAlt(list);
        }
        TimeSpan b = sw.Elapsed;
        Console.WriteLine($"a={a}, b={b}");
    }
}