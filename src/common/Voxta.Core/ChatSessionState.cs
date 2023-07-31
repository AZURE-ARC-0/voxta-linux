using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;

namespace Voxta.Core;

public class ChatSessionState
{
    private readonly ITimeProvider _timeProvider;
    public ChatSessionStates State = ChatSessionStates.Live;
    public TextData? PendingUserMessage;
    
    private DateTimeOffset _audioPlaybackStart;
    private double _audioPlaybackDuration;
    
    private IPerformanceMetricsTracker? _perfTracker;
    
    private CancellationTokenSource? _generateReplyAbort;
    private TaskCompletionSource<bool>? _generateReplyTaskCompletionSource;

    public ChatSessionState(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

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
        _audioPlaybackStart = _timeProvider.UtcNow;
        _audioPlaybackDuration = duration;
        _perfTracker?.Done();
        _perfTracker = null;
    }
    
    public void StopSpeechAudio()
    {
        _audioPlaybackStart = default;
        _audioPlaybackDuration = 0;
    }

    public double InterruptSpeech()
    {
        if (_audioPlaybackDuration == 0) return 0;
        var now = _timeProvider.UtcNow;
        var duration = now - _audioPlaybackStart;
        var ratio = duration.TotalSeconds / _audioPlaybackDuration;
        _audioPlaybackStart = default;
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