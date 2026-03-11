namespace LotoEngine.Core.Utils;

public static class SetOps
{
    public static int[] Intersect(int[] a, int[] b) => a.Intersect(b).OrderBy(x => x).ToArray();
    public static int[] Except(int[] a, int[] b) => a.Except(b).OrderBy(x => x).ToArray();
}

public static class RunsCalculator
{
    public static (int MaxRun, int RunsGe3) Analyze(int[] sorted)
    {
        if (sorted.Length == 0) return (0, 0);
        var maxRun = 1;
        var curr = 1;
        var runsGe3 = 0;
        for (var i = 1; i < sorted.Length; i++)
        {
            if (sorted[i] == sorted[i - 1] + 1)
            {
                curr++;
            }
            else
            {
                if (curr >= 3) runsGe3++;
                maxRun = Math.Max(maxRun, curr);
                curr = 1;
            }
        }
        if (curr >= 3) runsGe3++;
        maxRun = Math.Max(maxRun, curr);
        return (maxRun, runsGe3);
    }
}
