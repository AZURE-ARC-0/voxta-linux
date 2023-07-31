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
    
    public IUserConnection Create(IUserConnectionTunnel tunnel)
    {
        return new UserConnection(
            tunnel,
            _sp.GetRequiredService<IUserConnectionManager>(),
            _sp.GetRequiredService<IProfileRepository>(),
            _sp.GetRequiredService<ICharacterRepository>(),
            _sp.GetRequiredService<IChatRepository>(),
            _sp.GetRequiredService<ChatSessionFactory>(),
            _sp.GetRequiredService<ILoggerFactory>()
        );
    }
}