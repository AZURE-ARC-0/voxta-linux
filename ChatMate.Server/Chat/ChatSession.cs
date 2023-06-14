using System.Net.WebSockets;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChatMate.Server;

public class ChatSessionFactory
{
    private readonly ITextGenService _textGen;
    private readonly ITextToSpeechService _speechGen;
    private readonly IAnimationSelectionService _animSelect;
    private readonly ILogger<ChatSession> _logger;

    public ChatSessionFactory(ITextGenService textGen, ITextToSpeechService speechGen, IAnimationSelectionService animSelect, ILogger<ChatSession> logger)
    {
        _textGen = textGen;
        _speechGen = speechGen;
        _animSelect = animSelect;
        _logger = logger;
    }
    
    public ChatSession Create(WebSocket webSocket)
    {
        return new ChatSession(webSocket, _textGen, _speechGen, _animSelect, _logger);
    }
}

public class ChatSession
{
    private static readonly Regex SanitizeMessage = new(@"[^a-zA-Z0-9 '""\-\.\!\?\,\;]", RegexOptions.Compiled);
    
    private readonly WebSocket _webSocket;
    private readonly ITextGenService _textGen;
    private readonly ITextToSpeechService _speechGen;
    private readonly IAnimationSelectionService _animSelect;
    private readonly ILogger<ChatSession> _logger;
    private readonly SemaphoreSlim _sendLock = new(1);
    
    // TODO: Use a real chat data store, reload using auth
    private readonly ChatData _chatData = new ChatData();

    public ChatSession(WebSocket webSocket, ITextGenService textGen, ITextToSpeechService speechGen, IAnimationSelectionService animSelect, ILogger<ChatSession> logger)
    {
        _webSocket = webSocket;
        _textGen = textGen;
        _speechGen = speechGen;
        _animSelect = animSelect;
        _logger = logger;
    }
    
    public async Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];

        while (!_webSocket.CloseStatus.HasValue)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.CloseStatus.HasValue) return;

            var clientMessage = JsonSerializer.Deserialize<Message>(buffer.AsMemory(0, result.Count).Span);
            if (clientMessage == null)
            {
                continue;
            }

            if (clientMessage.Type == "Send")
            {
                _logger.LogInformation("Received chat message: {Text}", clientMessage.Content);
                _chatData.Messages.Add(new ChatMessageData
                {
                    User = _chatData.UserName,
                    Text = clientMessage.Content,
                });

                var gen = await _textGen.GenerateReplyAsync(_chatData);
                _logger.LogInformation("Reply: {Text}", gen);
                gen = SanitizeMessage.Replace(gen, "");
                _chatData.Messages.Add(new ChatMessageData
                {
                    User = _chatData.BotName,
                    Text = clientMessage.Content,
                });
                await SendAsync(new Message { Type = "Reply", Content = gen }, cancellationToken);

                await Task.WhenAll(
                    GenerateSpeechAsync(cancellationToken, gen),
                    SelectAnimationAsync(cancellationToken)
                );
            }
        }
    }

    private async Task GenerateSpeechAsync(CancellationToken cancellationToken, string gen)
    {
        var speechUrl = await _speechGen.GenerateSpeechUrlAsync(gen);
        _logger.LogInformation("Generated speech URL: {SpeechUrl}", speechUrl);
        await SendAsync(new Message { Type = "Speech", Content = speechUrl }, cancellationToken);
    }
    
    private async Task SelectAnimationAsync(CancellationToken cancellationToken)
    {
        var animation = await _animSelect.SelectAnimationAsync(_chatData);
        _logger.LogInformation("Selected animation: {Animation}", animation);
        await SendAsync(new Message { Type = "Animation", Content = animation }, cancellationToken);
    }

    private async Task SendAsync(Message message, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _webSocket.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes(message),
                WebSocketMessageType.Text,
                true,
                cancellationToken
            );
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
