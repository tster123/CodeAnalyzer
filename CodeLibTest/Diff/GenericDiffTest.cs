using System.Runtime.InteropServices.Marshalling;
using CodeLib.Diff;
#pragma warning disable MSTEST0037

[TestClass]
public class QuadraticMeyersDiffTest
{
    private GenericDiffTester tester = new(new QuadraticMeyersDiff());

    [TestMethod]
    public void OnlyInsert()
    {
        tester.RunTest([], [1, 2, 3, 4, 5, 6]);
        tester.RunTest(6, "", "123456");
    }

    [TestMethod]
    public void OnlyDelete()
    {
        tester.RunTest([1, 2, 3, 4, 5, 6], []);
    }

    [TestMethod]
    public void SimpleEdits()
    {
        tester.RunTest([1, 2], [1, 3, 2]);
        tester.RunTest([1, 5, 2], [1, 2]);
        tester.RunTest([1, 2], [1, 2, 3]);
    }

    [TestMethod]
    public void Deletes()
    {
        tester.RunTest(
            [1, 2, 3, 4, 5, 6, 7], 
            [1, 2, 6, 7]);
    }

    [TestMethod]
    public void Inserts()
    {
        tester.RunTest(9, [1, 2, 3, 4, 5, 6], [1, 2, 3, 10, 11, 4, 5, 12, 6]);
    }

    [TestMethod]
    public void Movements()
    {
        tester.RunTest(
            [1, 2, 3, 4, 5, 6, 7],
            [1, 2, 6, 7, 3, 4, 5]);
    }
    
    [TestMethod]
    public void Bigger()
    {
        tester.RunTest(15 + 2 + 1,
            "12233234  1234671", // 15 long
            "122  234421235 71"); // 3 deletes, 2 adds, 1 modify
    }
}

internal class GenericDiffTester
{
    private readonly IDiffAlgorithm algo;

    internal GenericDiffTester(IDiffAlgorithm algo)
    {
        this.algo = algo;
    }

    internal void RunTest(uint[] a, uint[] b)
    {
        RunTest(-1, a, b);
    }

    internal void RunTest(int expectedPartCount, string a, string b)
    {
        RunTest(expectedPartCount,
            a.Where(c => c != ' ').Select(c => (uint) c).ToArray(),
            b.Where(c => c != ' ').Select(c => (uint) c).ToArray());
    }

    internal void RunTest(int expectedPartCount, uint[] a, uint[] b)
    {
        List<DiffPart> diff = algo.Diff(a, b);
        for (int i = diff.Count - 1; i >= 0; i--)
        {
            Console.WriteLine(diff[i]);
        }
        uint[] bCheck = new uint[b.Length];
        uint[] aCheck = new uint[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            aCheck[i] = a[i];
        }
        foreach (DiffPart d in diff)
        {
            if (d.Operation == Operation.Delete)
            {
                Assert.IsTrue(d.APos < aCheck.Length);
                aCheck[d.APos] = 0;
            }

            else if (d.Operation == Operation.Insert)
            {
                Assert.IsTrue(d.BPos < bCheck.Length);
                bCheck[d.BPos] = b[d.BPos];
            }

            else if (d.Operation == Operation.Keep)
            {
                Assert.IsTrue(d.BPos < bCheck.Length);
                Assert.IsTrue(d.APos < aCheck.Length);
                bCheck[d.BPos] = a[d.APos];
                aCheck[d.APos] = 0;
            }
        }

        for (int i = 0; i < aCheck.Length; i++)
        {
            Assert.AreEqual(0U, aCheck[i]);
        }
        for (int i = 0; i < bCheck.Length; i++)
        {
            Assert.AreEqual(b[i], bCheck[i]);
        }

        if (expectedPartCount != -1)
        {
            Assert.AreEqual(expectedPartCount, diff.Count);
        }
    }

}
