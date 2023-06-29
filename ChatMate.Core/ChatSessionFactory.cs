using ChatMate.Abstractions.Network;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatSessionFactory
{
    private readonly ChatServicesLocator _servicesLocator;
    private readonly ILoggerFactory _loggerFactory;

    public ChatSessionFactory(ChatServicesLocator servicesLocator, ILoggerFactory loggerFactory)
    {
        _servicesLocator = servicesLocator;
        _loggerFactory = loggerFactory;
    }
    
    public ChatSession Create(IChatSessionTunnel tunnel)
    {
        return new ChatSession(tunnel, _loggerFactory, _servicesLocator);
    }
}