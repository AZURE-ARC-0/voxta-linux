namespace Voxta.Abstractions.System;

public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}

public class TimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
