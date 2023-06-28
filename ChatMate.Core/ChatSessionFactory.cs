using ChatMate.Abstractions.Network;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatSessionFactory
{
    private readonly ChatServicesFactory _servicesFactory;
    private readonly ILoggerFactory _loggerFactory;

    public ChatSessionFactory(ChatServicesFactory servicesFactory, ILoggerFactory loggerFactory)
    {
        _servicesFactory = servicesFactory;
        _loggerFactory = loggerFactory;
    }
    
    public ChatSession Create(IChatSessionTunnel tunnel)
    {
        return new ChatSession(tunnel, _loggerFactory, _servicesFactory);
    }
}