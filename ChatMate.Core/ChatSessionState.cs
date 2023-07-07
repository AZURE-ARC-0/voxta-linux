namespace ChatMate.Core;

public class ChatSessionState
{
    private bool _speaking;
    private CancellationTokenSource? _replyAbort;

    public async Task<CancellationToken> BeginGeneratingReply()
    {
        await AbortReplyAsync();
        var cts = new CancellationTokenSource();
        _replyAbort = cts;
        return cts.Token;
    }

    public ValueTask<bool> AbortReplyAsync()
    {
#warning This is a mess. Clean up.
        if (_replyAbort == null)
        {
            if (_speaking)
            {
                _speaking = false;
                return ValueTask.FromResult(true);
            }
            return ValueTask.FromResult(false);
        }
        
        try
        {
            _replyAbort?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        
#warning Wait for the abort task
        _replyAbort = null;
        _speaking = false;
        return ValueTask.FromResult(false);
    }
    
    public void SpeechStart()
    {
        _speaking = true;
    }

    public void SpeechComplete()
    {
        _speaking = false;
    }

    public void SpeechGenerationComplete()
    {
        _replyAbort?.Dispose();
        _replyAbort = null;
    }
}