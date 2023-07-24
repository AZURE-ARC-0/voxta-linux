namespace Voxta.Abstractions.Services;

public interface IRecordingService : IDisposable
{
    bool Speaking { get; set; }
    
    event EventHandler<RecordingDataEventArgs>? DataAvailable;
    void StartRecording();
    void StopRecording();
}

public class RecordingDataEventArgs : EventArgs
{
    public RecordingDataEventArgs(byte[] buffer, int bytesRecorded)
    {
        Buffer = buffer;
        BytesRecorded = bytesRecorded;
    }

    public byte[] Buffer { get; init; }
    public int BytesRecorded { get; init; }
}
