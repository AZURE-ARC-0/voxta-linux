using System.Net;

public class ChatMateServerOptions
{
    public IPAddress IpAddress { get; set; } = IPAddress.Loopback;
    public int Port { get; set; } = 5384;
}