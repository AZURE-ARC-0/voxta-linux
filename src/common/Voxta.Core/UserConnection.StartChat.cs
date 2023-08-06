using Voxta.Abstractions.Model;
using Microsoft.Extensions.Logging;
using Voxta.Common;

namespace Voxta.Core;

public sealed partial class UserConnection
{
    private async Task DisposeAndLockChatAsync(CancellationToken cancellationToken)
    {
        if (_chat != null) await _chat.DisposeAsync();
        _chat = null;

        if (!_userConnectionManager.TryGetChatLock(this))
        {
            await SendError("Another chat is in progress, close this one first.", cancellationToken);
            throw new InvalidOperationException("Another chat is in progress.");
        }
    }

    private async Task<Chat> CreateNewChatAsync(Character character)
    {
        var chat = new Chat
        {
            Id = Crypto.CreateCryptographicallySecureGuid(),
            CharacterId = character.Id,
        };
        await _chatRepository.SaveChatAsync(chat);
        return chat;
    }

    private async Task HandleNewChatAsync(ClientNewChatMessage newChatMessage, CancellationToken cancellationToken)
    {
        await DisposeAndLockChatAsync(cancellationToken);

        var character = await _charactersRepository.GetCharacterAsync(newChatMessage.CharacterId, cancellationToken);
        if (character == null) throw new NullReferenceException($"Could not find character {newChatMessage.CharacterId}");

        foreach (var c in await _chatRepository.GetChatsListAsync(newChatMessage.CharacterId, CancellationToken.None))
        {
            await _chatRepository.DeleteChatAsync(c.Id);
            _logger.LogInformation("Deleted previous chat {ChatId}", c.Id);
        }

        var chat = await CreateNewChatAsync(character);

        _chat = await _chatSessionFactory.CreateAsync(_tunnel, chat, character, newChatMessage, cancellationToken);
        _logger.LogInformation("New chat: {ChatId}", chat.Id);
        _chat.HandleStartChat();
    }

    private async Task HandleStartChatAsync(ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        await DisposeAndLockChatAsync(cancellationToken);

        var character = await _charactersRepository.GetCharacterAsync(startChatMessage.Character.Id, cancellationToken);
        if (character == null)
        {
            await _charactersRepository.SaveCharacterAsync(startChatMessage.Character);
            character = startChatMessage.Character;
        }

        Chat? chat = null;
        if (startChatMessage.ChatId != null)
        {
            chat = await _chatRepository.GetChatByIdAsync(startChatMessage.ChatId.Value, cancellationToken);
        }

        chat ??= await CreateNewChatAsync(character);

        _chat = await _chatSessionFactory.CreateAsync(_tunnel, chat, character, startChatMessage, cancellationToken);
        _logger.LogInformation("Started chat: {ChatId}", startChatMessage.ChatId);
        _chat.HandleStartChat();
    }

    private async Task HandleResumeChatAsync(ClientResumeChatMessage resumeChatMessage, CancellationToken cancellationToken)
    {
        await DisposeAndLockChatAsync(cancellationToken);

        var chat = await _chatRepository.GetChatByIdAsync(resumeChatMessage.ChatId, cancellationToken);
        if (chat == null) throw new InvalidOperationException($"Chat {resumeChatMessage.ChatId} not found");

        var character = await _charactersRepository.GetCharacterAsync(chat.CharacterId, cancellationToken);
        if (character == null) throw new InvalidOperationException($"Character {chat.CharacterId} referenced in chat {resumeChatMessage.ChatId} was found");

        _chat = await _chatSessionFactory.CreateAsync(_tunnel, chat, character, resumeChatMessage, cancellationToken);
        _logger.LogInformation("Resumed chat: {ChatId}", resumeChatMessage.ChatId);
        _chat.HandleStartChat();
    }
}
