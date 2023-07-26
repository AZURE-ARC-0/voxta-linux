using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
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

        Icon = new Icon(GetType(), "Resources.voxta.ico");

        FormClosing += MainForm_FormClosing;

        ConsoleControl = new ConsoleControl.ConsoleControl();
        ConsoleControl.Dock = DockStyle.Fill;
        ConsoleControl.IsInputEnabled = false;
        Controls.Add(ConsoleControl);

        // Initialize WebView2
        WebView = new WebView2
        {
            Dock = DockStyle.Fill,
            Visible = false,
        };
        Controls.Add(WebView);

        // Start the web server process
        
        var webServerPath = "Voxta.Server.exe";
        var webServerWorkingDirectoryPath = "";
        #if(DEBUG)
        if (!File.Exists(webServerPath))
        {
            webServerWorkingDirectoryPath = Path.GetFullPath(@"..\..\..\..\..\server\Voxta.Server");
            webServerPath = Path.GetFullPath(@"..\..\..\..\..\server\Voxta.Server\bin\Debug\net7.0\win-x64\Voxta.Server.exe");
        }
        #endif
        if (!File.Exists(webServerPath)) throw new FileNotFoundException(webServerPath);
        var processStartInfo = new ProcessStartInfo(webServerPath)
        {
            WorkingDirectory = webServerWorkingDirectoryPath
        };
        ConsoleControl.StartProcess(processStartInfo);
        var process = ConsoleControl.ProcessInterface.Process;
        if (process != null)
            process.Exited += WebServer_Exited;

        KeyPreview = true;
        KeyUp += MainForm_KeyUp;

        #pragma warning disable CS4014
        InitializeAsync();
        #pragma warning restore CS4014
    }

    private async Task InitializeAsync()
    {
        try
        {
            await WebView.EnsureCoreWebView2Async(null);
            WebView.CoreWebView2.WebMessageReceived += WebView_CoreWebView2_WebMessageReceived;
            await WaitForServerReady("http://127.0.0.1:5384/ping");
            SwitchToWebView();
            WebView.CoreWebView2.Navigate("http://127.0.0.1:5384");
        }
        catch (Exception exc)
        {
            // Handle the exception, e.g. show a message box
            MessageBox.Show(exc.ToString() ?? "Undefined exception");
        }
    }

    private void WebView_CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var str = e.TryGetWebMessageAsString();
        var json = JsonSerializer.Deserialize<MyMessage>(str, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        switch (json?.Command)
        {
            case "switchToTerminal":
                Invoke(SwitchToTerminal);
                break;
            case "toggleFullScreen":
                Invoke(ToggleFullScreen);
                break;
        }
    }
    
    [Serializable]
    public class MyMessage
    {
        public string? Command { get; set; }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        var process = ConsoleControl.ProcessInterface.Process;
        ConsoleControl.StopProcess();
        process.WaitForExit();
    }

    private void WebServer_Exited(object? sender, EventArgs e)
    {
        try
        {
            Invoke(SwitchToTerminal);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void MainForm_KeyUp(object? sender, KeyEventArgs e)
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
        else if (e.KeyCode == Keys.F11)
        {
            ToggleFullScreen();
        }
    }

    private void SwitchToWebView()
    {
        WebView.Visible = true;
        ConsoleControl.Visible = false;
        WebView.Focus();
    }

    private void SwitchToTerminal()
    {
        WebView.Visible = false;
        ConsoleControl.Visible = true;
        ConsoleControl.Focus();
    }

    private void ToggleFullScreen()
    {
        if (WindowState == FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.Sizable;
            TopMost = false;
        }
        else
        {
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            TopMost = true;
        }
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