using Voxta.Abstractions.Services;

namespace Voxta.IntegrationTests.Shared;

public class NullTokenCounter : ITokenCounter
{
    public int GetTokenCount(string message)
    {
        return 0;
    }
}