using ChatMate.Abstractions.Network;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class UserConnectionFactory
{
    private readonly ChatServicesLocator _servicesLocator;
    private readonly ILoggerFactory _loggerFactory;

    public UserConnectionFactory(ChatServicesLocator servicesLocator, ILoggerFactory loggerFactory)
    {
        _servicesLocator = servicesLocator;
        _loggerFactory = loggerFactory;
    }
    
    public UserConnection Create(IUserConnectionTunnel tunnel)
    {
        return new UserConnection(tunnel, _loggerFactory, _servicesLocator);
    }
}