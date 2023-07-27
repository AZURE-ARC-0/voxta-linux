using System.Diagnostics;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;

namespace Voxta.Core;

public class ChatSessionState
{
    public ChatSessionStates State = ChatSessionStates.Live;
    public TextData? PendingUserMessage;
    
    private readonly Stopwatch _audioPlaybackStopwatch = new();
    private double _audioPlaybackDuration;
    private IPerformanceMetricsTracker? _perfTracker;
    
    private CancellationTokenSource? _generateReplyAbort;
    private TaskCompletionSource<bool>? _generateReplyTaskCompletionSource;

    public CancellationToken GenerateReplyBegin(IPerformanceMetricsTracker perfTracker)
    {
        _perfTracker = perfTracker;
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
        _perfTracker?.Done();
        _perfTracker = null;
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
        _perfTracker = null;
        return ratio;
    }
}

public enum ChatSessionStates
{
    Live,
    Paused,
    Diagnostics,
}