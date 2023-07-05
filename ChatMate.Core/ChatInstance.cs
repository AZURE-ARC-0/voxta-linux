using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatInstance : IDisposable
{
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ChatServicesLocator _servicesLocator;
    private readonly ChatSessionData _chatSessionData;
    private readonly ClientStartChatMessage _startChatMessage;
    private readonly ChatTextProcessor _chatTextProcessor;
    private readonly string? _audioPath;
    private readonly bool _useServerSpeechRecognition;
    private readonly ILogger<UserConnection> _logger;

    public ChatInstance(
        IUserConnectionTunnel tunnel,
        ILoggerFactory loggerFactory,
        ChatServicesLocator servicesLocator,
        ChatSessionData chatSessionData,
        ClientStartChatMessage startChatMessage,
        ChatTextProcessor chatTextProcessor,
        string? audioPath,
        bool useServerSpeechRecognition)
    {
        _tunnel = tunnel;
        _servicesLocator = servicesLocator;
        _chatSessionData = chatSessionData;
        _startChatMessage = startChatMessage;
        _chatTextProcessor = chatTextProcessor;
        _audioPath = audioPath;
        _useServerSpeechRecognition = useServerSpeechRecognition;
        _logger = loggerFactory.CreateLogger<UserConnection>();

        if (useServerSpeechRecognition)
        {
            servicesLocator.LocalInputEventDispatcher.SpeechRecognitionStart += OnSpeechRecognitionStart;
            servicesLocator.LocalInputEventDispatcher.SpeechRecognitionEnd += OnSpeechRecognitionEnd;
        }
    }

    public async Task HandleMessageAsync(ClientSendMessage sendMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received chat message: {Text}", sendMessage.Text);
        if (_useServerSpeechRecognition) _servicesLocator.LocalInputEventDispatcher.OnPauseSpeechRecognition();
        // TODO: Save into some storage
        _chatSessionData.Messages.Add(new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = _chatSessionData.UserName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = _chatTextProcessor.ProcessText(sendMessage.Text),
        });

        var textGen = _servicesLocator.TextGenFactory.Create(_startChatMessage.TextGenService);
        var gen = await textGen.GenerateReplyAsync(_chatSessionData);
        await SendReply(_chatSessionData.BotName, gen, cancellationToken);
    }
    
    public Task HandleListenAsync()
    {
        if (!_useServerSpeechRecognition) return Task.CompletedTask;
        _servicesLocator.LocalInputEventDispatcher.OnReadyForSpeechRecognition();
        return Task.CompletedTask;
    }

    private async Task SendReply(string botName, TextData gen, CancellationToken cancellationToken, string? id = null)
    {
        var chatData = _chatSessionData;
        if (chatData == null) throw new NullReferenceException("No active chat");
        
        var reply = new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = botName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = gen.Text,
            Tokens = gen.Tokens,
        };
        _logger.LogInformation("Reply ({Tokens} tokens): {Text}", reply.Tokens, reply.Text);
        // TODO: Save into some storage
        chatData.Messages.Add(reply);

        var speechTask = CreateSpeech(gen.Text, $"msg_{_chatSessionData.ChatId.ToString()}_{reply.Id}", out var speechUrl);

        await _tunnel.SendAsync(new ServerReplyMessage
        {
            Text = reply.Text,
        }, cancellationToken);

        await speechTask;
        await _tunnel.SendAsync(new ServerSpeechMessage
        {
            Url = speechUrl,
        }, cancellationToken);

        #warning Re-enable this but not as a bot option
        /*
        if (_servicesLocator.AnimSelectFactory.TryCreate(bot.Services.AnimSelect.Service, out var animSelect))
        {
            var animation = await animSelect.SelectAnimationAsync(chatData);
            _logger.LogInformation("Selected animation: {Animation}", animation);
            await _tunnel.SendAsync(new ServerAnimationMessage { Value = animation }, cancellationToken);
        }
        */
    }

    private Task CreateSpeech(string text, string id, out string speechUrl)
    {
        var ttsService = _startChatMessage.TtsService;
        var ttsVoice = _startChatMessage.TtsVoice;
        if (string.IsNullOrEmpty(ttsService) || string.IsNullOrEmpty(ttsVoice))
        {
            speechUrl = "";
            return Task.CompletedTask;
        }

        Task speechTask;
        if (_audioPath != null)
        {
            var speechGen = _servicesLocator.TextToSpeechFactory.Create(ttsService);
            speechUrl = Path.Combine(_audioPath, $"{id}.wav");
            if (!File.Exists(speechUrl))
            {
                _servicesLocator.TemporaryFileCleanup.MarkForDeletion(speechUrl);
                speechTask = speechGen.GenerateSpeechAsync(new SpeechRequest
                {
                    Service = ttsService,
                    Text = text,
                    Voice = ttsVoice,
                }, new FileSpeechTunnel(speechUrl), "wav");
            }
            else
            {
                speechTask = Task.CompletedTask;
            }
        }
        else
        {
            speechUrl = CreateSpeechUrl(Crypto.CreateCryptographicallySecureGuid().ToString(), text, ttsService, ttsVoice);
            speechTask = Task.CompletedTask;
        }

        return speechTask;
    }

    private string CreateSpeechUrl(string id, string text, string ttsService, string ttsVoice)
    {
        _servicesLocator.PendingSpeech.Push(id, new SpeechRequest
        {
            Service = ttsService,
            Text = text,
            Voice = ttsVoice,
        });
        var speechUrl = $"/tts/{id}.wav";
        return speechUrl;
    }

    public async Task SendReadyAsync(CancellationToken cancellationToken)
    {
        if (_audioPath != null)
        {
            Directory.CreateDirectory(_audioPath);
        }

        #warning Bring back thinking speech
        /*
        var thinkingSpeechUrls = new string[_bot.ThinkingSpeech?.Length ?? 0];
        if (_bot.ThinkingSpeech != null)
        {
            byte i = 0;
            foreach (var thinkingSpeech in _bot.ThinkingSpeech)
            {
                await CreateSpeech(thinkingSpeech, Crypto.CreateCryptographicallySecureGuid().ToString(), out var speechUrl);
                thinkingSpeechUrls[i] = speechUrl;
                i++;
            }
        }
        */

        await _tunnel.SendAsync(new ServerReadyMessage
        {
            ChatId = _chatSessionData.ChatId,
            // ThinkingSpeechUrls = thinkingSpeechUrls,
                
        }, cancellationToken);

        if (_chatSessionData.Greeting != null)
        {
            await SendReply(_chatSessionData.BotName, _chatSessionData.Greeting, cancellationToken);
        }
    }

    private void OnSpeechRecognitionStart(object? sender, EventArgs e)
    {
        Task.Run(OnSpeechRecognitionStartAsync);
    }

    private async Task OnSpeechRecognitionStartAsync()
    {
        _logger.LogInformation("Speech recognition started");
        await _tunnel.SendAsync(new ServerSpeechRecognitionStartMessage(), CancellationToken.None);
    }

    private void OnSpeechRecognitionEnd(object? sender, string e)
    {
        Task.Run(() => OnSpeechRecognitionEndAsync(e));
    }

    private async Task OnSpeechRecognitionEndAsync(string e)
    {
        _logger.LogInformation("Speech recognition ended: {Text}", e);
        await _tunnel.SendAsync(new ServerSpeechRecognitionEndMessage { Text = e }, CancellationToken.None);
        await HandleMessageAsync(new ClientSendMessage { Text = e }, CancellationToken.None);
    }

    public void Dispose()
    {
        if (_useServerSpeechRecognition)
        {
            _servicesLocator.LocalInputEventDispatcher.SpeechRecognitionStart -= OnSpeechRecognitionStart;
            _servicesLocator.LocalInputEventDispatcher.SpeechRecognitionEnd -= OnSpeechRecognitionEnd;
        }
    }
}