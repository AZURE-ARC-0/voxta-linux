using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class UserConnectionFactory
{
    private readonly IServiceProvider _sp;

    public UserConnectionFactory(
        IServiceProvider sp
        )
    {
        _sp = sp;
    }
    
    public UserConnection Create(IUserConnectionTunnel tunnel)
    {
        return new UserConnection(
            tunnel,
            _sp.GetRequiredService<ILoggerFactory>(),
            _sp.GetRequiredService<IBotRepository>(),
            _sp.GetRequiredService<ChatSessionFactory>()
        );
    }
}