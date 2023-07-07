namespace ChatMate.Core;

public class ActionMessageProcessing : IMessageProcessing
{
    private readonly Func<CancellationToken, ValueTask> _action;

    public ActionMessageProcessing(Func<CancellationToken, ValueTask> action)
    {
        _action = action;
    }

    public async ValueTask HandleAsync(CancellationToken cancellationToken)
    {
        await _action(cancellationToken);
    }
}