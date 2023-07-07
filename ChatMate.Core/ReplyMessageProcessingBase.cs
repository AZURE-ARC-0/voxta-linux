using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Common;

namespace ChatMate.Core;

public abstract class ReplyMessageProcessingBase
{
#warning It might be better to just inline it again
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ChatSessionData _chatSessionData;
    private readonly ChatSessionState _chatSessionState;
    private readonly ClientStartChatMessage _startChatMessage;
    private readonly ChatServices _services;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly ITemporaryFileCleanup _temporaryFileCleanup;

    protected ReplyMessageProcessingBase(IUserConnectionTunnel tunnel, ChatSessionData chatSessionData, ChatSessionState chatSessionState, ClientStartChatMessage startChatMessage, ChatServices services, PendingSpeechManager pendingSpeech, ITemporaryFileCleanup temporaryFileCleanup)
    {
        _tunnel = tunnel;
        _chatSessionData = chatSessionData;
        _chatSessionState = chatSessionState;
        _startChatMessage = startChatMessage;
        _services = services;
        _pendingSpeech = pendingSpeech;
        _temporaryFileCleanup = temporaryFileCleanup;
    }
}