using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;

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
        if (_process == null) return;
  
        try
        {
            if (_process is { HasExited: false })
            {
                SendExitSignal();
                _process.WaitForExit(30000);
                _process.Kill();
            }
            _process.Close();
            _process.Dispose();
            _process = null;
        }
        catch
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
    
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AttachConsole(uint dwProcessId);
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    static extern bool FreeConsole();
    [DllImport("kernel32.dll")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? HandlerRoutine, bool Add);
    delegate bool ConsoleCtrlDelegate(uint dwCtrlType);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

    private static void SendExitSignal()
    {
        if (_process == null || _process.HasExited) return;
        if (!AttachConsole((uint)_process.Id)) return;
        SetConsoleCtrlHandler(null, true);
        GenerateConsoleCtrlEvent(0, 0);
        FreeConsole();
        SetConsoleCtrlHandler(null, false);
    }
}