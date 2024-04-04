namespace Markdowser.Processing;

public readonly struct ProcessingProgress(long current, long total, string? message = null)
{
    public long Current { get; } = current;
    public long Total { get; } = total;
    public string? Message { get; } = message;

    public int Percentage => (int)((double)Total / Current * 100);
}