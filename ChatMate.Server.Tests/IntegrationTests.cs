using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

[TestFixture]
public class ServerIntegrationTest
{
    private Task _serverTask;
    private CancellationTokenSource _serverCts;
    private TcpClient _tcpClient = null!;
    private NetworkStream _tcpStream = null!;

    [SetUp]
    public void Setup()
    {
        _serverCts = new CancellationTokenSource();
        _serverTask = Program.Start(new string[0], _serverCts.Token);
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

        var receivedMessage = ReceiveJson().GetAwaiter().GetResult();

        Assert.That(receivedMessage.Type, Is.EqualTo("success"));
        Assert.That(receivedMessage.Content, Is.EqualTo("Connection established"));
    }

    [TearDown]
    public void Teardown()
    {
        try
        {
            var disconnectMessage = new Message { Type = "disconnect", Content = "" };
            SendJson(disconnectMessage).Wait();
        }
        catch { }
        _tcpClient.Dispose();
        _serverCts.Cancel();
        _serverTask.Wait();
    }

    [Test]
    public async Task TestChatMessage()
    {
        await SendJson(new Message { Type = "chat", Content = "Hello World!" });

        Assert.That((await ReceiveJson()).Type, Is.EqualTo("waiting"));

        Assert.That((await ReceiveJson()).Type, Is.EqualTo("message"));
    }

    private async Task<Message> ReceiveJson()
    {
        var buffer = new byte[1024];
        int bytesRead = await _tcpStream.ReadAsync(buffer);
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
