using System.Net.Sockets;

namespace ChatMate.Server;

public interface ITextToSpeechService
{
    ValueTask<string> GenerateSpeechUrlAsync(string text);
    Task HandleSpeechRequest(string rawRequest, NetworkStream responseStream);
}