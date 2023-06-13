using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace ChatMate.Server.Tests;

[TestFixture]
public class ServerIntegrationTest
{
    private static readonly HttpClient HttpClient = new();
    
    private Task _serverTask = null!;
    private CancellationTokenSource _serverCts = null!;
    private TcpClient _tcpClient = null!;
    private NetworkStream _tcpStream = null!;

    [SetUp]
    public void Setup()
    {
        _serverCts = new CancellationTokenSource();
        _serverTask = Program.Start(Array.Empty<string>(), _serverCts.Token);
        _tcpClient = new TcpClient();
        const int maxAttempts = 10;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                _tcpClient.Connect("localhost", 5384);
                _tcpStream = _tcpClient.GetStream();
                break; // Server has started, break the loop
            }
            catch (SocketException)
            {
                if (attempt == maxAttempts - 1)
                    throw;

                Task.Delay(100).Wait();
            }
        }
    }

    [TearDown]
    public void Teardown()
    {
        _tcpClient.Close();
        _tcpClient.Dispose();
        _serverCts.Cancel();
        try
        {
            if (_serverTask is { IsCanceled: false, IsFaulted: false })
                _serverTask.Wait();
        }
        catch (AggregateException)
        {
            // Ignore
        }
    }

    [Test]
    public async Task TestChatMessage()
    {
        await SendJson(new Message { Type = "Send", Content = "Hello World!" });

        var reply = await ReceiveJson();
        Assert.Multiple(() =>
        {
            Assert.That(reply.Type, Is.EqualTo("Reply"));
            Assert.That(reply.Content, Is.Not.Null.Or.Empty);
        });

        var speech = await ReceiveJson();
        Assert.Multiple(() =>
        {
            Assert.That(speech.Type, Is.EqualTo("Speech"));
            Assert.That(speech.Content, Does.StartWith("http"));
        });

        var response = await HttpClient.GetAsync(speech.Content);
        if (!response.IsSuccessStatusCode)
            Assert.Fail($"GET {speech.Content}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}");
        
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), speech.Content);
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("audio/mpeg"), speech.Content);
            Assert.That(response.Content.Headers.ContentLength, Is.GreaterThan(1000), speech.Content);
        });
    }

    private async Task<Message> ReceiveJson()
    {
        var buffer = new byte[1024];
        var bytesRead = await _tcpStream.ReadAsync(buffer);
        var receivedMessage = JsonSerializer.Deserialize<Message>(buffer.AsMemory(0, bytesRead).Span);
        if (receivedMessage == null) throw new NullReferenceException("receivedMessage is null");
        return receivedMessage;
    }

    private async Task SendJson(Message message)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message);
        await _tcpStream.WriteAsync(json.AsMemory(), CancellationToken.None);
    }
}
