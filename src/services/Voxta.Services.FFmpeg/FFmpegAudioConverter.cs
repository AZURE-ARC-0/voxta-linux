using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Services.FFmpeg;

public class FFmpegAudioConverter : IAudioConverter
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

    public FFmpegAudioConverter(IPerformanceMetrics performanceMetrics)
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

    public Task<AudioData> ConvertAudioAsync(AudioData input, CancellationToken cancellationToken)
    {
        if (_outputContentType == null)
            throw new NullReferenceException("Call SelectOutputContentType() first.");
        if (_outputContentType == input.ContentType)
            return Task.FromResult(input);
        
        var audioConvPerf = _performanceMetrics.Start("FFmpeg.AudioConversion");

        // Convert from input.ContentType to _outputContentType

        audioConvPerf.Done();
        
        // Remove this line once implemented
        throw new NotImplementedException();
    }
}