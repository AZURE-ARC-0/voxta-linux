namespace ChatMate.Core;

public interface IMessageProcessing
{
    ValueTask HandleAsync(CancellationToken cancellationToken);
}