using FFMpegCore; // Add the necessary using statement
using Voxta.Abstractions.Services;

namespace Voxta.Services.FFmpeg
{
    public class FFmpegRecordingService : IRecordingService
    {
        private bool _recording;
        private bool _disposed;
        private FFMpeg ffmpeg; // Make ffmpeg a class-level field
        private Task _recordingTask; // Declare the recording task

        public bool Speaking { get; set; }
        public event EventHandler<RecordingDataEventArgs>? DataAvailable;

        public FFmpegRecordingService()
        {
            // When recording, invoke event: DataAvailable?.Invoke(s, new RecordingDataEventArgs(e.Buffer, e.BytesRecorded)):
            DataAvailable?.Invoke(this, new RecordingDataEventArgs(Array.Empty<byte>(), 0));
        }

        public void StartRecording()
        {
            // Check if already recording or disposed
            if (_recording || _disposed)
            {
                throw new InvalidOperationException("Already recording or disposed.");
            }

            // Set recording flag
            _recording = true;

            // Initialize ffmpeg
            ffmpeg = new FFMpeg();

            // Define input and output options (adjust as needed)
            var inputOptions = new ConversionOptions
            {
                // Input options for capturing audio from the microphone
            };

            var outputOptions = new ConversionOptions
            {
                // Output options for the desired audio format
            };

            // Start recording and handle the audio data
            ffmpeg.ConvertMedia("input_device", "output_format", inputOptions, outputOptions)
                .DataReceived += (sender, args) =>
                {
                    // Trigger the DataAvailable event with the recorded data
                    DataAvailable?.Invoke(this, new RecordingDataEventArgs(args.Buffer, args.Buffer.Length));
                };

            // Optionally, start a background task to manage the recording process
            _recordingTask = Task.Run(() => ffmpeg.Start());
        }

        public void StopRecording()
        {
            // Check if already stopped or disposed
            if (!_recording || _disposed)
            {
                return; // Already stopped or disposed
            }

            // Set flags to stop recording
            Speaking = false;
            _recording = false;

            // Stop the FFmpeg recording process
            ffmpeg.Stop();

            // Optionally, wait for the background recording task to complete
            _recordingTask?.Wait();
            _recordingTask = null;
        }

        public void Dispose()
        {
            _disposed = true;
            StopRecording();
            // Dispose any FFmpeg references
        }
    }
}