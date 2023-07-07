using System.Diagnostics;
using System.Text;

namespace ChatMate.Core;

public class ChatSessionState
{
    public Stopwatch _audioPlaybackStopwatch = new();
    public double _audioPlaybackDuration;
    
    public readonly StringBuilder PendingUserMessage = new();
    
    private CancellationTokenSource? _generateReplyAbort;
    private TaskCompletionSource<bool>? _generateReplyTaskCompletionSource;

    public CancellationToken GenerateReplyBegin()
    {
        _generateReplyTaskCompletionSource = new TaskCompletionSource<bool>();
        var cts = new CancellationTokenSource();
        _generateReplyAbort = cts;
        return cts.Token;
    }

    public void GenerateReplyEnd()
    {
        _generateReplyTaskCompletionSource?.SetResult(true);
        _generateReplyAbort?.Dispose();
        _generateReplyAbort = null;
    }

    public async ValueTask AbortGeneratingReplyAsync()
    {
        try
        {
            try
            {
                _generateReplyAbort?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            
            if (_generateReplyTaskCompletionSource != null)
                await _generateReplyTaskCompletionSource.Task;
        }
        finally
        {
            _generateReplyTaskCompletionSource = null;
            _generateReplyAbort = null;
        }
    }

    public void StartSpeechAudio(double duration)
    {
        _audioPlaybackStopwatch.Restart();
        _audioPlaybackDuration = duration;
    }
    
    public void StopSpeechAudio()
    {
        _audioPlaybackStopwatch.Stop();
        _audioPlaybackDuration = 0;
    }

    public double InterruptSpeech()
    {
        if (!_audioPlaybackStopwatch.IsRunning || _audioPlaybackDuration == 0) return 0;
        _audioPlaybackStopwatch.Stop();
        var ratio = _audioPlaybackStopwatch.Elapsed.TotalSeconds / _audioPlaybackDuration;
        _audioPlaybackDuration = 0;
        return ratio;
    }
}