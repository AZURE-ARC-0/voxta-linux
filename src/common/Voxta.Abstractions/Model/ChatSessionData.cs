using System.Globalization;

namespace Voxta.Abstractions.Model;

public interface IChatSessionDataUnsafe : IChatEditableData
{
    List<ChatMessageData> Messages { get; }
    List<ChatSessionDataMemory> Memories { get; }
}

[Serializable]
public class ChatSessionData : IChatSessionDataUnsafe
{
    private readonly ReaderWriterLockSlim _accessLock = new();

    public Guid Id => Chat.Id;
    
    public required Chat Chat { get; init; }
    public required string Culture { get; set; } = "en-US";
    public CultureInfo CultureInfo => CultureInfo.GetCultureInfoByIetfLanguageTag(Culture);
    public required ChatSessionDataUser User { get; init; }

    public required ChatSessionDataCharacter Character { get; init; }
    public TextData? Context { get; set; }
    public string[]? Actions { get; set; }
    public string[]? ThinkingSpeech { get; init; }

    List<ChatMessageData> IChatSessionDataUnsafe.Messages { get; } = new();
    List<ChatSessionDataMemory> IChatSessionDataUnsafe.Memories { get; } = new();

    public string? AudioPath { get; init; }
    
    
    public ChatSessionDataReadToken GetReadToken()
    {
        _accessLock.EnterReadLock();
        return new ChatSessionDataReadToken(this, () => _accessLock.ExitReadLock());
    }
    
    public ChatSessionDataWriteToken GetWriteToken()
    {
        _accessLock.EnterWriteLock();
        return new ChatSessionDataWriteToken(this, () => _accessLock.ExitWriteLock());
    }
}