using System.IO.Compression;
using System.Security;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Voxta.Services.Vosk;

public interface IVoskModelDownloader
{
    Task<global::Vosk.Model> AcquireModelAsync(string model, string? modelZipHash, CancellationToken cancellationToken1);
}

public class VoskModelDownloader : IVoskModelDownloader
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    
    private readonly ILogger<VoskModelDownloader> _logger;

    public VoskModelDownloader(ILogger<VoskModelDownloader> logger)
    {
        _logger = logger;
    }
    
    public async Task<global::Vosk.Model> AcquireModelAsync(string model, string? modelZipHash, CancellationToken cancellationToken)
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            return await AcquireModelInternalAsync(model, modelZipHash, cancellationToken);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task<global::Vosk.Model> AcquireModelInternalAsync(string model, string? modelZipHash, CancellationToken cancellationToken)
    {
        var modelsPath = Path.GetFullPath("Models/Vosk");
        var modelPath = Path.Combine(modelsPath, model);
        
        if (Directory.Exists(modelPath))
        {
            _logger.LogDebug("Vosk model already downloaded");
            return new global::Vosk.Model(modelPath);
        }
        
        var fileUrl = $"https://alphacephei.com/vosk/models/{model}.zip";

        _logger.LogInformation("Downloading Vosk model from {FileUrl}...", fileUrl);
        using var httpClient = new HttpClient();
        var fileBytes = await httpClient.GetByteArrayAsync(fileUrl, cancellationToken);
        var hashBytes = SHA256.HashData(fileBytes);
        var actualZipHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        _logger.LogInformation("Downloaded Vosk model, hash is {ActualZipHash}", actualZipHash);
        if (!string.IsNullOrEmpty(modelZipHash) && actualZipHash != modelZipHash)
            throw new SecurityException($"Expected vosk model to have hash '{modelZipHash}' but hash was '{actualZipHash}'.");
        Directory.CreateDirectory("Models/Vosk");
        _logger.LogInformation("Extracting Vosk model...");
        using var stream = new MemoryStream(fileBytes);
        using var archive = new ZipArchive(stream);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\')) continue;
            var entryExtractPath = Path.Combine(modelsPath, entry.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(entryExtractPath)!);
            entry.ExtractToFile(entryExtractPath, overwrite: true);
        }
        _logger.LogInformation("Extracted Vosk model");
        if (!Directory.Exists(modelPath))
            throw new Exception("Vosk model directory does not exist after extracting");
        return new global::Vosk.Model(modelPath);
    }
}