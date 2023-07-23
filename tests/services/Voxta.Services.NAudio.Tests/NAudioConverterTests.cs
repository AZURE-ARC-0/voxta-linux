using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Moq;

namespace Voxta.Services.NAudio.Tests;

public class NAudioConverterTests
{
    private NAudioAudioConverter _converter = null!;

    [SetUp]
    public void Setup()
    {
        var performanceMetrics = Mock.Of<IPerformanceMetrics>();
        _converter = new NAudioAudioConverter(performanceMetrics);
    }

    [Test, Explicit]
    public async Task ConvertMpegToWav()
    {
        await using var source = File.OpenRead(@"source.mp3");
        _converter.SelectOutputContentType(new[] { "audio/x-wav" }, "audio/mpeg");
        var result = await _converter.ConvertAudioAsync(new AudioData(source, "audio/mpeg"), CancellationToken.None);
        await using var dest = File.OpenWrite(@"result.wav");
        await result.Stream.CopyToAsync(dest);
    }
}