using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Services;
using NAudio.Wave;

namespace ChatMate.Services.NAudio;

public class NAudioAudioConverter : IAudioConverter
{
    private static readonly string[] SupportedContentTypes =
    {
        "audio/mpeg",
        "audio/wav",
        "audio/x-wav",
    };
    
    private readonly IPerformanceMetrics _performanceMetrics;
    private string? _contentType;

    public string ContentType => _contentType ?? throw new NullReferenceException("Call SelectContentType first.");

    public NAudioAudioConverter(IPerformanceMetrics performanceMetrics)
    {
        _performanceMetrics = performanceMetrics;
    }

    public void SelectContentType(string[] acceptedContentTypes, string generatedContentType)
    {
        if (acceptedContentTypes.Contains(generatedContentType))
        {
            _contentType = generatedContentType;
            return;
        }
        // Find common values between acceptedContentType and SupportedContentTypes
        var common = acceptedContentTypes.Intersect(SupportedContentTypes).ToArray();
        if (common.Length == 0)
            throw new InvalidOperationException($"No common content type found between accepted {string.Join(", ", acceptedContentTypes)} and supported {string.Join(", ", SupportedContentTypes)}.");
        _contentType = common.Contains(generatedContentType) ? generatedContentType : common[0];
    }

    public async Task<AudioData> ConvertAudioAsync(AudioData input, CancellationToken cancellationToken)
    {
        if (_contentType == input.ContentType)
            return input;
        if (_contentType == null)
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
                switch (_contentType)
                {
                    case "audio/mpeg":
                        // ReSharper disable once RedundantArgumentDefaultValue
                        MediaFoundationEncoder.EncodeToMp3(reader, ms, 192_000);
                        ms.Seek(0, SeekOrigin.Begin);
                        return new AudioData(ms, _contentType);
                    case "audio/wav":
                    case "audio/x-wav":
                        WaveFileWriter.WriteWavFileToStream(ms, reader);
                        ms.Seek(0, SeekOrigin.Begin);
                        return new AudioData(ms, _contentType);
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

        if(input.ContentType == "audio/mpeg" && _contentType is "audio/wav" or "audio/x-wav")
        {
            await using var mp3Reader = new Mp3FileReader(input.Stream);
            using var ms = new MemoryStream();
            await using var wavWriter = new WaveFileWriter(ms, mp3Reader.WaveFormat);
            await mp3Reader.CopyToAsync(wavWriter, cancellationToken);
            ms.Seek(0, SeekOrigin.Begin);
            return new AudioData(ms, _contentType);
        }

        throw new NotSupportedException($"Input {input.ContentType} and output {_contentType} pair is not supported");
    }
}