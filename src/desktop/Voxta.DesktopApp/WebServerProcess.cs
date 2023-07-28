using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;

namespace Voxta.DesktopApp;

public static class WebServerProcess
{
    public const string BaseUrl = "http://127.0.0.1:5384";
    
    private static Process? _process;
    
    public static ProcessStartInfo CreateProcessStartInfo()
    {
        var webServerPath = "Voxta.Server.exe";
        var webServerWorkingDirectoryPath = "";
#if(DEBUG)
        if (!File.Exists(webServerPath))
        {
            webServerWorkingDirectoryPath = Path.GetFullPath(@"..\..\..\..\..\..\server\Voxta.Server");
            webServerPath = Path.Combine(webServerWorkingDirectoryPath, @"bin\Debug\net7.0\win-x64\Voxta.Server.exe");
        }
#endif
        if (!File.Exists(webServerPath)) throw new FileNotFoundException(webServerPath);
        var processStartInfo = new ProcessStartInfo(webServerPath)
        {
            WorkingDirectory = webServerWorkingDirectoryPath
        };
        return processStartInfo;
    }

    public static void Attach(Process process)
    {
        if (_process != null)
        {
            StopWebServer();
            _process = process;
            StopWebServer();
            throw new InvalidOperationException("There was already a process attached.");
        }
        _process = process;
    }

    public static void StopWebServer()
    {
        try
        {
            if (_process is { HasExited: false })
                try
                {
                    SendExitSignal();
                }
                catch
                {
                    /* ignored */
                }

            if (_process is { HasExited: false })
                try
                {
                    _process.WaitForExit(10000);
                }
                catch
                {
                    /* ignored */
                }

            if (_process is { HasExited: false })
                try
                {
                    _process.Kill();
                }
                catch
                {
                    /* ignored */
                }

            try
            {
                _process?.Close();
            }
            catch
            {
                /* ignored */
            }

            _process?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            /* ignored */
        }
    }

    public static async Task<bool> WaitForServerReady(string url = "http://127.0.0.1:5384/ping", int millisecondsDelay = 500, int maxAttempts = 20)
    {
        if (_process == null || _process.HasExited) return false;
        
        var client = new HttpClient();
        var attempts = 0;
        while (attempts < maxAttempts)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
                // Ignore exceptions, server is not ready
            }

            attempts++;
            await Task.Delay(millisecondsDelay);
        }

        return false;
    }
    
    private static void SendExitSignal()
    {
        if (_process == null || _process.HasExited) return;
        var client = new HttpClient();
        client.Send(new HttpRequestMessage(HttpMethod.Post, "/admin/shutdown") { Content = JsonContent.Create(new { }) });
    }
}