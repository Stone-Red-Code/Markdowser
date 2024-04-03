namespace Markdowser.Processing;

public readonly struct ProcessingProgress(int current, int total)
{
    public int Current { get; } = current;
    public int Total { get; } = total;

    public double Percentage => (double)Current / Total * 100;
}