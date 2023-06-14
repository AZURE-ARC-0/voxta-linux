using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using ChatMate.Server;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WebSocketTests
{
    [TestFixture]
    public class WebSocketTest
    {
        private WebSocketClient _wsClient = null!;
        private WebSocket _wsConnection;
        private TestServer _server;
        private HttpClient _httpClient;

        [SetUp]
        public async Task SetUp()
        {
            var webDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "ChatMate.Server");
            var builder = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseContentRoot(webDir);
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("appsettings.Local.json", optional: false, reloadOnChange: true);
            });
            _server = new TestServer(builder);
            _httpClient = _server.CreateClient();
            _wsClient = _server.CreateWebSocketClient();
            var wsUri = new UriBuilder(_server.BaseAddress)
            {
                Scheme = "ws",
                Path = "/ws"
            }.Uri;
            _wsConnection = await _wsClient.ConnectAsync(wsUri, CancellationToken.None);
        }

        [TearDown]
        public async Task TearDown()
        {
            try
            {
                await _wsConnection.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch (Exception)
            {
                // ignored
            }
            _server.Dispose();
        }

        [Test]
        public async Task SendMessageAndGetReply()
        {
            await _wsConnection.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes(new Message {Type = "Send", Content = "Hello, world!"}),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            var buffer = new byte[1024];
            var result = await _wsConnection.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.That(result.CloseStatus, Is.Null, result.CloseStatusDescription);
            var reply = JsonSerializer.Deserialize<Message>(buffer.AsMemory(0, result.Count).Span);
            Assert.Multiple(() =>
            {
                Assert.That(reply.Type, Is.EqualTo("Reply"));
                Assert.That(reply.Content, Is.Not.Null.Or.Empty);
            });

            result = await _wsConnection.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.That(result.CloseStatus, Is.Null, result.CloseStatusDescription);
            var speech = JsonSerializer.Deserialize<Message>(buffer.AsMemory(0, result.Count).Span);
            Assert.Multiple(() =>
            {
                Assert.That(speech.Type, Is.EqualTo("Speech"));
                Assert.That(speech.Content, Does.StartWith("/speech"));
            });

            var response = await _httpClient.GetAsync(new Uri(_server.BaseAddress, speech.Content));
            if (!response.IsSuccessStatusCode)
                Assert.Fail($"GET {speech.Content}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}");
        
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), speech.Content);
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("audio/mpeg"), speech.Content);
                Assert.That(response.Content.Headers.ContentLength, Is.GreaterThan(1000), speech.Content);
            });
        }
    }
}