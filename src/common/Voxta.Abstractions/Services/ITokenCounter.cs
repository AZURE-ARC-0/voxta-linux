namespace Voxta.Abstractions.Services;

public interface ITokenCounter
{
    int GetTokenCount(string message);
}