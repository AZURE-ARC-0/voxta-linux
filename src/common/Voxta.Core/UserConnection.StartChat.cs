using Voxta.Abstractions.Model;
using Microsoft.Extensions.Logging;
using Voxta.Common;

namespace Voxta.Core;

public sealed partial class UserConnection
{
    private async Task DisposeAndLockChat(CancellationToken cancellationToken)
    {
        if (_chat != null) await _chat.DisposeAsync();
        _chat = null;

        if (!_userConnectionManager.TryGetChatLock(this))
        {
            await SendError("Another chat is in progress, close this one first.", cancellationToken);
            throw new InvalidOperationException("Another chat is in progress.");
        }
    }

    private async Task<Chat> CreateNewChat(Character character)
    {
        var chat = new Chat
        {
            Id = Crypto.CreateCryptographicallySecureGuid(),
            CharacterId = character.Id,
        };
        await _chatRepository.SaveChatAsync(chat);
        return chat;
    }

    private async Task StartChatAsync(ClientNewChatMessage newChatMessage, CancellationToken cancellationToken)
    {
        await DisposeAndLockChat(cancellationToken);

        var character = await _charactersRepository.GetCharacterAsync(newChatMessage.CharacterId, cancellationToken);
        if (character == null) throw new NullReferenceException($"Could not find character {newChatMessage.CharacterId}");

        foreach (var c in await _chatRepository.GetChatsListAsync(newChatMessage.CharacterId, CancellationToken.None))
        {
            await _chatRepository.DeleteAsync(c.Id);
        }

        var chat = await CreateNewChat(character);

        _chat = await _chatSessionFactory.CreateAsync(_tunnel, chat, character, newChatMessage, cancellationToken);
        _logger.LogInformation("Started chat: {ChatId}", chat.Id);
        _chat.SendReady();
    }

    private async Task StartChatAsync(ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        await DisposeAndLockChat(cancellationToken);

        var character = await _charactersRepository.GetCharacterAsync(startChatMessage.Character.Id, cancellationToken);
        if (character == null)
        {
            await _charactersRepository.SaveCharacterAsync(startChatMessage.Character);
            character = startChatMessage.Character;
        }

        Chat? chat = null;
        if (startChatMessage.ChatId != null)
        {
            chat = await _chatRepository.GetChatAsync(startChatMessage.ChatId.Value, cancellationToken);
        }

        chat ??= await CreateNewChat(character);

        _chat = await _chatSessionFactory.CreateAsync(_tunnel, chat, character, startChatMessage, cancellationToken);
        _logger.LogInformation("Started chat: {ChatId}", startChatMessage.ChatId);
        _chat.SendReady();
    }

    private async Task ResumeChatAsync(ClientResumeChatMessage resumeChatMessage, CancellationToken cancellationToken)
    {
        await DisposeAndLockChat(cancellationToken);

        var chat = await _chatRepository.GetChatAsync(resumeChatMessage.ChatId, cancellationToken);
        if (chat == null) throw new InvalidOperationException($"Chat {resumeChatMessage.ChatId} not found");

        var character = await _charactersRepository.GetCharacterAsync(chat.CharacterId, cancellationToken);
        if (character == null) throw new InvalidOperationException($"Character {chat.CharacterId} referenced in chat {resumeChatMessage.ChatId} was found");

        _chat = await _chatSessionFactory.CreateAsync(_tunnel, chat, character, resumeChatMessage, cancellationToken);
        _logger.LogInformation("Started chat: {ChatId}", resumeChatMessage.ChatId);
        _chat.SendReady();
    }
}
