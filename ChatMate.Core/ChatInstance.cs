using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatInstance : IDisposable
{
    private readonly IChatSessionTunnel _tunnel;
    private readonly ChatServicesLocator _servicesLocator;
    private readonly ChatData _chatData;
    private readonly BotDefinition _bot;
    private readonly ChatTextProcessor _chatTextProcessor;
    private readonly string? _audioPath;
    private readonly bool _useServerSpeechRecognition;
    private readonly ILogger<ChatSession> _logger;

    public ChatInstance(
        IChatSessionTunnel tunnel,
        ILoggerFactory loggerFactory,
        ChatServicesLocator servicesLocator,
        ChatData chatData,
        BotDefinition bot,
        ChatTextProcessor chatTextProcessor,
        string? audioPath,
        bool useServerSpeechRecognition)
    {
        _tunnel = tunnel;
        _servicesLocator = servicesLocator;
        _chatData = chatData;
        _bot = bot;
        _chatTextProcessor = chatTextProcessor;
        _audioPath = audioPath;
        _useServerSpeechRecognition = useServerSpeechRecognition;
        _logger = loggerFactory.CreateLogger<ChatSession>();

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
        _chatData.Messages.Add(new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = _chatData.UserName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = _chatTextProcessor.ProcessText(sendMessage.Text),
        });

        var textGen = _servicesLocator.TextGenFactory.Create(_bot.Services.TextGen.Service);
        var gen = await textGen.GenerateReplyAsync(_chatData);
        await SendReply(_chatData.BotName, gen, cancellationToken);
    }
    
    public Task HandleListenAsync()
    {
        if (!_useServerSpeechRecognition) return Task.CompletedTask;
        _servicesLocator.LocalInputEventDispatcher.OnReadyForSpeechRecognition();
        return Task.CompletedTask;
    }

    private async Task SendReply(string botName, TextData gen, CancellationToken cancellationToken, string? id = null)
    {
        var chatData = _chatData;
        var bot = _bot;
        if (chatData == null || bot == null) throw new NullReferenceException("No active chat");
        
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

        var speechTask = CreateSpeech(gen.Text, $"msg_{_bot.Id}_{id ?? _chatData.Id.ToString()}_{reply.Id}", out var speechUrl);

        await _tunnel.SendAsync(new ServerReplyMessage
        {
            Text = reply.Text,
        }, cancellationToken);

        await speechTask;
        await _tunnel.SendAsync(new ServerSpeechMessage
        {
            Url = speechUrl,
        }, cancellationToken);

        if (_servicesLocator.AnimSelectFactory.TryCreate(bot.Services.AnimSelect.Service, out var animSelect))
        {
            var animation = await animSelect.SelectAnimationAsync(chatData);
            _logger.LogInformation("Selected animation: {Animation}", animation);
            await _tunnel.SendAsync(new ServerAnimationMessage { Value = animation }, cancellationToken);
        }
    }

    private Task CreateSpeech(string text, string id, out string speechUrl)
    {
        Task speechTask;
        if (_audioPath != null)
        {
            var speechGen = _servicesLocator.TextToSpeechFactory.Create(_bot.Services.SpeechGen.Service);
            speechUrl = Path.Combine(_audioPath, $"{id}.wav");
            if (!File.Exists(speechUrl))
            {
                _servicesLocator.TemporaryFileCleanup.MarkForDeletion(speechUrl);
                speechTask = speechGen.GenerateSpeechAsync(new SpeechRequest
                {
                    Service = _bot.Services.SpeechGen.Service,
                    Text = text,
                    Voice = _bot.Services.SpeechGen.Settings["Voice"]
                }, new FileSpeechTunnel(speechUrl), "wav");
            }
            else
            {
                speechTask = Task.CompletedTask;
            }
        }
        else
        {
            speechUrl = CreateSpeechUrl(Crypto.CreateCryptographicallySecureGuid().ToString(), text);
            speechTask = Task.CompletedTask;
        }

        return speechTask;
    }

    private string CreateSpeechUrl(string id, string text)
    {
        _servicesLocator.PendingSpeech.Push(id, new SpeechRequest
        {
            Service = _bot.Services.SpeechGen.Service,
            Text = text,
            Voice = _bot.Services.SpeechGen.Settings.TryGetValue("Voice", out var voice) ? voice : "Default"
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

        await _tunnel.SendAsync(new ServerReadyMessage
        {
            ChatId = _chatData.Id,
            BotId = _bot.Name,
            ThinkingSpeechUrls = thinkingSpeechUrls,
                
        }, cancellationToken);

        if (_chatData.Greeting.HasValue)
        {
            await SendReply(_chatData.BotName, _chatData.Greeting, cancellationToken);
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