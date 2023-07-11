using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Services;
using NAudio.Utils;
using NAudio.Wave;

namespace ChatMate.Services.NAudio;

public class NAudioAudioConverter : IAudioConverter
{
    private static readonly string[] SupportedOutputContentTypes =
    {
        "audio/mpeg",
        "audio/wav",
        "audio/x-wav",
    };
    
    private readonly IPerformanceMetrics _performanceMetrics;
    private string? _outputContentType;

    public string ContentType => _outputContentType ?? throw new NullReferenceException("Call SelectContentType first.");

    public NAudioAudioConverter(IPerformanceMetrics performanceMetrics)
    {
        _performanceMetrics = performanceMetrics;
    }

    public void SelectOutputContentType(string[] acceptedContentTypes, string sourceContentType)
    {
        if (acceptedContentTypes.Contains(sourceContentType))
        {
            _outputContentType = sourceContentType;
            return;
        }
        // Find common values between acceptedContentType and SupportedContentTypes
        var common = acceptedContentTypes.Intersect(SupportedOutputContentTypes).ToArray();
        if (common.Length == 0)
            throw new InvalidOperationException($"No common content type found between accepted {string.Join(", ", acceptedContentTypes)} and supported {string.Join(", ", SupportedOutputContentTypes)}.");
        _outputContentType = common.Contains(sourceContentType) ? sourceContentType : common[0];
    }

    public async Task<AudioData> ConvertAudioAsync(AudioData input, CancellationToken cancellationToken)
    {
        if (_outputContentType == input.ContentType)
            return input;
        if (_outputContentType == null)
            throw new NullReferenceException("Call SelectContentType first.");
        
        var audioConvPerf = _performanceMetrics.Start("NAudio.AudioConversion");

        if (input.ContentType == "audio/webm")
        {
            var tmp = Path.GetTempFileName();
            await using var f = File.OpenWrite(tmp);
            await input.Stream.CopyToAsync(f, cancellationToken);
            f.Close();
            try
            {
                await using var reader = new MediaFoundationReader(tmp);
                var ms = new MemoryStream();
                switch (_outputContentType)
                {
                    case "audio/mpeg":
                        // ReSharper disable once RedundantArgumentDefaultValue
                        MediaFoundationEncoder.EncodeToMp3(reader, ms, 192_000);
                        ms.Seek(0, SeekOrigin.Begin);
                        return new AudioData(ms, _outputContentType);
                    case "audio/wav":
                    case "audio/x-wav":
                        WaveFileWriter.WriteWavFileToStream(ms, reader);
                        ms.Seek(0, SeekOrigin.Begin);
                        return new AudioData(ms, _outputContentType);
                    default:
                        throw new InvalidOperationException("Unexpected extension {extension}");
                }
            }
            finally
            {
                File.Delete(tmp);
                audioConvPerf.Done();
            }
        }

        if(input.ContentType == "audio/mpeg" && _outputContentType is "audio/wav" or "audio/x-wav")
        {
            // TODO: Add unit tests?
            await using var mp3Reader = new Mp3FileReader(input.Stream);
            var ms = new MemoryStream();
            await using var wavWriter = new WaveFileWriter(new IgnoreDisposeStream(ms), mp3Reader.WaveFormat);
            await mp3Reader.CopyToAsync(wavWriter, cancellationToken);
            ms.Seek(0, SeekOrigin.Begin);
            return new AudioData(ms, _outputContentType);
        }

        throw new NotSupportedException($"Input {input.ContentType} and output {_outputContentType} pair is not supported");
    }
}