namespace Voxta.Abstractions.Model;

public class ChatSessionDataReadToken : ChatSessionDataToken<IReadOnlyList<ChatMessageData>, IReadOnlyList<ChatSessionDataMemory>>
{
    public ChatSessionDataReadToken(IChatSessionDataUnsafe data, Action exit)
        : base(data, exit)
    {
    }
}

public class ChatSessionDataWriteToken : ChatSessionDataToken<List<ChatMessageData>, List<ChatSessionDataMemory>>
{
    public ChatSessionDataWriteToken(IChatSessionDataUnsafe data, Action exit)
        : base(data, exit)
    {
    }
}

public class ChatSessionDataToken<TMessages, TMemory> : IDisposable
    where TMessages : class, IReadOnlyList<ChatMessageData>
    where TMemory : class, IReadOnlyList<ChatSessionDataMemory>
{
    private readonly Action _exit;

    public IChatSessionData Data { get; }
    public TMessages Messages { get; }
    public TMemory Memories { get; }

    protected ChatSessionDataToken(IChatSessionDataUnsafe data, Action exit)
    {
        _exit = exit;
        Data = data;
        Messages = data.Messages as TMessages ?? throw new InvalidCastException();
        Memories = data.Memories as TMemory ?? throw new InvalidCastException();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _exit();
    }
}
