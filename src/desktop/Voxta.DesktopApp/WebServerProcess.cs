using System.Diagnostics;
using System.IO;

namespace Voxta.DesktopApp;

public static class WebServerProcess
{
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
        if (_process is { HasExited: false })
            try
            {
                _process.CloseMainWindow();
            }
            catch
            {
                /* ignored */
            }

        if (_process is { HasExited: false })
            try
            {
                _process.WaitForExit(1000);
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
}