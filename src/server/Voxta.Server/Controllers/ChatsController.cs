using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Repositories;
using Voxta.Server.ViewModels.Chats;

namespace Voxta.Server.Controllers;

[Controller]
public class ChatsController : Controller
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IChatMessageRepository _messageRepository;

    public ChatsController(ICharacterRepository characterRepository, IChatRepository chatRepository, IChatMessageRepository messageRepository)
    {
        _characterRepository = characterRepository;
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
    }
    
    [HttpGet("/chats")]
    public async Task<IActionResult> Chats(CancellationToken cancellationToken)
    {
        var chats = new List<ChatsListItemViewModel>();
        var characters = await _characterRepository.GetCharactersListAsync(cancellationToken);
        foreach(var character in characters)
        {
            foreach (var chat in await _chatRepository.GetChatsListAsync(character.Id, cancellationToken))
            {
                chats.Add(new ChatsListItemViewModel
                {
                    Id = chat.Id,
                    Created = (DateTimeOffset.UtcNow - chat.CreatedAt).Humanize(),
                    Character = character
                });                
            }
        }
        return View(chats);
    }
    
    [HttpPost("/chats/delete")]
    public async Task<IActionResult> Delete([FromForm] Guid chatId)
    {
        await _chatRepository.DeleteChatAsync(chatId);
        return RedirectToAction("Chats");
    }
    
    [HttpGet("/chats/{chatId}")]
    public async Task<IActionResult> Chat([FromRoute] Guid chatId, CancellationToken cancellationToken)
    {
        var chat = await _chatRepository.GetChatByIdAsync(chatId, cancellationToken);
        if (chat == null) return NotFound();
        var character = await _characterRepository.GetCharacterAsync(chat.CharacterId, cancellationToken);
        if (character == null) return NotFound();
        var messages = await _messageRepository.GetChatMessagesAsync(chat.Id, cancellationToken);
        return View(new ChatViewModel
        {
            Id = chat.Id,
            Created = chat.CreatedAt.Humanize(),
            Character = character,
            Messages = messages,
        });
    }
    
    [HttpPost("/chat/{id}/messages/{messageId}")]
    public async Task<IActionResult> Chat([FromRoute] Guid messageId, [FromBody] UpdateMessage value, CancellationToken cancellationToken)
    {
        
        var message = await _messageRepository.GetChatMessageAsync(messageId, cancellationToken);
        if (message == null) return NotFound();
        if (string.IsNullOrEmpty(value.Text))
        {
            await _messageRepository.DeleteMessageAsync(messageId);
        }
        else
        {
            message.Value = value.Text;
            await _messageRepository.UpdateMessageAsync(message);
        }

        return Ok();
    }

    [Serializable]
    public class UpdateMessage
    {
        public required string Text { get; init; }
    }
}