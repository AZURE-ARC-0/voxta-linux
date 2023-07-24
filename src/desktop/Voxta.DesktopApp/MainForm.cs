using System.Diagnostics;
using Microsoft.Web.WebView2.WinForms;

namespace Voxta.DesktopApp;

public partial class MainForm : Form
{
    // ReSharper disable MemberCanBePrivate.Global
    protected readonly WebView2 WebView;
    protected readonly ConsoleControl.ConsoleControl ConsoleControl;
    // ReSharper restore MemberCanBePrivate.Global

    public MainForm()
    {
        InitializeComponent();

        FormClosing += MainForm_FormClosing;

        ConsoleControl = new ConsoleControl.ConsoleControl();
        ConsoleControl.Dock = DockStyle.Fill;
        ConsoleControl.IsInputEnabled = false;
        Controls.Add(ConsoleControl);

        // Initialize WebView2
        WebView = new WebView2
        {
            Dock = DockStyle.Fill,
            Visible = false
        };
        Controls.Add(WebView);

        // Start the web server process
        var webServerWorkingDirectoryPath = Path.GetFullPath(@"..\..\..\..\..\server\Voxta.Server");
        var webServerPath = Path.GetFullPath(@"..\..\..\..\..\server\Voxta.Server\bin\Debug\net7.0\win-x64\Voxta.Server.exe");
        if (!File.Exists(webServerPath)) throw new FileNotFoundException(webServerPath);
        var processStartInfo = new ProcessStartInfo(webServerPath);
        processStartInfo.WorkingDirectory = webServerWorkingDirectoryPath;
        ConsoleControl.StartProcess(processStartInfo);
        ConsoleControl.ProcessInterface.Process.Exited += WebServer_Exited;

        KeyPreview = true;
        KeyDown += MainForm_KeyDown;

        InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await WebView.EnsureCoreWebView2Async(null);
            await WaitForServerReady("http://127.0.0.1:5384/ping");
            SwitchToWebView();
            WebView.CoreWebView2.Navigate("http://127.0.0.1:5384/newchat");
        }
        catch (Exception exc)
        {
            // Handle the exception, e.g. show a message box
            MessageBox.Show(exc.ToString() ?? "Undefined exception");
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        ConsoleControl.StopProcess();
        ConsoleControl.ProcessInterface.Process.WaitForExit();
    }

    private void WebServer_Exited(object? sender, EventArgs e)
    {
        Invoke(SwitchToTerminal);
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F2)
        {
            if (WebView.Visible)
            {
                SwitchToTerminal();
            }
            else
            {
                SwitchToWebView();
            }
        }
    }

    private void SwitchToWebView()
    {
        WebView.Visible = true;
        ConsoleControl.Visible = false;
    }

    private void SwitchToTerminal()
    {
        WebView.Visible = false;
        ConsoleControl.Visible = true;
    }

    private static async Task WaitForServerReady(string url, int millisecondsDelay = 500, int maxAttempts = 20)
    {
        var client = new HttpClient();
        var attempts = 0;
        while (attempts < maxAttempts)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Ignore exceptions, server is not ready
            }

            attempts++;
            await Task.Delay(millisecondsDelay);
        }

        throw new InvalidOperationException("Server is not ready");
    }
}