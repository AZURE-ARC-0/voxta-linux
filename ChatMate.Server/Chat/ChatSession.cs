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
    private readonly WebSocket _webSocket;
    private readonly ITextGenService _textGen;
    private readonly ITextToSpeechService _speechGen;
    private readonly IAnimationSelectionService _animSelect;
    private readonly ILogger<ChatSession> _logger;
    private readonly SemaphoreSlim _sendLock = new(1);
    
    private ChatData? _chatData;

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
        // TODO: Use a real chat data store, reload using auth
        _chatData = new ChatData();
        _chatData.PreambleTokens = _textGen.GetTokenCount(_chatData.Preamble);
        
        var buffer = new byte[1024 * 4];
        
        // TODO: Send available bots list

        while (!_webSocket.CloseStatus.HasValue)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.CloseStatus.HasValue) return;

            var clientMessage = JsonSerializer.Deserialize<Message>(buffer.AsMemory(0, result.Count).Span);
            if (clientMessage == null)
            {
                continue;
            }
            
            // TODO: Select a bot from the provided bots list and a conversation ID to load the  chat

            if (clientMessage.Type == "Send")
            {
                _logger.LogInformation("Received chat message: {Text}", clientMessage.Content);
                // TODO: Save into some storage
                _chatData.Messages.Add(new ChatMessageData
                {
                    User = _chatData.UserName,
                    Text = clientMessage.Content,
                });

                var reply = await _textGen.GenerateReplyAsync(_chatData);
                _logger.LogInformation("Reply ({Tokens} tokens): {Text}", reply.Tokens, reply.Text);
                // TODO: Save into some storage
                _chatData.Messages.Add(reply);
                await SendAsync(new Message { Type = "Reply", Content = reply.Text }, cancellationToken);

                // TODO: Return this directly in the Reply instead
                var speechUrl = await _speechGen.GenerateSpeechUrlAsync(reply.Text);
                _logger.LogInformation("Generated speech URL: {SpeechUrl}", speechUrl);
                await SendAsync(new Message { Type = "Speech", Content = speechUrl }, cancellationToken);

                var animation = await _animSelect.SelectAnimationAsync(_chatData);
                _logger.LogInformation("Selected animation: {Animation}", animation);
                await SendAsync(new Message { Type = "Animation", Content = animation }, cancellationToken);
            }
        }
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
