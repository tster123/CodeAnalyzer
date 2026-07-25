namespace CodeLib.Diff;


/// <summary>
/// Simple implementation of myers algorithm that runs in O(m*n) and uses m*n space.
/// </summary>
public class QuadraticMyersDiff : IDiffAlgorithm
{
    public string Name => "myers";
    public List<DiffPart> Diff(Span<ulong> a, Span<ulong> b)
    {
        uint[,] matrix = new uint[a.Length + 1, b.Length + 1];
        // fill first and last column with cost to reach which is just straight insert/delete
        for (uint x = 0; x < a.Length + 1; x++)
        {
            matrix[x, 0] = x;
        }
        for (uint y = 0; y < b.Length + 1; y++)
        {
            matrix[0, y] = y;
        }

        // fill in cost to get to each spot.
        for (int x = 1; x < a.Length + 1; x++)
        for (int y = 1; y < b.Length + 1; y++)
        {
            if (a[x - 1] == b[y - 1])
            {
                // no change means the move is free
                matrix[x, y] = matrix[x - 1, y - 1] + 1;
            }
            else
            {
                // min cost is one change plus the cheaper of above or left
                matrix[x, y] = 1 + Math.Min(matrix[x - 1, y], matrix[x, y - 1]);
            }
        }
        
        // now walk back from the bottom right to the top left always following rule of diagonal if free or else
        // whichever one costs 1.
        int curX = a.Length;
        int curY = b.Length;
        int maxSize = a.Length + b.Length;
        Print(matrix, a.Length, b.Length);
        List<DiffPart> ret = new(maxSize);
        while (curX != 0 || curY != 0)
        {
            if (curX == 0)
            {
                ret.Add(new DiffPart(Operation.Insert, 0, curY - 1, 1));
                curY--;
                continue;
            }

            if (curY == 0)
            {
                ret.Add(new DiffPart(Operation.Delete, curX - 1, 0, 1));
                curX--;
                continue;
            }

            uint leftVal = matrix[curX - 1, curY];
            uint upVal = matrix[curX, curY - 1];
            if (a[curX - 1] == b[curY - 1] && 
                leftVal > matrix[curX - 1, curY - 1] &&
                upVal > matrix[curX - 1, curY - 1])
            {
                ret.Add(new DiffPart(Operation.Keep, curX - 1, curY - 1, 1));
                curX--;
                curY--;
            }
            // go up or left
            else if (leftVal < upVal)
            {
                ret.Add(new DiffPart(Operation.Delete, curX - 1, curY - 1, 1));
                curX--;
            }
            else
            {
                ret.Add(new DiffPart(Operation.Insert, curX - 1, curY - 1, 1));
                curY--;
            }
        }

        //Debug.Assert(maxSize - keeps == ret.Count);
        ret.Reverse();
        return ret;
    }

    private void Print(uint[,] matrix, int width, int height)
    {
        string? env = Environment.GetEnvironmentVariable("OUTPUT_VERBOSITY");
        if (env == null || int.Parse(env) <= 2) return;
        Console.WriteLine("[");
        for (int y = 0; y < height; y++)
        {
            string line = "  ";
            for (int x = 0; x < width; x++)
            {
                if (x != 0) line += ", ";
                line += matrix[x, y];
            }

            Console.WriteLine(line);
        }
        Console.WriteLine("]");
    }
}
