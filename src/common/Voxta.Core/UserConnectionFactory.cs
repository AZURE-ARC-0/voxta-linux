using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Voxta.Core;

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
            _sp.GetRequiredService<ICharacterRepository>(),
            _sp.GetRequiredService<ChatSessionFactory>()
        );
    }
}