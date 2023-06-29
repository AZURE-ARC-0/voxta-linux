namespace ChatMate.Core;

public class LocalInputEventDispatcher
{
    public event EventHandler? SpeechRecognitionStart;
    public event EventHandler<string>? SpeechRecognitionEnd;
    public event EventHandler? PauseSpeechRecognition;
    public event EventHandler? ReadyForSpeechRecognition;

    public virtual void OnSpeechRecognitionStart()
    {
        SpeechRecognitionStart?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnSpeechRecognitionEnd(string text)
    {
        SpeechRecognitionEnd?.Invoke(this, text);
    }

    public virtual void OnPauseSpeechRecognition()
    {
        PauseSpeechRecognition?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnReadyForSpeechRecognition()
    {
        ReadyForSpeechRecognition?.Invoke(this, EventArgs.Empty);
    }
}
