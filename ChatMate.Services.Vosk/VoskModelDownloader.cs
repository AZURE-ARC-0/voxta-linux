using System.IO.Compression;
using System.Security;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatMate.Services.Vosk;

public interface IVoskModelDownloader
{
    Task<global::Vosk.Model> AcquireModelAsync();
}

public class VoskModelDownloader : IVoskModelDownloader
{
    private readonly ILogger<VoskModelDownloader> _logger;
    private readonly IOptions<VoskOptions> _options;

    public VoskModelDownloader(ILogger<VoskModelDownloader> logger, IOptions<VoskOptions> options)
    {
        _logger = logger;
        _options = options;
    }
    
    public async Task<global::Vosk.Model> AcquireModelAsync()
    {
        var modelsPath = Path.GetFullPath("Models/Vosk");
        var modelName = _options.Value.Model;
        var modelZipHash = _options.Value.ModelZipHash;
        var modelPath = Path.Combine(modelsPath, modelName);
        
        if (Directory.Exists(modelPath))
        {
            _logger.LogInformation("Vosk model already downloaded");
            return new global::Vosk.Model(modelPath);
        }
        
        var fileUrl = $"https://alphacephei.com/vosk/models/{modelName}.zip";

        _logger.LogInformation("Downloading Vosk model from {FileUrl}...", fileUrl);
        using var httpClient = new HttpClient();
        var fileBytes = await httpClient.GetByteArrayAsync(fileUrl);
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