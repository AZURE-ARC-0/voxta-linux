namespace ChatMate.Core;

public class ExclusiveLocalInputManager
{
    private readonly object _lock = new();
    private ExclusiveLocalInputHandle?  _handle;
    
    public event EventHandler? PauseSpeechRecognitionRequested;
    public event EventHandler? ResumeSpeechRecognitionRequested;
    
    public ExclusiveLocalInputHandle Acquire()
    {
        lock (_lock)
        {
            _handle?.Dispose();
            _handle = new ExclusiveLocalInputHandle(this);
            return _handle;
        }    
    }
    
    public void OnSpeechRecognitionStarted()
    {
        _handle?.OnSpeechRecognitionStarted();
    }

    public void OnSpeechRecognitionFinished(string text)
    {
        _handle?.OnSpeechRecognitionFinished(text);
    }

    public void RequestPauseSpeechRecognition()
    {
        PauseSpeechRecognitionRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RequestResumeSpeechRecognition()
    {
        ResumeSpeechRecognitionRequested?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class ExclusiveLocalInputHandle : IDisposable
{
    private readonly ExclusiveLocalInputManager _owner;

    public ExclusiveLocalInputHandle(ExclusiveLocalInputManager owner)
    {
        _owner = owner;
    }

    public event EventHandler? SpeechRecognitionStarted;
    public event EventHandler<string>? SpeechRecognitionFinished;
    
    public void RequestPauseSpeechRecognition()
    {
        _owner.RequestPauseSpeechRecognition();
    }

    public void RequestResumeSpeechRecognition()
    {
        _owner.RequestResumeSpeechRecognition();
    }

    public void OnSpeechRecognitionStarted()
    {
        SpeechRecognitionStarted?.Invoke(this, EventArgs.Empty);
    }

    public void OnSpeechRecognitionFinished(string text)
    {
        SpeechRecognitionFinished?.Invoke(this, text);
    }

    public void Dispose()
    {
        SpeechRecognitionStarted = null;
        SpeechRecognitionFinished = null;
    }
}
