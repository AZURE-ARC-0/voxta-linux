using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;

namespace ChatMate.Server;

public class HttpProxyHandlerFactory
{
    public HttpProxyHandler Create(Span<byte> memorySpan, NetworkStream stream)
    {
        var line = memorySpan[..memorySpan.IndexOf((byte)'\r')];
        var parts = Encoding.ASCII.GetString(line).Split(' ');
        var uri = new Uri("http://localhost" + parts[1]);
        var query = HttpUtility.ParseQueryString(uri.Query);
        return new HttpProxyHandler(parts[0], uri, query, stream);
    }
}

public class HttpProxyHandler : IDisposable
{
    private readonly NetworkStream _responseStream;
    public string Method { get; }
    public Uri Uri { get; }
    public NameValueCollection Query { get; }
    
    private readonly StreamWriter _writer;

    public HttpProxyHandler(string method, Uri uri, NameValueCollection query, NetworkStream responseStream)
    {
        Method = method;
        Uri = uri;
        Query = query;
        _responseStream = responseStream;
        _writer = new StreamWriter(responseStream, Encoding.ASCII, leaveOpen: true);
    }

    private async Task StartResponse(HttpStatusCode status)
    {
        await _writer.WriteAsync($"HTTP/1.1 {(int)status} {status}\r\n");
        await _writer.WriteAsync("Date: " + DateTime.UtcNow.ToString("R") + "\r\n");
        await _writer.WriteAsync("Server: ChatMate\r\n");
        await _writer.WriteAsync("Connection: keep-alive\r\n");
    }

    public async Task WriteTextResponseAsync(HttpStatusCode status, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        await WriteBytesResponseAsync(status, bytes, "text/plain");
    }

    public async Task WriteBytesResponseAsync(HttpStatusCode status, byte[] bytes, string contentType)
    {
        await StartResponse(status);
        await _writer.WriteAsync($"Content-Type: {contentType}\r\n");
        await _writer.WriteAsync($"Content-Length: {bytes.Length}\r\n");
        await _writer.WriteAsync("\r\n");
        await _writer.FlushAsync();
        await _responseStream.WriteAsync(bytes);
        await _responseStream.FlushAsync();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}