namespace Voxta.Abstractions.System;

public interface ITimeProvider
{
    DateTimeOffset LocalNow { get; }
    DateTimeOffset UtcNow { get; }
}

public class TimeProvider : ITimeProvider
{
    public static readonly ITimeProvider Current = new TimeProvider();
    
    public DateTimeOffset LocalNow => DateTimeOffset.Now;
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
