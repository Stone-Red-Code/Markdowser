namespace Markdowser.Processing;

public readonly struct ProcessingProgress(long current, long total, string? message = null, bool isBytes = false)
{
    public long Current { get; } = current;
    public long Total { get; } = total;
    public string? Message { get; } = message;

    public bool IsBytes { get; } = isBytes;

    public int Percentage => (int)((double)Current / Total * 100);
}