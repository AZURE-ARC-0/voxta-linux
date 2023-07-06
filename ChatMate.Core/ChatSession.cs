using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatSession : IDisposable
{
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ChatServicesLocator _servicesLocator;
    private readonly ChatSessionData _chatSessionData;
    private readonly ClientStartChatMessage _startChatMessage;
    private readonly ChatTextProcessor _chatTextProcessor;
    private readonly string? _audioPath;
    private readonly bool _pauseSpeechRecognitionDuringPlayback;
    private readonly ILogger<UserConnection> _logger;
    private readonly ExclusiveLocalInputHandle? _inputHandle;

    private bool _speaking;
    private CancellationTokenSource? _speakingAbort;

    public ChatSession(
        IUserConnectionTunnel tunnel,
        ILoggerFactory loggerFactory,
        ChatServicesLocator servicesLocator,
        ChatSessionData chatSessionData,
        ClientStartChatMessage startChatMessage,
        ChatTextProcessor chatTextProcessor,
        string? audioPath,
        bool useSpeechRecognition,
        bool pauseSpeechRecognitionDuringPlayback)
    {
        _tunnel = tunnel;
        _servicesLocator = servicesLocator;
        _chatSessionData = chatSessionData;
        _startChatMessage = startChatMessage;
        _chatTextProcessor = chatTextProcessor;
        _audioPath = audioPath;
        _pauseSpeechRecognitionDuringPlayback = pauseSpeechRecognitionDuringPlayback;
        _logger = loggerFactory.CreateLogger<UserConnection>();

        if (useSpeechRecognition)
        {
            _inputHandle = _servicesLocator.ExclusiveLocalInputManager.Acquire();
            _inputHandle.SpeechRecognitionStarted += OnSpeechRecognitionStarted;
            _inputHandle.SpeechRecognitionFinished += OnSpeechRecognitionFinished;
        }
    }

    public async Task HandleMessageAsync(ClientSendMessage sendMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received chat message: {Text}", sendMessage.Text);
        if (_pauseSpeechRecognitionDuringPlayback) _inputHandle?.RequestPauseSpeechRecognition();
        try
        {
            _speakingAbort?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        _speakingAbort = null;

        var textGen = _servicesLocator.TextGenFactory.Create(_startChatMessage.TextGenService);
        var text = sendMessage.Text;

        if (_speaking)
        {
            _speaking = false;
            #warning Refactor this, find a cleaner way to do that (e.g. estimate the audio length cutoff?)
            var lastBotMessage = _chatSessionData.Messages.LastOrDefault(m => m.User == _startChatMessage.BotName);
            if (lastBotMessage != null)
            {
                lastBotMessage.Text = lastBotMessage.Text[..(lastBotMessage.Text.Length / 2)] + "...";
                lastBotMessage.Tokens = textGen.GetTokenCount(lastBotMessage.Text);
                _logger.LogInformation("Cutoff last bot message to account for the interruption: {Text}", lastBotMessage.Text);
            }
            text = "*interrupts {{Bot}}* " + text;
            _logger.LogInformation("Added interruption notice to the user message: {Text}", text);
        }
        
        // TODO: Save into some storage
        _chatSessionData.Messages.Add(new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = _chatSessionData.UserName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = _chatTextProcessor.ProcessText(text),
        });

        using var speakingAbort = new CancellationTokenSource();
        _speakingAbort = speakingAbort;
        try
        {
            using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, speakingAbort.Token);
            var linkedCancellationToken = linkedCancellationSource.Token;

            ChatMessageData reply;
            try
            {
                var gen = await textGen.GenerateReplyAsync(_chatSessionData, linkedCancellationToken);
                if (string.IsNullOrWhiteSpace(gen.Text)) throw new InvalidOperationException("AI service returned an empty string.");
                reply = CreateMessageFromGen(gen);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            try
            {
                await SendReply(reply, linkedCancellationToken);
            }
            catch (TaskCanceledException)
            {
                if (cancellationToken.IsCancellationRequested) return;
                #warning This is tricky because we cancel the bot message but we can't say for sure if the response was sent. Requires refactoring.
                _chatSessionData.Messages.Remove(reply);
            }
        }
        finally
        {
            _speakingAbort = null;
        }
    }
    
    public Task HandleSpeechPlaybackCompleteAsync()
    {
        _speaking = false;
        _inputHandle?.RequestResumeSpeechRecognition();
        return Task.CompletedTask;
    }

    private async Task SendReply(ChatMessageData reply, CancellationToken cancellationToken)
    {
        var speechTask = CreateSpeechAsync(reply.Text, $"msg_{_chatSessionData.ChatId.ToString()}_{reply.Id}", out var speechUrl, cancellationToken);

        await _tunnel.SendAsync(new ServerReplyMessage
        {
            Text = reply.Text,
        }, cancellationToken);

        await speechTask;
        _speaking = true;
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

    private ChatMessageData CreateMessageFromGen(TextData gen)
    {
        var reply = new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = _startChatMessage.BotName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = gen.Text,
            Tokens = gen.Tokens,
        };
        _logger.LogInformation("Reply ({Tokens} tokens): {Text}", reply.Tokens, reply.Text);
        // TODO: Save into some storage
        _chatSessionData.Messages.Add(reply);
        return reply;
    }

    private Task CreateSpeechAsync(string text, string id, out string speechUrl, CancellationToken cancellationToken)
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
                    },
                    new FileSpeechTunnel(speechUrl),
                    "wav",
                    cancellationToken
                );
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
            var reply = CreateMessageFromGen(_chatSessionData.Greeting);
            await SendReply(reply, cancellationToken);
        }
    }

    private void OnSpeechRecognitionStarted(object? sender, EventArgs e)
    {
        Task.Run(OnSpeechRecognitionStartedAsync);
    }

    private async Task OnSpeechRecognitionStartedAsync()
    {
        _logger.LogInformation("Speech recognition started");
        _speakingAbort?.Cancel();
        await _tunnel.SendAsync(new ServerSpeechRecognitionStartMessage(), CancellationToken.None);
    }

    private void OnSpeechRecognitionFinished(object? sender, string e)
    {
        if (_pauseSpeechRecognitionDuringPlayback) _inputHandle?.RequestPauseSpeechRecognition();
        Task.Run(() => OnSpeechRecognitionFinishedAsync(e));
    }

    private async Task OnSpeechRecognitionFinishedAsync(string e)
    {
        _logger.LogInformation("Speech recognition finished: {Text}", e);
        await _tunnel.SendAsync(new ServerSpeechRecognitionEndMessage { Text = e }, CancellationToken.None);
        await HandleMessageAsync(new ClientSendMessage { Text = e }, CancellationToken.None);
    }

    public void Dispose()
    {
        if (_inputHandle != null)
        {
            _inputHandle.SpeechRecognitionStarted -= OnSpeechRecognitionStarted;
            _inputHandle.SpeechRecognitionFinished -= OnSpeechRecognitionFinished;
            _inputHandle.Dispose();
        }
    }
}