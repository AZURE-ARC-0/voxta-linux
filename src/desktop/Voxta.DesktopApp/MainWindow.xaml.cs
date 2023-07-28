using System.ComponentModel;
using System.Windows;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace Voxta.DesktopApp;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        Closing += MainWindow_FormClosing;
        KeyUp += MainWindow_KeyUp;

        ConsoleControl.IsInputEnabled = false;
        ConsoleControl.FocusVisualStyle = null;

        ConsoleControl.StartProcess(WebServerProcess.CreateProcessStartInfo());
        var process = ConsoleControl.ProcessInterface.Process;
        WebServerProcess.Attach(process);
        if (process != null) process.Exited += WebServer_Exited;

        #pragma warning disable CS4014
        InitializeAsync();
        #pragma warning restore CS4014
    }

    private async Task InitializeAsync()
    {
        try
        {
            await Task.Delay(0);
            var env = await CoreWebView2Environment.CreateAsync(null);
            await WebView.EnsureCoreWebView2Async(env);
            WebView.CoreWebView2.WebMessageReceived += WebView_CoreWebView2_WebMessageReceived;
            WebView.Visibility = Visibility.Hidden;
            await WaitForServerReady("http://127.0.0.1:5384/ping");
            WebView.CoreWebView2.Navigate("http://127.0.0.1:5384");
        }
        catch (Exception exc)
        {
            MessageBox.Show(exc.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Dispatcher.Invoke(SwitchToTerminal);
                break;
            case "toggleFullScreen":
                Dispatcher.Invoke(ToggleFullScreen);
                break;
        }
    }
    
    [Serializable]
    public class MyMessage
    {
        public string? Command { get; set; }
    }

    private void MainWindow_FormClosing(object? sender, CancelEventArgs cancelEventArgs)
    {
        WebServerProcess.StopWebServer();
    }

    private void WebServer_Exited(object? sender, EventArgs e)
    {
        try
        {
            Dispatcher.Invoke(SwitchToTerminal);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void MainWindow_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            ShowHelp();
        }
        if (e.Key == Key.F2)
        {
            e.Handled = true;
            if (WebView.Visibility == Visibility.Visible)
            {
                SwitchToTerminal();
            }
            else
            {
                SwitchToWebView();
            }
        }
        else if (e.Key == Key.F11)
        {
            e.Handled = true;
            ToggleFullScreen();
        }
    }
    
    private static void ShowHelp()
    {
        const string messageBoxText = """
            F1 - Show help
            F2 - Switch between terminal and web view
            F11 - Toggle full screen"
            """;
        MessageBox.Show(messageBoxText, "Help", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SwitchToWebView()
    {
        WebView.Visibility = Visibility.Visible;
        ConsoleControl.Visibility = Visibility.Hidden;
        WebView.Focus();
    }

    private void SwitchToTerminal()
    {
        WebView.Visibility = Visibility.Hidden;
        ConsoleControl.Visibility = Visibility.Visible;
        ConsoleControl.Focus();
    }

    private void ToggleFullScreen()
    {
        if (WindowState == WindowState.Maximized && WindowStyle == WindowStyle.None)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
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
        
        MessageBox.Show("Failed to start the server. Inspect the console output for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        SwitchToWebView();
    }
}