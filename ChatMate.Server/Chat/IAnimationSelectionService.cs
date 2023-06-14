namespace ChatMate.Server;

public interface IAnimationSelectionService
{
    ValueTask<string> SelectAnimationAsync(ChatData chatData);
}